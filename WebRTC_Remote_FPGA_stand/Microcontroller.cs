using System;
using System.IO.Ports;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicrocontrollerAPI
{
    [System.Flags]
    public enum CommandType : byte
    {
        GetSignalLevel = 0b_0000_0001,
        SetLowLevel = 0b_0000_0010,
        SetHighLevel = 0b_0000_0011,
        SetPWMSignal = 0b_0000_0100
    }
    /// <summary>
    /// Available pins of Arduino specified in documentation
    /// </summary>
    static public class Ids_Usings_Pins
    {
        // input GPIO_0[0] on FPGA = LEDDR[0] - output pin number Arduino 11 - PWM
        // input GPIO_0[2] on FPGA = LEDDR[1] - output pin number Arduino 10 - PWM
        // input GPIO_0[4] on FPGA = LEDDR[2] - output pin number Arduino 9 - PWM
        // input GPIO_0[6] on FPGA = LEDDR[3] - output pin number Arduino 8
        // input GPIO_0[10] on FPGA = LEDDR[4] - output pin number Arduino 5 - PWM - in current version here error
        // input GPIO_0[12] on FPGA = LEDDR[5] - output pin number Arduino 4
        // input GPIO_0[14] on FPGA = LEDDR[6] - output pin number Arduino 3 - PWM
        // input GPIO_0[16] on FPGA = LEDDR[7] - output pin number Arduino 2
        static public readonly Dictionary<int, int> Using_Digital_GPIO_Arduino_Outputs = new Dictionary<int, int>{
            {0, 11},
            {1, 10},
            {2, 9},
            {3, 8},
            {4, 5},
            {5, 4},
            {6, 3},
            {7, 2}
        };

        static public readonly List<int> Available_PWM_Pins = new List<int> { 11, 10, 9, 3 };
    }
    /// <summary>
    /// Command Transfer Protocol (CTP) Packet structure 
    /// </summary>
    public struct CTP_packet
    {
        /// <summary>
        /// 1 - Give signal level on pin in any case (PWM or Digital)
        /// 2 - Set on pin low level signal (if earlier was set PWM, PWM stopped)
        /// 3 - Set on pin high level signal (if earlier was set PWM, PWM stopped)
        /// 4 - Set on special pins (3 - button_2 FPGA, 5 - button_4 FPGA, 9 - button_6 FPGA, 10 - button_7 FPGA, 11 - button_8 FPGA) PWM signal
        /// </summary>
        public byte command_type { set; get; }
        /// <summary>
        /// GPIO pin numbers: 2,3,4,5,8,9,10,11
        /// </summary>
        public byte pin_number { set; get; }
        /// <summary>
        /// If set command = 4, should be defined duty (0 - 255)
        /// </summary>
        public byte duty { set; get; }
        /// <summary>
        /// If set command = 4, should be defined frequency (250 - 2000'000 Herz)
        /// </summary>
        public uint frequency { set; get; }
    }
    public class Microcontroller : IDisposable
    {
        private static SerialPort connection { get; set; }
        private static CTP_packet command;
        private static Microcontroller instance;

        static public byte[] CommandStruct_to_Bytes(CTP_packet command)
        {
            byte[] arr = new byte[Marshal.SizeOf(command)];
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(command));
            Marshal.StructureToPtr(command, ptr, true);
            Marshal.Copy(ptr, arr, 0, Marshal.SizeOf(command));
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        static private bool Check_phisically_connected()
        {
            command = new CTP_packet();
            const int number_of_ports_to_check = 6;
            bool is_connected = false;
            byte[] buffer = new byte[1];
            // Expected response - Unxepected pin
            command.command_type = 1;
            command.pin_number = 255;
            command.duty = 0;
            command.frequency = 0;
            for (int port_number = number_of_ports_to_check; port_number > 0; port_number--)
            {
                connection.PortName = "COM" + port_number.ToString();
                try
                {
                    connection.Open();
                    connection.Write(CommandStruct_to_Bytes(command), 0, Marshal.SizeOf(command) - 1);
                    connection.Read(buffer, 0, 1);
                    Console.WriteLine("{0} : {1}", connection.PortName, buffer[0]);
                }
                catch (Exception)
                {
                    connection.Close();
                    continue;
                }
                //Console.WriteLine($"Comparison result: {string.Compare(response, expected_response)}");
                if (Convert.ToInt32(buffer[0]) == 101)
                {
                    //Console.WriteLine("Arduino connected!!!");
                    is_connected = true;
                    break;
                }
                connection.Close();
            }
            return is_connected;

        }
        private Microcontroller()
        {
            connection = new System.IO.Ports.SerialPort();
            connection.BaudRate = 9600;
            connection.Parity = System.IO.Ports.Parity.None;
            connection.StopBits = System.IO.Ports.StopBits.One;
            connection.DataBits = 8;
            // Preventing TimeOut exception - we set 100 seconds
            connection.ReadTimeout = 1000;

            //Check_phisically_connected();
            //Console.WriteLine("Here building and connected Arduino");
            if (Check_phisically_connected() == false)
            {
                //Console.WriteLine("Microcontroller device not found.");
                throw new Exception("Microcontroller device not found.");
            }
            else
            {
                Console.WriteLine("Arduino connection opened!!!");
            }
        }

        static public Microcontroller Create()
        {
            if (instance == null)
            {
                instance = new Microcontroller();
            }

            return instance;
        }

        public byte SendCTP_Command(CTP_packet _command)
        {
            if (connection.IsOpen == false)
            {
                return Convert.ToByte(0);
            }
            Console.WriteLine("Sending");
            connection.Write(CommandStruct_to_Bytes(_command), 0, Marshal.SizeOf(_command) - 1);
            byte[] buffer = new byte[1];
            try
            {
                connection.Read(buffer, 0, 1);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message);
                return 0;
            };
            //Console.WriteLine($"Recieved after command: {Convert.ToInt32(buffer[0])}");
            Console.WriteLine($"Recieved after command: {Convert.ToInt32(buffer[0])}");
            return buffer[0];
        }
        public Task<byte> SendCTP_CommandAsync(CTP_packet _command)
        {
            return Task.Run(() => SendCTP_Command(_command));
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Console.WriteLine("Close Serial Port Connection by Disposable");
            if (instance != null)
            {
                instance = null;
                connection?.Close();
            }
        }

        public static bool WasOpened() {
            return instance != null;
        }
    }
}
