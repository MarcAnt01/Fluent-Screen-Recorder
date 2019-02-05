using CaptureFun;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using WinRTInteropTools;

namespace CaptureFunTestApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            _device = new Direct3D11Device();
        }

        private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;

            // Get our capture item
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();
            if (item == null)
            {
                button.IsChecked = false;
                return;
            }

            // Find a place to put our vidoe for now
            var file = await GetTempFileAsync();

            // Tell the user we've started recording
            MainTextBlock.Text = "recording...";
            MainProgressBar.IsIndeterminate = true;

            // Kick off the encoding
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            using (_encoder = new Encoder(_device, item))
            {
                await _encoder.EncodeAsync(stream);
            }

            // At this point the encoding has finished,
            // tell the user we're now saving
            MainTextBlock.Text = "saving...";

            // Ask the user where they'd like the video to live
            var newFile = await PickVideoAsync();
            if (newFile == null)
            {
                // User decided they didn't want it
                // Throw out the encoded video
                button.IsChecked = false;
                MainTextBlock.Text = "canceled";
                MainProgressBar.IsIndeterminate = false;
                await file.DeleteAsync();
                return;
            }
            // Move our vidoe to its new home
            await file.MoveAndReplaceAsync(newFile);

            // Tell the user we're done
            button.IsChecked = false;
            MainTextBlock.Text = "done";
            MainProgressBar.IsIndeterminate = false;

            // Open the final product
            await Launcher.LaunchFileAsync(newFile);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // If the encoder is doing stuff, tell it to stop
            _encoder?.Dispose();
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

        private async Task<StorageFile> GetTempFileAsync()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var name = DateTime.Now.ToString("yyyyMMdd-HHmm-ss");
            var file = await folder.CreateFileAsync($"{name}.mp4");
            return file;
        }

        private Direct3D11Device _device;
        private Encoder _encoder;
    }
}
