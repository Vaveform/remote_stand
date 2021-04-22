using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Linq;

namespace WebRTC_Remote_FPGA_stand
{
    public static class SystemConfiguration
    {
        private static IEnumerable<IceServer> LoadFromConfigAllSTUN()
        {
            Regex stun_regex = new Regex(@"[S,s][t, T][u, U][n, N]");
            // Return all stun servers from config
            // For stun server you only need url
            return ConfigurationManager.AppSettings.AllKeys.
                Where((key) => stun_regex.IsMatch(key)).
                Select((key) => new IceServer { Urls = { ConfigurationManager.AppSettings.Get(key) } });
        }
        private static string ReadTurnParametr(string value, string pattern)
        {
            int begin = value.IndexOf(pattern) + pattern.Length + 1;
            if (begin == -1)
            {
                return "";
            }
            int end = value.IndexOf(" ", begin);
            if (end == -1)
            {
                return value.Substring(begin, value.Length - begin);
            }
            return value.Substring(begin, end - begin);
        }
        private static IEnumerable<IceServer> LoadFromConfigAllTURN()
        {
            Regex turn_regex = new Regex(@"[T,t][u, U][r, R][n, N]");
            // Adding all turn servers. To use turn servers you should 
            // add to WebRTC_Remote_FPGA_stand.dll.config 
            // TurnPassword and UserName
            return ConfigurationManager.AppSettings.AllKeys.Where((key) => turn_regex.IsMatch(key)).Select((key) => {
                IceServer turnServer = new IceServer();
                string Value = ConfigurationManager.AppSettings.Get(key);
                turnServer.Urls.Add(ReadTurnParametr(Value, "Url"));
                turnServer.TurnUserName = ReadTurnParametr(Value, "UserName");
                turnServer.TurnPassword = ReadTurnParametr(Value, "Password");
                return turnServer;
            });
        }
        private static string[] LoadWebSocketTokens() {
            Regex websockettokens_regex = new Regex(@"WebSocketToken");
            return ConfigurationManager.AppSettings.AllKeys.
                Where((key) => websockettokens_regex.IsMatch(key)).
                Select((key) => ConfigurationManager.AppSettings.Get(key)).
                ToArray();
        }
        private static PeerConnectionConfiguration CreateSetting()
        {

            // Here the main settings of PeerConnection object
            // If you want to someone change in settings of PeerConnection objects
            // You can make it here
            PeerConnectionConfiguration setting = new PeerConnectionConfiguration();
            setting.IceServers.AddRange(LoadFromConfigAllSTUN());
            setting.IceServers.AddRange(LoadFromConfigAllTURN());
            //Console.WriteLine("Creating PeerConnection");
            return setting;
        }

        // Loading one time, when one of the fields of static class calling
        public static PeerConnectionConfiguration PeerConnectionSettings { get; } = CreateSetting();

        // URL of signaling mechanism
        public static string SignalingURL { get; } = ConfigurationManager.AppSettings.Get("SignalingMechanismURL");
        public static LocalVideoDeviceInitConfig VideoDeviceSettings {get; set;} = new LocalVideoDeviceInitConfig();
        public static string[] WebSocketTokens { get; } = LoadWebSocketTokens();

    }
}
