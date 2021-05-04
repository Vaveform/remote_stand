using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MicrocontrollerAPI;
using Microsoft.MixedReality.WebRTC;
using System.Text.Json;
using System.Threading;

namespace WebRTC_Remote_FPGA_stand
{
    public class UserCell : IUserCell
    {
        private FileStream UserAssignedFile { get; set; }
        private bool FileCreated { get; set; } = false;
        private Quartus FirmwareLoader { get; set; }
        private Microcontroller InputEmulator { get; set; }

        private static void WriteFileSegment(byte[] segment, FileStream stream)
        {
            if (stream.CanWrite)
            {
                Console.WriteLine("Writing file segment with size: {0}", segment.Length);
                stream.Write(segment);
            }
        }

        // VideoTrackSource initialized in Startup async method
        public UserCell(ISystemController _MainController)
        {
            Console.WriteLine("Working constructor of UserCell in thread {0}", Thread.CurrentThread.ManagedThreadId);
            MainController = _MainController;
        }

        public override void RemoveRemoteControlling(PeerConnection Connection)
        {
            // Removing handler of data channel adding
            Connection.DataChannelAdded -= DataChannelAddedHandler;

            // Closing opened file, destroying Arduino input emulator and Quartus firmware loader
            InputEmulator?.Close();
            UserAssignedFile?.Close();
            FirmwareLoader?.Close();
        }

        private async void CommandChannelHandler(byte[] command)
        {
            try
            {
                Console.WriteLine("Received command {0}", ASCIIEncoding.ASCII.GetString(command));
                Console.WriteLine(Microcontroller.WasOpened());
                await InputEmulator.SendCTP_CommandAsync(JsonSerializer.Deserialize<CTP_packet>(command));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void FileChannelHandler(byte[] segment)
        {
            if (segment.Length == 3 && Encoding.Default.GetString(segment) == "EOF")
            {
                string file_name = UserAssignedFile.Name;
                Console.WriteLine("Received file with size: {0}", UserAssignedFile.Length);
                UserAssignedFile.Close();
                try
                {
                    Task<string> t = FirmwareLoader.RunQuartusCommandAsync($"quartus_pgm -m jtag –o \"p;{file_name}@1\"");
                    t.ContinueWith((task) => {
                        File.Delete(file_name);
                        FileCreated = false;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }

            }
            else
            {
                WriteFileSegment(segment, UserAssignedFile);
            }
        }

        private void DataChannelAddedHandler(DataChannel added_dataChannel)
        {
            // In Artem Baskal code (client) data channel which created when client
            // send CTP command has name sendDataChannel, other name for firmware (files)
            if (added_dataChannel.Label == "sendDataChannel")
            {
                added_dataChannel.MessageReceived -= CommandChannelHandler;
                added_dataChannel.MessageReceived += CommandChannelHandler;
            }
            else
            {
                if (FileCreated == false)
                {
                    Console.WriteLine("File Creation");
                    UserAssignedFile = new FileStream(added_dataChannel.Label, FileMode.Append);
                    FileCreated = true;
                }
                added_dataChannel.MessageReceived -= FileChannelHandler;
                added_dataChannel.MessageReceived += FileChannelHandler;
            }
        }

        public override void AddRemoteControlling(PeerConnection Connection)
        {
            // Creating instance of system component to manipulate of equipment
            FirmwareLoader = Quartus.GetInstance();
            InputEmulator = Microcontroller.Create();


            // Adding data channel for loading firmware and controling equipment
            Connection.DataChannelAdded += DataChannelAddedHandler;

            //Console.WriteLine("End of GetMedia which initialized UserCell in Thread {0}", Thread.CurrentThread.ManagedThreadId);

        }

    }
}
