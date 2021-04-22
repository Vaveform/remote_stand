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
        public static async Task<PeerConnection> InitializePeerConnection(ISystemController Controller)
        {
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
}
