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
            if (InputEmulator.IsOpened())
            {
                InputEmulator.Close();
            }
            UserAssignedFile?.Close();
            FirmwareLoader.Close();

            Connection.DataChannelAdded -= DataChanelAddedHandler;

        }

        private async void CommandChannelHandler(byte[] command)
        {
            try
            {
                Console.WriteLine("Received command {0}", ASCIIEncoding.ASCII.GetString(command));
                Console.WriteLine(InputEmulator.IsOpened());
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

        private void DataChanelAddedHandler(DataChannel added_dataChannel)
        {
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
            // Main method. This method called, when arrived getRemoteMedia : true
            // This message create instance PeerConnection with config, Quartus instance 
            // and Microcontroller instance 
            Console.WriteLine("Called GetMedia which initialized UserCell in Thread {0}", Thread.CurrentThread.ManagedThreadId);
            FirmwareLoader = Quartus.GetInstance();
            InputEmulator = Microcontroller.Create();

            // Adding data channel for loading firmware and controling equipment
            Connection.DataChannelAdded += DataChanelAddedHandler;

            Console.WriteLine("End of GetMedia which initialized UserCell in Thread {0}", Thread.CurrentThread.ManagedThreadId);

            // Now we are ready to create offer and call Notyfying of signaling mechanism
        }

    }
}
