using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Threading;
using System.Threading.Tasks;

namespace WebRTC_Remote_FPGA_stand
{
    public static class Camera
    {
        private static VideoTrackSource VideoSource { get; set; } = null;

        // Semaphore allows to block code for n Threads
        private static SemaphoreSlim Gate { get; set; } = new SemaphoreSlim(1, 1);

        static public async Task<VideoTrackSource> CreateAsync(LocalVideoDeviceInitConfig config = null)
        {
            Console.WriteLine("Calling Create Camera Method in thread {0}", Thread.CurrentThread.ManagedThreadId);

            await Gate.WaitAsync();
            if (VideoSource != null)
            {
                throw new SystemException("Camera instance already created and using");
            }
            Gate.Release();
            VideoSource = await DeviceVideoTrackSource.CreateAsync(config);
            return VideoSource;

        }
    }
}
