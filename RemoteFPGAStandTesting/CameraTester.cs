using NUnit.Framework;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using Microsoft.MixedReality.WebRTC;

namespace RemoteFPGAStandTesting
{
    [TestFixture]
    public class CameraTester
    {
        private int Captured_Frames { get; set; } = 0;
        private VideoTrackSource VideoSource { get; set; } = null;
        [Test]
        public void TestCapturingVideoDevice() {
            try
            {
                VideoSource = WebRTC_Remote_FPGA_stand.Camera.CreateAsync().GetAwaiter().GetResult();

                // Adding captured video frames
                VideoSource.I420AVideoFrameReady += (frame) => { Captured_Frames++; };
                Thread.Sleep(450);
                VideoSource.Dispose();
            }
            catch (Exception ex)
            {
                VideoSource?.Dispose();
                throw new AssertionException("Some low-level problems with video device");

            }
            finally {
                VideoSource?.Dispose();
            }
            Assert.AreEqual(true, Captured_Frames > 0, "Frames of video source capturing");
            Assert.Pass();
        }
    }
}
