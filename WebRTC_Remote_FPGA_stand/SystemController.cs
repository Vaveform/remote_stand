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
    public static class ApplicationInterface {
        public static VideoCaptureFormat CapturingFormatSelector(IReadOnlyList<VideoCaptureFormat> VideoFormats) {
            int availibale_devices_count = VideoFormats.Count();
            if (VideoFormats.Count() == 0)
            {
                throw new PlatformNotSupportedException("Select object not found.");
            }
            int index = 0;
            foreach (var format in VideoFormats)
            {
                Console.WriteLine("Format #{0}: framerate = {1}, resolution = {2}x{3}", index, format.framerate, format.width, format.height);
                index++;
            }
            int Selected = 0;
            while (true)
            {
                try
                {
                    Selected = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown videoformat, try select again.");
                    continue;
                }

                if (Selected < 0 || Selected >= availibale_devices_count)
                {
                    Console.WriteLine("Unknown videoformat, try select again.");
                }
                else
                {
                    break;
                }
            }
            return VideoFormats[Selected];

        }
        public static VideoCaptureDevice VideoDeviceSelector(IReadOnlyList<VideoCaptureDevice> VideoDevices) {
            int availibale_devices_count = VideoDevices.Count();
            if (VideoDevices.Count() == 0) {
                throw new PlatformNotSupportedException("Select object not found.");
            }
            int index = 0;
            foreach (var device in VideoDevices)
            {
                Console.WriteLine("{0}: Name: {1} ID: {2}", index, device.name, device.id);
                index++;
            }
            int Selected = 0;
            while (true)
            {
                try
                {
                    Selected = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception ex) {
                    Console.WriteLine("Unknown device, try select again.");
                    continue;
                }

                if (Selected < 0 || Selected >= availibale_devices_count)
                {
                    Console.WriteLine("Unknown device, try select again.");
                }
                else {
                    break;
                }
            }
            return VideoDevices[Selected];
        }
    }
    public static class WebRTCPeerCreator {
        public static PeerConnection AddVideoTransceiver(PeerConnection connection, VideoTrackSource source) {
            Console.WriteLine("Added video transceiver to peer connection in thread {0}", Thread.CurrentThread.ManagedThreadId);
            LocalVideoTrack localVideoTrack = LocalVideoTrack.CreateFromSource(source, new LocalVideoTrackInitConfig { trackName = "webcam_track" });
            Console.WriteLine("Create video transceiver and add webcam track...");
            TransceiverInitSettings option = new TransceiverInitSettings();
            option.Name = "webcam_track";
            option.StreamIDs = new List<string> { "webcam_name" };

            Transceiver videoTransceiver = connection.AddTransceiver(MediaKind.Video, option);
            videoTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            videoTransceiver.LocalVideoTrack = localVideoTrack;
            return connection;
        }
        public static async Task<PeerConnection> InitializePeerConnection(ISystemController Controller) {
            Console.WriteLine("Initializing peer connection in thread: {0}", Thread.CurrentThread.ManagedThreadId);
            PeerConnection connection = new PeerConnection();
            
            connection.Connected += () =>
            {
                Controller.PeerConnected(connection, "New user connected");
            };

            connection.IceCandidateReadytoSend += (candidate) =>
            {
                Controller.NotifySignaling(connection, typeof(IceCandidate), candidate.ToABJson());
            };

            connection.LocalSdpReadytoSend += (sdp) =>
            {
                Controller.NotifySignaling(connection, typeof(SdpMessage), sdp.ToABJson());
            };

            connection.RenegotiationNeeded += () =>
            {
                Console.WriteLine("Regotiation");
                bool OfferCreated = connection.CreateOffer();
            };
            connection.IceStateChanged += async (state) =>
            {
                if (state == IceConnectionState.Closed)
                {
                    await Controller.PeerConnectionClosed(connection, "ICE state change to closed");
                }
                if (state == IceConnectionState.Disconnected)
                {
                    Controller.PeerConnectionDisconnected(connection, "ICE state change to disconnected");
                }
            };

            await connection.InitializeAsync(SystemConfiguration.PeerConnectionSettings);

            return connection;
        }
    }
    public interface ISystemController
    {
        public void NotifySignaling(object sender, Type message_type, string message);
        public Task NotifyPeerConnection(object sender, Type message_type, string message);
        public Task PeerConnectionClosed(object sender, string message);
        public void PeerConnectionDisconnected(object sender, string message);
        public void SignalingClosed(object sender, string message);
        public void PeerConnected(object sender, string message);
        public Task SystemStartup();
        public Task RunSystem();
    }

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

    public abstract class ISignaling : IDisposable
    {
        protected ISystemController MainController { get; set; }

        abstract public void Dispose();

        abstract public void SendToEndPoint(object sender, string message);

    }

    public class WebSocketSignaling : ISignaling
    {
        protected WebSocket Socket { get; set; }
        public WebSocketSignaling(ISystemController _MainController) {
            MainController = _MainController;
            Socket = new WebSocket(SystemConfiguration.SignalingURL, SystemConfiguration.WebSocketTokens);
            Socket.Connect();
            Socket.OnError += (sender, message) =>
            {
                MainController.SignalingClosed(Socket, message.Message);
            };
            Socket.OnClose +=  (sender, message) =>
            {
                MainController.SignalingClosed(Socket, message.Reason);
            };

            Socket.OnMessage += async (sender, message) =>
            {
                await MainController.NotifyPeerConnection(Socket, typeof(string), message.Data);
                //if (!t.IsFaulted) {
                //    Console.WriteLine("On Message task failed");
                //    await t;
                //}
                
            };

            Console.WriteLine("Constructed Signaling");

        }

        public override void SendToEndPoint(object sender, string message)
        {
            Socket.Send(message);
        }

        public override void Dispose()
        {
            if (Socket.IsAlive == true) {
                Socket.Close();
            }
        }
    }

    public abstract class IUserCell
    {
        protected ISystemController MainController { get; set; }
        abstract public void AddRemoteControlling(PeerConnection Connection);
        abstract public void RemoveRemoteControlling(PeerConnection Connection);
    }

    public class UserCell : IUserCell
    {

        private FileStream UserAssignedFile {get; set;}
        private bool FileCreated { get; set; } = false;
        private Quartus FirmwareLoader { get; set; }
        private Microcontroller InputEmulator { get; set; }

        private static void WriteFileSegment(byte[] segment, FileStream stream)
        {
            if (stream.CanWrite)
            {
                Console.WriteLine("Writing file segment with size: {0}", segment.Length);
                stream.Write(segment);
            }
        }

        // VideoTrackSource initialized in Startup async method
        public UserCell(ISystemController _MainController) {
            Console.WriteLine("Working constructor of UserCell in thread {0}", Thread.CurrentThread.ManagedThreadId);
            MainController = _MainController;
        }

        public override void RemoveRemoteControlling(PeerConnection Connection)
        {
            if (InputEmulator.IsOpened()) {
                InputEmulator.Close();
            }
            UserAssignedFile?.Close();
            FirmwareLoader.Close();

            Connection.DataChannelAdded -= DataChanelAddedHandler;

        }

        private async void CommandChannelHandler(byte[] command) {
            try
            {
                Console.WriteLine("Received command {0}", ASCIIEncoding.ASCII.GetString(command));
                Console.WriteLine(InputEmulator.IsOpened());
                await InputEmulator.SendCTP_CommandAsync(JsonSerializer.Deserialize<CTP_packet>(command));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void FileChannelHandler(byte[] segment) {
            if (segment.Length == 3 && Encoding.Default.GetString(segment) == "EOF")
            {
                string file_name = UserAssignedFile.Name;
                Console.WriteLine("Received file with size: {0}", UserAssignedFile.Length);
                UserAssignedFile.Close();
                try
                {
                    Task<string> t = FirmwareLoader.RunQuartusCommandAsync($"quartus_pgm -m jtag –o \"p;{file_name}@1\"");
                    t.ContinueWith((task) => {
                        File.Delete(file_name);
                        FileCreated = false;
                    });
                }
                catch (Exception ex) {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }

            }
            else
            {
                WriteFileSegment(segment, UserAssignedFile);
            }
        }

        private void DataChanelAddedHandler(DataChannel added_dataChannel) {
            if (added_dataChannel.Label == "sendDataChannel")
            {
                added_dataChannel.MessageReceived -= CommandChannelHandler;
                added_dataChannel.MessageReceived += CommandChannelHandler;
            }
            else
            {
                if (FileCreated == false)
                {
                    Console.WriteLine("File Creation");
                    UserAssignedFile = new FileStream(added_dataChannel.Label, FileMode.Append);
                    FileCreated = true;
                }
                added_dataChannel.MessageReceived -= FileChannelHandler;
                added_dataChannel.MessageReceived += FileChannelHandler;
            }
        }

        public override void AddRemoteControlling(PeerConnection Connection)
        {
            // Main method. This method called, when arrived getRemoteMedia : true
            // This message create instance PeerConnection with config, Quartus instance 
            // and Microcontroller instance 
            Console.WriteLine("Called GetMedia which initialized UserCell in Thread {0}", Thread.CurrentThread.ManagedThreadId);
            FirmwareLoader = Quartus.GetInstance();
            InputEmulator = Microcontroller.Create();

            // Adding data channel for loading firmware and controling equipment
            Connection.DataChannelAdded += DataChanelAddedHandler;

            Console.WriteLine("End of GetMedia which initialized UserCell in Thread {0}", Thread.CurrentThread.ManagedThreadId);

            // Now we are ready to create offer and call Notyfying of signaling mechanism
        }

    }
}
