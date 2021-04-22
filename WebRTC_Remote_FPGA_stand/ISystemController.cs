using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebRTC_Remote_FPGA_stand
{
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
