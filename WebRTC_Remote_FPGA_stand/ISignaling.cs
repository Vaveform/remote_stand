using System;
using System.Collections.Generic;
using System.Text;

namespace WebRTC_Remote_FPGA_stand
{
    /// <summary>
    /// Class, which implement abstract Signaling mechanism in
    /// WebRTC standard. In WebRTC standard Signaling mechanism can be 
    /// anything: from WebSocket server to Http server
    /// </summary>
    public abstract class ISignaling : IDisposable
    {
        protected ISystemController MainController { get; set; }

        abstract public void Dispose();

        abstract public void SendToEndPoint(object sender, string message);
        abstract public bool IsAlive();

    }
}
