using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.MixedReality.WebRTC;
using System.Threading;
using System.Threading.Tasks;

namespace WebRTC_Remote_FPGA_stand
{
    class Camera : IDisposable
    {
        private static VideoTrackSource VideoSource { get; set; } = null;
        private static Camera Instance { get; set; } = null;
        private static SemaphoreSlim Gate { get; set; } = new SemaphoreSlim(1);

        private Camera() { }

        static public async Task<Camera> CreateAsync()
        {
            Console.WriteLine("Calling Create Camera Method in thread {0}", Thread.CurrentThread.ManagedThreadId);
            await Gate.WaitAsync();
            if (Instance != null)
            {
                throw new SystemException("Camera instance already created and using");
            }
            Gate.Release();
            Instance = new Camera();
            VideoSource = await DeviceVideoTrackSource.CreateAsync();
            return Instance;

        }

        public List<ISystemController> mediators { get; set; }
        public VideoTrackSource source { get => VideoSource; }

        public void Close() {
            Instance?.Dispose();
        }


        public void Dispose()
        {
            VideoSource?.Dispose();
            Instance = null;
            // Here should be Notify, to notify HardwareCell - Camera free
        }
    }
}
