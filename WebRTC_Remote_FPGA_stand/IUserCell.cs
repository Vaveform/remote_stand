using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;

namespace WebRTC_Remote_FPGA_stand
{
    public abstract class IUserCell
    {
        protected ISystemController MainController { get; set; }
        abstract public void AddRemoteControlling(PeerConnection Connection);
        abstract public void RemoveRemoteControlling(PeerConnection Connection);
    }
}
