using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using MicrocontrollerAPI;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using WebSocketSharp;

namespace WebRTC_Remote_FPGA_stand
{
    
    
    

    public class SystemController : ISystemController
    {
        private static void WritingVideo(I420AVideoFrame frame) {
            Console.WriteLine("Writing video for async working: width: {0}, height {1}", frame.width, frame.height);
        }
        protected ISignaling SignalingMechanism { get; set; }
        protected IUserCell ClientCell { get; set; }
        protected VideoTrackSource Source { get; set; }
        protected PeerConnection Connection { get; set; } 

        public async Task NotifyPeerConnection(object sender, Type message_type, string message)
        {
            try
            {
                (string header, string correct_message) = message.DivideHeaderAndOriginalJSON();
                Console.WriteLine("Called NotifyPeerConnection with header: {0}", header);
                if (header == "{\"data\":{\"getRemoteMedia\":" && correct_message == "true")
                {
                    Connection.CreateOffer();
                    // Console.WriteLine("GetMedia call after await ended in thread {0}", Thread.CurrentThread.ManagedThreadId);
                }
                if (header.IndexOf("candidate") != -1 && correct_message != "null")
                {
                    Connection.AddIceCandidate(
                        JsonSerializer.Deserialize<ICEJavaScriptNotation>(correct_message).
                        ToMRNetCoreNotation());
                }
                if (header.IndexOf("description") != -1)
                {
                    SdpMessage sdp = JsonSerializer.Deserialize<SDPJavaScriptNotation>(correct_message).
                        ToMRNetCoreNotation();
                    await Connection.SetRemoteDescriptionAsync(sdp);
                    if (sdp.Type == SdpMessageType.Offer) {
                        Connection.CreateAnswer();
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine("In NotifyPeerConnection caused exception: {0}", ex.Message);
            }
        }

        public void NotifySignaling(object sender, Type message_type, string message)
        {
            Console.WriteLine("Called NotifySignaling with message: {0} in thread {1}", message, Thread.CurrentThread.ManagedThreadId);
            if (SignalingMechanism != null)
            {
                SignalingMechanism.SendToEndPoint(sender, message);
            }
        }

        public void PeerConnected(object sender, string message)
        {
            Console.WriteLine("User connected");
            Console.WriteLine("Here stop working async compucting");
            Source.I420AVideoFrameReady -= WritingVideo;
            ClientCell.AddRemoteControlling(Connection);
        }

        public async Task PeerConnectionClosed(object sender, string message)
        {
            if (!Source.Enabled) {
                Source = await Camera.CreateAsync(SystemConfiguration.VideoDeviceSettings);
            }
            Connection = await WebRTCPeerCreator.InitializePeerConnection(this);
            WebRTCPeerCreator.AddVideoTransceiver(Connection, Source);

        }

        public async Task RunSystem()
        {
            await VideoDeviceSelection();
            await SystemStartup();
            SignalingMechanism = new WebSocketSignaling(this);
            var autoEvent = new AutoResetEvent(false);
            Console.ReadKey(true);
            Console.WriteLine("Program termined.");
        }

        public void SignalingClosed(object sender, string message)
        {
            SignalingMechanism?.Dispose();
            SignalingMechanism = new WebSocketSignaling(this);
        }



        public async Task VideoDeviceSelection() {
            Console.WriteLine("Select availiable video device:");
            var devices_list = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
            var VideoDevice = ApplicationInterface.VideoDeviceSelector(devices_list);
            Console.WriteLine("Select video format:");
            var formats_list = await DeviceVideoTrackSource.GetCaptureFormatsAsync(VideoDevice.id);
            var SelectedFormat = ApplicationInterface.CapturingFormatSelector(formats_list);
            SystemConfiguration.VideoDeviceSettings.framerate = SelectedFormat.framerate;
            SystemConfiguration.VideoDeviceSettings.height = SelectedFormat.height;
            SystemConfiguration.VideoDeviceSettings.width = SelectedFormat.width;
            Source = await Camera.CreateAsync(SystemConfiguration.VideoDeviceSettings);
        }

        public async Task SystemStartup()
        {
            Console.WriteLine("SystemStartup task");
            if (!Source.Enabled)
            {
                Source = await Camera.CreateAsync(SystemConfiguration.VideoDeviceSettings);
            }
            Connection = await WebRTCPeerCreator.InitializePeerConnection(this);
            ClientCell = new UserCell(this);
            WebRTCPeerCreator.AddVideoTransceiver(Connection, Source);
        }

        public void PeerConnectionDisconnected(object sender, string message)
        {
            Console.WriteLine("User disconnected");
            Console.WriteLine("Here can working async compucting");
            ClientCell.RemoveRemoteControlling(Connection);
            Source.I420AVideoFrameReady += WritingVideo;
        }

    }
}
