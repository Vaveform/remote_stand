using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebRTC_Remote_FPGA_stand
{
    /// <summary>
    /// Main interface of system, which provide
    /// controlling life time of WebRTC PeerConnection, Signaling Mechanism,
    /// Arduino manipulator, Quartus firmware loading and VideoSource.
    /// Implement mediator in this WebRTC FPGA remote system, which built on Mediator Pattern
    /// </summary>
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
}
