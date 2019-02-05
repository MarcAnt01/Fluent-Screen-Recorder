using CaptureFun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTInteropTools;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CaptureFunTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            _device = new Direct3D11Device();
            InitComposition();
        }

        private void InitComposition()
        {
            _compositor = Window.Current.Compositor;
            _visual = _compositor.CreateSpriteVisual();
            _visual.RelativeSizeAdjustment = Vector2.One;
            ElementCompositionPreview.SetElementChildVisual(this, _visual);

            _graphicsDevice = CompositionGraphics.CreateCompositionGraphicsDevice(_compositor, _device);
            _surface = _graphicsDevice.CreateDrawingSurface(
                new Size(1, 1), 
                DirectXPixelFormat.B8G8R8A8UIntNormalized, 
                DirectXAlphaMode.Premultiplied);

            _visual.Brush = _compositor.CreateSurfaceBrush(_surface);
        }

        private async Task<bool> InitAsync()
        {
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();

            if (item == null)
            {
                return false;
            }

            _frameGenerator?.Dispose();
            _frameGenerator = new CaptureFrameGenerator(_device, item, item.Size);
            return true;
        }

        private async void SnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_frameGenerator == null && !await InitAsync())
            {
                return;
            }

            using (var surfaceAndInfo = await _frameGenerator.GetNextFrameAsync())
            {
                if (surfaceAndInfo != null)
                {
                    CompositionGraphics.CopyDirect3DSurfaceIntoCompositionSurface(_device, surfaceAndInfo.Surface, _surface);
                }
                else
                {
                    await InitAsync();
                }
            }
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (AppBarToggleButton)sender;
            if (button.IsChecked.Value)
            {
                var picker = new GraphicsCapturePicker();
                var item = await picker.PickSingleItemAsync();
                if (item == null)
                {
                    button.IsChecked = false;
                    return;
                }

                var file = await PickVideoAsync();
                if (file == null)
                {
                    button.IsChecked = false;
                    return;
                }

                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                using (_encoder = new Encoder(_device, item))
                {
                    await _encoder.EncodeAsync(stream);
                }
            }
            else
            {
                _encoder?.Dispose();
            }
        }

        private async Task<StorageFile> PickVideoAsync()
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.SuggestedFileName = "recordedVideo";
            picker.DefaultFileExtension = ".mp4";
            picker.FileTypeChoices.Add("MP4 Video", new List<string> { ".mp4" });

            var file = await picker.PickSaveFileAsync();
            return file;
        }
        
        private Direct3D11Device _device;
        private CaptureFrameGenerator _frameGenerator;
        private Encoder _encoder;

        private Compositor _compositor;
        private CompositionGraphicsDevice _graphicsDevice;
        private SpriteVisual _visual;
        private CompositionDrawingSurface _surface;
    }
}
