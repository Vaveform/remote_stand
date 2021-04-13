using System;
using WebSocketSharp;
using Microsoft.MixedReality.WebRTC;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.Web;
using System.Text.Json;
using System.Runtime.InteropServices;
using MicrocontrollerAPI;
using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Text.RegularExpressions;

namespace WebRTC_Remote_FPGA_stand
{
    public interface ISystemController
    {
        void Notify(object sender, string message);
    }

    public class SystemController : ISystemController
    {
        public void Notify(object sender, string message)
        {
            // Main logic method.
        }
    }

    public class HardwareCell
    {
        // Quartus Prime Command Line Interface Instance
        // Using USB Jtag Programmer connected field programmable gates array (FPGA)
        public Quartus QuartusSource { get; set; }

        // Arduino Nano/Uno using to control the FPGA
        // via GPIO interface
        public Microcontroller ControlSource { get; set; }
    }
    public class UserCell
    {

        public PeerConnection WebRTCConnection { get; set; }
        public FileStream AssignedUserFile { get; set; }
    }

    class Program
    {
        // Working with sdp and ice candidate messages

        private static string CreateSignalingServerUrl()
        {
            Console.WriteLine("Input room number:");
            return $"wss://wss-signaling.herokuapp.com?room={Convert.ToInt32(Console.ReadLine())}";
        }
        private static void CheckStatus(object stateInfo)
        {
            Console.WriteLine("Time of connection end");
        }
        static private async Task StartStend()
        {
            var autoEvent = new AutoResetEvent(false);
            bool video_translator = true;
            bool file_created = false;
            FileStream file = null;
            Quartus quartus = Quartus.GetInstance();
            //Microcontroller arduino = Microcontroller.Create(); 

            if (video_translator)
            {
                // Asynchronously retrieve a list of available video capture devices (webcams).
                var deviceList = await DeviceVideoTrackSource.GetCaptureDevicesAsync();


                // For example, print them to the standard output
                foreach (var device in deviceList)
                {
                    Console.WriteLine($"Found webcam {device.name} (id: {device.id})");
                }
            }

            // Create a new peer connection automatically disposed at the end of the program
            var pc = new PeerConnection();
            // Initialize the connection with a STUN server to allow remote access
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } },
                            new IceServer{ Urls = { "stun:stun1.l.google.com:19302" } },
                            new IceServer{ Urls = { "stun:stun2.l.google.com:19302" } },
                            new IceServer{ Urls = { "stun:stun3.l.google.com:19302" } },
                            new IceServer
                            {
                                Urls = {"turn:numb.viagenie.ca" },
                                TurnPassword = "9u7prU:2}R{Sut~.)d[bP7,;Pgc\'Pa",
                                TurnUserName = "fkrveacbukypqsqyaq@miucce.com"
                            }

                }
            };
            config.IceServers.Add(new IceServer { Urls = { ConfigurationManager.AppSettings.Get("Stun1") } });


            await pc.InitializeAsync(config);
            Console.WriteLine("Peer connection initialized.");
            //var chen = await pc.AddDataChannelAsync("sendDataChannel", true, true, cancellationToken: default);
            Console.WriteLine("Opening local webcam...");


            // pc - PeerConnection object
            Transceiver videoTransceiver = null;
            VideoTrackSource videoTrackSource = null;
            LocalVideoTrack localVideoTrack = null;

            videoTrackSource = (await Camera.CreateAsync()).source;
            Console.WriteLine("Create local video track...");
            var trackSettings = new LocalVideoTrackInitConfig { trackName = "webcam_track" };
            localVideoTrack = LocalVideoTrack.CreateFromSource(videoTrackSource, trackSettings);
            Console.WriteLine("Create video transceiver and add webcam track...");
            TransceiverInitSettings option = new TransceiverInitSettings();
            option.Name = "webcam_track";
            option.StreamIDs = new List<string> { "webcam_name" };
            videoTransceiver = pc.AddTransceiver(MediaKind.Video, option);
            videoTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            videoTransceiver.LocalVideoTrack = localVideoTrack;

            WebSocketSharp.WebSocket signaling = new WebSocketSharp.WebSocket(CreateSignalingServerUrl(), "id_token", "alpine");

            pc.LocalSdpReadytoSend += (SdpMessage message) =>
            {
                //Console.WriteLine(SdpMessage.TypeToString(message.Type));
                Console.WriteLine(message.Content);
                //Console.WriteLine(HttpUtility.JavaScriptStringEncode(message.Content));
                Console.WriteLine("Sdp offer to send: {\"data\":{\"description\":{\"type\":\"" + SdpMessage.TypeToString(message.Type) + "\",\"sdp\":\"" + HttpUtility.JavaScriptStringEncode(message.Content) + "\"}}}");
                signaling.Send(message.ToABJson());
            };

            pc.RenegotiationNeeded += () =>
            {
                Console.WriteLine("Regotiation needed");
                bool OfferCreated = pc.CreateOffer();
                Console.WriteLine("OfferCreated? {0}", OfferCreated);

            };
            pc.DataChannelAdded += (DataChannel channel) =>
            {
                Console.WriteLine("Added data channel ID: {0}, Label: {1}; Reliable: {2}, Ordered: {3}", channel.ID, channel.Label, channel.Reliable, channel.Ordered);

                if (channel.Label == "sendDataChannel")
                {
                    channel.MessageReceived += (byte[] mess) => {
                        try
                        {
                            CTP_packet command = JsonSerializer.Deserialize<CTP_packet>(mess);
                            //arduino.SendCTP_Command(command);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    };

                }
                else
                {
                    if (file_created == false)
                    {
                        file = new FileStream(channel.Label, FileMode.Append);
                        file_created = true;
                    }
                    channel.MessageReceived += async (byte[] mess) =>
                    {
                        // Console.WriteLine(System.Text.Encoding.Default.GetString(mess));
                        if (mess.Length == 3 && System.Text.Encoding.Default.GetString(mess) == "EOF")
                        {
                            string file_name = file.Name;
                            file.Close();
                            string t = await quartus.RunQuartusCommandAsync($"quartus_pgm -m jtag –o \"p;{file_name}@1\"");
                            File.Delete(file_name);
                            file_created = false;
                        }
                        else
                        {
                            WriteFileSegment(mess, file);
                        }


                    };
                }

                channel.StateChanged += () =>
                {
                    Console.WriteLine("State change: {0}", channel.State);
                };
            };

            pc.IceCandidateReadytoSend += (IceCandidate candidate) =>
            {
                //Console.WriteLine("Content: {0}, SdpMid: {1}, SdpMlineIndex: {2}", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
                try
                {
                    Console.WriteLine("Candidate to send: Content: {0}, SdpMid: {1}, SdpMlineIndex: {2}", candidate.Content, candidate.SdpMid, candidate.SdpMlineIndex);
                    signaling.Send(candidate.ToABJson());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error to send local ice candidate");
                }

            };
            //videoTrackSource.I420AVideoFrameReady += (frame) =>
            //{
            //    Console.WriteLine("Argb32 frame ready. {0} : {1}", frame.width, frame.height);
            //    Console.WriteLine("DataA: {0}, DataU: {1}, DataV: {2}, DataY: {3}", Marshal.SizeOf(frame.dataA),
            //                        Marshal.SizeOf(frame.dataU),
            //                        Marshal.SizeOf(frame.dataV),
            //                        Marshal.SizeOf(frame.dataY));
            //};

            signaling.OnMessage += async (sender, message) =>
            {
                (string header, string correct_message) = message.Data.DivideHeaderAndOriginalJSON();
                Console.WriteLine("Correct message: {0}", correct_message);
                Console.WriteLine("Header: {0}", header);
                if (header == "{\"data\":{\"getRemoteMedia\":" && correct_message == "true")
                {

                    bool OfferCreated = pc.CreateOffer();
                    Console.WriteLine("OfferCreated? {0}", OfferCreated);
                }
                //Console.WriteLine(message.Data);
                if (header.IndexOf("candidate") != -1 && correct_message != "null")
                {
                    try
                    {
                        var candidate = JsonSerializer.Deserialize<ICEJavaScriptNotation>(correct_message);
                        Console.WriteLine("Content of ice: {0}, SdpMid: {1}, SdpMLineIndex: {2}", candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex);
                        pc.AddIceCandidate(candidate.ToMRNetCoreNotation());
                        Console.WriteLine("Deserialized by ice_candidate");
                        //return;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could not deserialize as ice candidate");
                    }
                }

                if (header.IndexOf("description") != -1)
                {
                    try
                    {
                        SdpMessage received_description = JsonSerializer.Deserialize<SDPJavaScriptNotation>(correct_message).ToMRNetCoreNotation();
                        await pc.SetRemoteDescriptionAsync(received_description).ConfigureAwait(true);
                        if (received_description.Type == SdpMessageType.Offer)
                        {
                            bool res = pc.CreateAnswer();
                            Console.WriteLine("Answer created? {0}", res);
                        }
                        Console.WriteLine("Deserialized by sdp_message");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could not deserialize as sdp message");
                    }
                }
            };


            pc.Connected += () =>
            {
                Console.WriteLine("Connected");

            };
            pc.IceStateChanged += (IceConnectionState newState) =>
            {
                if (newState == IceConnectionState.Disconnected)
                {
                    Console.WriteLine("Disconected");
                }
            };


            signaling.Connect();
            if (!video_translator)
            {
                signaling.Send("{\"data\":{\"getRemoteMedia\":true}}");
            }

            //Console.WriteLine("Press a key to terminate the application...");
            Console.ReadKey(true);
            Console.WriteLine("Program termined.");
            file?.Close();
            pc?.Close();
            signaling?.Close();
            //arduino?.Close();
            //(var a, var b) = ConvertString("{\"data\":{\"candidate\":null}}");
            //Console.WriteLine("{0}, {1}", a, b);
        }

        private static void WriteFileSegment(byte[] segment, FileStream stream)
        {
            if (stream.CanWrite)
            {
                stream.Write(segment);
            }
        }

        


        static async Task Main(string[] args)
        {
            //var t = SystemConfiguration.SignalingURL;
            Console.WriteLine("dsfsdf");
        }
    }
}
