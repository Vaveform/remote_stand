using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RemoteFPGAStandTesting
{
    [TestFixture]
    public class QuartusTester
    {
        [Test]
        public void QuartusProcessCreation()
        {
            WebRTC_Remote_FPGA_stand.Quartus quartus = null;
            try
            {
                quartus = WebRTC_Remote_FPGA_stand.Quartus.GetInstance();
            }
            catch (Exception ex)
            {
                throw new AssertionException($"Failed to create quartus, reason: {ex.Message}");
            }
            finally
            {
                quartus?.Close();
            }
        }
        [Test]
        public void LoadingFirmware()
        {
            WebRTC_Remote_FPGA_stand.Quartus quartus = null;
            try
            {
                quartus = WebRTC_Remote_FPGA_stand.Quartus.GetInstance();
                // Running quartus programmer to load firmware
                string QuartusOutput = quartus.RunQuartusCommand($"quartus_pgm -m jtag –o \"p;Quartus_test_file.sof@1\"");
                Console.WriteLine(QuartusOutput);
                bool IsSuccessful = QuartusOutput.IndexOf("Quartus Prime Programmer was successful") != -1;
                Assert.AreEqual(true, IsSuccessful, "Loading Quartus_test_file.sof");
            }
            catch (Exception ex)
            {
                throw new AssertionException($"Failed to create quartus, reason: {ex.Message}");
            }
            finally
            {
                quartus?.Close();
            }
        }
    }
}
