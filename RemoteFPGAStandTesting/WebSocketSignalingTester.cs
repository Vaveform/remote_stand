using NUnit.Framework;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;

namespace RemoteFPGAStandTesting
{
    [TestFixture]
    public class WebSocketSignalingTester
    {
        private string Correct_Message { get; set; } = "Hello world!!!";
        private WebRTC_Remote_FPGA_stand.WebSocketSignaling Tested_Object { get; set; }
        private string url_of_signaling {get; set;}
        private string[] tokens_to_signaling { get; set; }
        [SetUp]
        public void Setup()
        {
            // Opening system configuration - file, which can be modified by user
            Configuration OpenedConfiguration = ConfigurationManager.OpenExeConfiguration("WebRTC_Remote_FPGA_stand.dll");
            // Parsing signaling server Url
            url_of_signaling = OpenedConfiguration.AppSettings.Settings["SignalingMechanismURL"].Value;

            // Parsing WebSocket tokens from configuration file, using LINQ
            Regex websockettokens_regex = new Regex(@"WebSocketToken");
            tokens_to_signaling = OpenedConfiguration.AppSettings.Settings.AllKeys.
                Where((key) => websockettokens_regex.IsMatch(key)).
                Select((key) => OpenedConfiguration.AppSettings.Settings[key].Value).
                ToArray();
            // Creating WebSocketSignaling object, which will be tested later
            Tested_Object = new WebRTC_Remote_FPGA_stand.WebSocketSignaling(url_of_signaling, tokens_to_signaling);
        }

        [Test]
        public void TestEchoSignalingServer()
        {
            // Modeling situtaion, when connected client send some message through the signaling mechanism
            WebSocketSharp.WebSocket receiver = new WebSocketSharp.WebSocket(url_of_signaling, tokens_to_signaling);
            receiver.Connect();
            receiver.Send(Correct_Message);
            // Time to receive result by Testing object
            Thread.Sleep(450);

            receiver.Close();
            string TestResult = Tested_Object.LastReceivedMessage;
            Tested_Object.Dispose();

            //Console.WriteLine(TestResult);
            //Console.WriteLine(Tested_Object.LastReceivedMessage);
            //string TestResult2 = TestResult;
            //TestResult2 += "aaa";
            //Console.WriteLine(TestResult2);
            //output: Hello world!!!aaa, but TestResult: Hello world!!! 
            // and string referece type, where here is problem? String class have value semantic.

            Assert.AreEqual(Correct_Message, TestResult);
            Assert.Pass();
        }
    }
}