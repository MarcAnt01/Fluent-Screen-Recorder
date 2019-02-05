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
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;
using WinRTInteropTools;

namespace CaptureFun
{
    public sealed class Encoder : IDisposable
    {
        public Encoder(IDirect3DDevice device, GraphicsCaptureItem item)
        {
            _device = Direct3D11Device.CreateFromDirect3D11Device(device);
            _deviceContext = _device.ImmediateContext;
            _multiThread = _device.Multithread;
            _captureItem = item;
            _isRecording = false;

            CreateMediaObjects();
        }

        public IAsyncAction EncodeAsync(IRandomAccessStream stream)
        {
            return EncodeInternalAsync(stream).AsAsyncAction();
        }

        private async Task EncodeInternalAsync(IRandomAccessStream stream)
        {
            if (!_isRecording)
            {
                _isRecording = true;

                _frameGenerator = new CaptureFrameWait(
                    _device,
                    _captureItem,
                    _captureItem.Size);

                try
                {
                    var encodingProfile = new MediaEncodingProfile();
                    encodingProfile.Container.Subtype = "MPEG4";
                    encodingProfile.Video.Subtype = "H264";
                    encodingProfile.Video.Width = (uint)_captureItem.Size.Width;
                    encodingProfile.Video.Height = (uint)_captureItem.Size.Height;
                    encodingProfile.Video.Bitrate = _videoDescriptor.EncodingProperties.Bitrate;
                    encodingProfile.Video.FrameRate.Numerator = c_frameRateN;
                    encodingProfile.Video.FrameRate.Denominator = 1;
                    encodingProfile.Video.PixelAspectRatio.Numerator = 1;
                    encodingProfile.Video.PixelAspectRatio.Denominator = 1;
                    var transcode = await _transcoder.PrepareMediaStreamSourceTranscodeAsync(_mediaStreamSource, stream, encodingProfile);

                    await transcode.TranscodeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        public void Dispose()
        {
            if (_closed)
            {
                return;
            }
            _closed = true;

            if (!_isRecording)
            {
                DisposeInternal();
            }

            _isRecording = false;            
        }

        private void DisposeInternal()
        {
            _device.Dispose();
            _deviceContext.Dispose();
            _multiThread.Dispose();
            _frameGenerator.Dispose();
        }

        private void CreateMediaObjects()
        {
            // Create our encoding profile based on the size of the item
            // TODO: This only really makes sense for monitors, we need
            //       to change this to make sense in all cases.
            int width = _captureItem.Size.Width;
            int height = _captureItem.Size.Height;

            // Describe our input: uncompressed BGRA8 buffers comming in at the monitor's refresh rate
            // TODO: We pick 60Hz here because it applies to most monitors. However this should be
            //       more robust.
            var videoProperties = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, (uint)width, (uint)height);
            _videoDescriptor = new VideoStreamDescriptor(videoProperties);
            _videoDescriptor.EncodingProperties.FrameRate.Numerator = c_frameRateN;
            _videoDescriptor.EncodingProperties.FrameRate.Denominator = c_frameRateD;
            _videoDescriptor.EncodingProperties.Bitrate = (uint)(c_frameRateN * c_frameRateD * width * height * 4);

            // Create our MediaStreamSource
            _mediaStreamSource = new MediaStreamSource(_videoDescriptor);
            _mediaStreamSource.BufferTime = TimeSpan.FromSeconds(0);
            _mediaStreamSource.Starting += OnMediaStreamSourceStarting;
            _mediaStreamSource.SampleRequested += OnMediaStreamSourceSampleRequested;

            // Create our device manager
            _mediaGraphicsDevice = MediaGraphicsDevice.CreateFromMediaStreamSource(_mediaStreamSource);
            _mediaGraphicsDevice.RenderingDevice = _device;

            // Create our transcoder
            _transcoder = new MediaTranscoder();
            _transcoder.HardwareAccelerationEnabled = true;
        }

        private void OnMediaStreamSourceSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            if (_isRecording && !_closed)
            {
                try
                {
                    using (var frame = _frameGenerator.WaitForNewFrame())
                    using (var lockSession = _multiThread.Lock())
                    {
                        if (frame == null)
                        {
                            args.Request.Sample = null;
                            DisposeInternal();
                            return;
                        }

                        var timeStamp = frame.SystemRelativeTime;

                        var sample = MediaStreamSample.CreateFromDirect3D11Surface(frame.Surface, timeStamp);
                        args.Request.Sample = sample;                       
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    System.Diagnostics.Debug.WriteLine(e);
                    args.Request.Sample = null;
                    DisposeInternal();
                }
            }
            else
            {
                args.Request.Sample = null;
                DisposeInternal();
            }
        }

        private void OnMediaStreamSourceStarting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            using (var frame = _frameGenerator.WaitForNewFrame())
            {
                args.Request.SetActualStartPosition(frame.SystemRelativeTime);
            }
        }

        private Direct3D11Device _device;
        private Direct3D11DeviceContext _deviceContext;
        private Direct3D11Multithread _multiThread;
        private MediaGraphicsDevice _mediaGraphicsDevice;

        private GraphicsCaptureItem _captureItem;
        private CaptureFrameWait _frameGenerator;

        private VideoStreamDescriptor _videoDescriptor;
        private MediaStreamSource _mediaStreamSource;
        private MediaTranscoder _transcoder;
        private bool _isRecording;
        private bool _closed = false;

        // Constants
        private const int c_frameRateN = 60;
        private const int c_frameRateD = 1;
    }
}
