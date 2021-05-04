using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;

namespace WebRTC_Remote_FPGA_stand
{
    /// <summary>
    /// Concrete implementation of signaling mechanism
    /// based on WebSocket from WebSocketSharp nuget
    /// and derived from ISignaling abstract class
    /// </summary>
    public class WebSocketSignaling : ISignaling
    {
        protected WebSocket Socket { get; set; }
        protected string Url { get; set; }
        protected string[] Tokens { get; set; }

        public string LastReceivedMessage { get; private set; }

        /// <summary>
        /// General contructor, which do not bind
        /// WebSocket events with ISystemCotroller.
        /// When you use this constructor see LastReceivedMessage
        /// where saved status of last WebSocket connection status or last message
        /// </summary>
        /// <param name="url"></param>
        /// <param name="tokens"></param>
        public WebSocketSignaling(string url, string[] tokens) {
            Url = url;
            Tokens = tokens;
            Socket = new WebSocket(Url, Tokens);
            Socket.Connect();

            Socket.OnError += (sender, message) => { LastReceivedMessage = "Error"; };
            Socket.OnClose += (sender, message) => { LastReceivedMessage = "Closed"; };
            Socket.OnMessage += (sender, message) => { LastReceivedMessage = message.Data; };
        }


        /// <summary>
        /// Main constructor which using
        /// for binding events of Websocket
        /// object with methods of ISystemController interface object (mediator).
        /// </summary>
        /// <param name="_MainController"></param>
        /// <param name="url"></param>
        /// <param name="tokens"></param>
        public WebSocketSignaling(ISystemController _MainController, string url, string[] tokens) {
            // Adding reference to Controller.
            // It allows to call methods of interface ISystemCotroller (mediator)
            // to do something (depends of event, which occure on WebSocket object). 
            // Concrete implementation of methods ISystemCotroller
            // in SystemCotroller, object of which passed in SystemController code (using keyword this)
            MainController = _MainController;
            // Building WebSocket object.
            // It is main object through which we send and receive data from signaling server
            Url = SystemConfiguration.SignalingURL;
            Tokens = SystemConfiguration.WebSocketTokens;
            Socket = new WebSocket(Url, Tokens);
            Socket.Connect();
            Socket.OnError += (sender, message) =>
            {
                // Calling method of mediator to notify all system that the 
                // WebSocket signaling mechanism object had error and should be closed by mediator
                MainController.SignalingClosed(Socket, message.Message);
            };
            Socket.OnClose += (sender, message) =>
            {
                // Calling method of mediator to notify all system that the 
                // WebSocket signaling mechanism object was closed and should be disposed by mediator
                MainController.SignalingClosed(Socket, message.Reason);
            };

            Socket.OnMessage += async (sender, message) =>
            {
                // Calling method of mediator to notify all system that the 
                // WebSocket signaling mechanism received message (it can be ICE candidate or SDP message).
                // WebSocket signaling calls method of mediator with parametr of this message to
                // Add ICE or SDP to (WebRTC)PeerConnection of SystemController (ISystemController)
                await MainController.NotifyPeerConnection(Socket, typeof(string), message.Data);
            };

            Console.WriteLine("Constructed Signaling");
        }

        /// <summary>
        /// This is overriden method of abstract class 'ISignaling'. Simply calls method 'Send' of WebSocket object
        /// if this object not null
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public override void SendToEndPoint(object sender, string message)
        {
            Socket?.Send(message);
        }

        /// <summary>
        /// Close WebSocket object, if thos object not null. This is overriden method
        /// of abstract class 'ISignaling'
        /// </summary>
        public override void Dispose()
        {
            Socket?.Close();
        }

        /// <summary>
        /// Check if WebSocket object connection is opened and works.
        /// It is overriden method of abstract class 'ISignaling'.
        /// </summary>
        /// <returns></returns>
        public override bool IsAlive()
        {
            return Socket.IsAlive;
        }
    }
}
