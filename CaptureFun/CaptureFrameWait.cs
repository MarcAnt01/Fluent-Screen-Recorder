using Microsoft.Graphics.Canvas;
using System;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

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

    public sealed class CaptureFrameWait : IDisposable
    {
        public CaptureFrameWait(
            IDirect3DDevice device,
            GraphicsCaptureItem item,
            SizeInt32 size)
        {
            MakeCopy = !ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8);

            _device = CanvasDevice.CreateFromDirect3D11Device(device);
            _item = item;
            _event = new ManualResetEvent(false);
            _completed = false;

            InitializeCapture(size);
        }

        private void InitializeCapture(SizeInt32 size)
        {
            _item.Closed += OnClosed;
            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                size);
            _framePool.FrameArrived += OnFrameArrived;
            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        private void SetResult(Direct3D11CaptureFrame frame)
        {
            if (!_completed)
            {
                _currentFrame = frame;
                _completed = _currentFrame == null;
            }
            
            _event.Set();
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            SetResult(sender.TryGetNextFrame());
        }

        private void OnClosed(GraphicsCaptureItem sender, object args)
        {
            SetResult(null);
        }

        public SurfaceWithInfo WaitForNewFrame()
        {
            if (_completed)
            {
                return null;
            }

            // Let's get a fresh one.
            _currentFrame?.Dispose();
            _event.Reset();

            if (_completed || !_event.WaitOne() || _currentFrame == null)
            {
                return null;
            }

            var result = new SurfaceWithInfo();
            if (MakeCopy)
            {
                var copyBitmap = new CanvasRenderTarget(_device, _currentFrame.Surface.Description.Width, _currentFrame.Surface.Description.Height, 96);
                using (var sourceFrame = CanvasBitmap.CreateFromDirect3D11Surface(_device, _currentFrame.Surface))
                {
                    copyBitmap.CopyPixelsFromBitmap(sourceFrame);
                    result.Surface = copyBitmap;
                }
            }
            else
            {
                result.Surface = _currentFrame.Surface;
            }

            result.SystemRelativeTime = _currentFrame.SystemRelativeTime;

            return result;
        }

        public void Dispose()
        {
            SetResult(null);
            _framePool?.Dispose();
            _session?.Dispose();
            if (_item != null)
            {
                _item.Closed -= OnClosed;
            }
            _item = null;
            _device?.Dispose();
            _currentFrame?.Dispose();
        }

        private CanvasDevice _device;
        private ManualResetEvent _event;
        private Direct3D11CaptureFrame _currentFrame;
        private bool _completed;

        private GraphicsCaptureItem _item;
        private GraphicsCaptureSession _session;
        private Direct3D11CaptureFramePool _framePool;

        private readonly bool MakeCopy;
    }
}
