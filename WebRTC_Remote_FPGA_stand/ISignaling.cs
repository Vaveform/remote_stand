using System;
using System.Collections.Generic;
using System.Text;

namespace WebRTC_Remote_FPGA_stand
{
    public abstract class ISignaling : IDisposable
    {
        protected ISystemController MainController { get; set; }

        abstract public void Dispose();

        abstract public void SendToEndPoint(object sender, string message);

    }
}
