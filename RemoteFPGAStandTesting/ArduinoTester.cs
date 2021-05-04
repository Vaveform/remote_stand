using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace RemoteFPGAStandTesting
{
    [TestFixture]
    public class ArduinoTester
    {
        private const byte ExpectedResult = 200;
        [Test]
        public void SetLowLevels()
        {
            MicrocontrollerAPI.Microcontroller Arduino = null;
            try
            {
                Arduino = MicrocontrollerAPI.Microcontroller.Create();
                MicrocontrollerAPI.CTP_packet command = new MicrocontrollerAPI.CTP_packet();
                command.command_type = (byte)MicrocontrollerAPI.CommandType.SetLowLevel;
                foreach (var element in MicrocontrollerAPI.Ids_Usings_Pins.Using_Digital_GPIO_Arduino_Outputs)
                {
                    command.pin_number = (byte)element.Value;
                    Assert.AreEqual(ExpectedResult, Arduino.SendCTP_Command(command), $"Setting low level on pin {element.Value}");
                }

            }
            catch (Exception ex)
            {
                Arduino?.Close();
                throw new AssertionException($"In SetLowLevels test method caused error by exception, reason: {ex.Message}");
            }
            finally
            {
                Arduino?.Close();
            }
        }
        [Test]
        public void SetHighLevels()
        {
            MicrocontrollerAPI.Microcontroller Arduino = null;
            try
            {
                Arduino = MicrocontrollerAPI.Microcontroller.Create();
                MicrocontrollerAPI.CTP_packet command = new MicrocontrollerAPI.CTP_packet();
                command.command_type = (byte)MicrocontrollerAPI.CommandType.SetHighLevel;
                foreach (var element in MicrocontrollerAPI.Ids_Usings_Pins.Using_Digital_GPIO_Arduino_Outputs)
                {
                    command.pin_number = (byte)element.Value;
                    Assert.AreEqual(ExpectedResult, Arduino.SendCTP_Command(command), $"Setting high level on pin {element.Value}");
                }
            }
            catch (Exception ex)
            {
                Arduino?.Close();
                throw new AssertionException($"In SetHighLevels test method caused error by exception, reason: {ex.Message}");
            }
            finally
            {
                Arduino?.Close();
            }
        }
        [Test]
        public void SetPWMSignals()
        {
            MicrocontrollerAPI.Microcontroller Arduino = null;
            try
            {
                // Testing connection
                Arduino = MicrocontrollerAPI.Microcontroller.Create();
                MicrocontrollerAPI.CTP_packet command = new MicrocontrollerAPI.CTP_packet();
                command.command_type = (byte)MicrocontrollerAPI.CommandType.SetPWMSignal;
                // Testing command sending and CTP
                foreach (var element in MicrocontrollerAPI.Ids_Usings_Pins.Available_PWM_Pins)
                {
                    command.pin_number = (byte)element;
                    command.duty = 30;
                    command.frequency = 2500;
                    Assert.AreEqual(ExpectedResult, Arduino.SendCTP_Command(command), $"Setting PWM signal on pin {element} with duty 30");
                }
                foreach (var element in MicrocontrollerAPI.Ids_Usings_Pins.Available_PWM_Pins)
                {
                    command.pin_number = (byte)element;
                    command.duty = 150;
                    command.frequency = 4500;
                    Assert.AreEqual(ExpectedResult, Arduino.SendCTP_Command(command), $"Setting PWM signal on pin {element} with duty 150");
                }
            }
            catch (Exception ex)
            {
                Arduino?.Close();
                throw new AssertionException($"In PWM signal test method caused error by exception, reason: {ex.Message}");
            }
            finally
            {
                Arduino?.Close();
            }
        }
    }
}
