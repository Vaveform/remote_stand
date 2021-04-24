using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Threading.Tasks;
using System.Threading;

namespace WebRTC_Remote_FPGA_stand
{
    public static class WebRTCPeerCreator
    {
        public static PeerConnection BindPeerWithController(PeerConnection connection, ISystemController controller)
        {

            connection.Connected += () =>
            {
                controller.PeerConnected(connection, "New user connected");
            };


            connection.IceCandidateReadytoSend += (candidate) =>
            {
                Console.WriteLine("Candidate to send: {0}", candidate.ToABJson());
                controller.NotifySignaling(connection, typeof(IceCandidate), candidate.ToABJson());
            };

            connection.LocalSdpReadytoSend += (sdp) =>
            {
                Console.WriteLine("SDP to send: {0}", sdp.ToABJson());
                controller.NotifySignaling(connection, typeof(SdpMessage), sdp.ToABJson());
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
                    Console.WriteLine("Connection was closed, call Controller.PeerConnectionClosed");
                    await controller.PeerConnectionClosed(connection, "ICE state change to closed");
                }
                if (state == IceConnectionState.Disconnected)
                {
                    controller.PeerConnectionDisconnected(connection, "ICE state change to disconnected");
                }
            };
            return connection;
        }
        public static PeerConnection AddVideoTransceiver(PeerConnection connection, VideoTrackSource source)
        {
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
        public static async Task<PeerConnection> InitializePeerConnection()
        {
            Console.WriteLine("Initializing peer connection in thread: {0}", Thread.CurrentThread.ManagedThreadId);
            PeerConnection connection = new PeerConnection();


            await connection.InitializeAsync(SystemConfiguration.PeerConnectionSettings);


            return connection;
        }
    }
}
