using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRTInteropTools;

namespace CaptureFun
{
    public sealed class SurfaceWithInfo : IDisposable
    {
        public IDirect3DSurface Surface { get; internal set; }
        public TimeSpan SystemRelativeTime { get; internal set; }

        public void Dispose()
        {
            Surface?.Dispose();
            Surface = null;
        }
    }


    public sealed class CaptureFrameGenerator : IDisposable
    {
        public CaptureFrameGenerator(
            IDirect3DDevice device, 
            GraphicsCaptureItem item, 
            SizeInt32 size)
        {
            _device = Direct3D11Device.CreateFromDirect3D11Device(device);
            _item = item;
            _size = size;
            _frameTask = new TaskCompletionSource<Direct3D11CaptureFrame>();

            InitializeCapture();
        }

        private void InitializeCapture()
        {
            _item.Closed += OnClosed;
            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                _size);
            _framePool.FrameArrived += OnFrameArrived;
            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        private void OnClosed(GraphicsCaptureItem sender, object args)
        {
            _frameTask.SetResult(null);
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var frame = sender.TryGetNextFrame();
            _frameTask.SetResult(frame);
        }

        private async Task<SurfaceWithInfo> GetNextFrameInternalAsync()
        {
            _currentFrame?.Dispose();
            _currentFrame = await _frameTask.Task;
            if (_currentFrame == null)
            {
                Dispose();
                return null;
            }
            _frameTask = new TaskCompletionSource<Direct3D11CaptureFrame>();

            SurfaceWithInfo result = new SurfaceWithInfo();
            using (var multithread = _device.Multithread)
            using (var lockSession = multithread.Lock())
            using (var texture = Direct3D11Texture2D.CreateFromDirect3DSurface(_currentFrame.Surface))
            using (var context = _device.ImmediateContext)
            {
                var desc = texture.Description2D;
                desc.Usage = Direct3DUsage.Default;
                desc.BindFlags = 0;
                desc.CpuAccessFlags = 0;
                desc.MiscFlags = 0;
                result.Surface = _device.CreateTexture2D(desc);
                result.SystemRelativeTime = _currentFrame.SystemRelativeTime;
                context.CopyResource(result.Surface, texture);
            }
            return result;
        }

        public IAsyncOperation<SurfaceWithInfo> GetNextFrameAsync()
        {
            return GetNextFrameInternalAsync().AsAsyncOperation();
        }

        public void Dispose()
        {
            _framePool?.Dispose();
            _session?.Dispose();
            _item.Closed -= OnClosed;
            _item = null;
            _device?.Dispose();
            _currentFrame?.Dispose();
        }

        private Direct3D11Device _device;
        private TaskCompletionSource<Direct3D11CaptureFrame> _frameTask;
        private Direct3D11CaptureFrame _currentFrame;

        private GraphicsCaptureItem _item;
        private GraphicsCaptureSession _session;
        private Direct3D11CaptureFramePool _framePool;
        private SizeInt32 _size;
    }
}
