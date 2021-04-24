using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;

namespace WebRTC_Remote_FPGA_stand
{
    public class WebSocketSignaling : ISignaling
    {
        protected WebSocket Socket { get; set; }
        public WebSocketSignaling(ISystemController _MainController)
        {
            MainController = _MainController;
            Socket = new WebSocket(SystemConfiguration.SignalingURL, SystemConfiguration.WebSocketTokens);
            Socket.Connect();
            Socket.OnError += (sender, message) =>
            {
                MainController.SignalingClosed(Socket, message.Message);
            };
            Socket.OnClose += (sender, message) =>
            {
                MainController.SignalingClosed(Socket, message.Reason);
            };

            Socket.OnMessage += async (sender, message) =>
            {
                await MainController.NotifyPeerConnection(Socket, typeof(string), message.Data);
            };

            Console.WriteLine("Constructed Signaling");

        }

        public override void SendToEndPoint(object sender, string message)
        {
            Socket.Send(message);
        }

        public override void Dispose()
        {
            if (Socket.IsAlive == true)
            {
                Socket.Close();
            }
        }

        public override bool IsAlive()
        {
            return Socket.IsAlive;
        }
    }
}
