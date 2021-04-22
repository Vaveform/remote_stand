using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Linq;

namespace WebRTC_Remote_FPGA_stand
{
    public static class ApplicationInterface
    {
        public static VideoCaptureFormat CapturingFormatSelector(IReadOnlyList<VideoCaptureFormat> VideoFormats)
        {
            int availibale_devices_count = VideoFormats.Count();
            if (VideoFormats.Count() == 0)
            {
                throw new PlatformNotSupportedException("Select object not found.");
            }
            int index = 0;
            foreach (var format in VideoFormats)
            {
                Console.WriteLine("Format #{0}: framerate = {1}, resolution = {2}x{3}", index, format.framerate, format.width, format.height);
                index++;
            }
            int Selected = 0;
            while (true)
            {
                try
                {
                    Selected = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown videoformat, try select again.");
                    continue;
                }

                if (Selected < 0 || Selected >= availibale_devices_count)
                {
                    Console.WriteLine("Unknown videoformat, try select again.");
                }
                else
                {
                    break;
                }
            }
            return VideoFormats[Selected];

        }
        public static VideoCaptureDevice VideoDeviceSelector(IReadOnlyList<VideoCaptureDevice> VideoDevices)
        {
            int availibale_devices_count = VideoDevices.Count();
            if (VideoDevices.Count() == 0)
            {
                throw new PlatformNotSupportedException("Select object not found.");
            }
            int index = 0;
            foreach (var device in VideoDevices)
            {
                Console.WriteLine("{0}: Name: {1} ID: {2}", index, device.name, device.id);
                index++;
            }
            int Selected = 0;
            while (true)
            {
                try
                {
                    Selected = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown device, try select again.");
                    continue;
                }

                if (Selected < 0 || Selected >= availibale_devices_count)
                {
                    Console.WriteLine("Unknown device, try select again.");
                }
                else
                {
                    break;
                }
            }
            return VideoDevices[Selected];
        }
    }
}
