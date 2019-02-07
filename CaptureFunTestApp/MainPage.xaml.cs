using CaptureFun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
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

            var names = new List<string>();
            names.Add(nameof(VideoEncodingQuality.HD1080p));
            names.Add(nameof(VideoEncodingQuality.HD720p));
            names.Add(nameof(VideoEncodingQuality.Uhd2160p));
            names.Add(nameof(VideoEncodingQuality.Uhd4320p));
            QualityComboBox.ItemsSource = names;
            QualityComboBox.SelectedIndex = names.IndexOf(nameof(VideoEncodingQuality.HD1080p));

            var frameRates = new string[] { "30fps", "60fps" };
            FrameRateComboBox.ItemsSource = frameRates;
            FrameRateComboBox.SelectedIndex = 1;
        }

        private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;

            // Get our encoder properties
            var frameRate = uint.Parse(((string)FrameRateComboBox.SelectedItem).Replace("fps", ""));
            var quality = (VideoEncodingQuality)Enum.Parse(typeof(VideoEncodingQuality), (string)QualityComboBox.SelectedItem, false);
            var temp = MediaEncodingProfile.CreateMp4(quality);
            var bitrate = temp.Video.Bitrate;
            var width = temp.Video.Width;
            var height = temp.Video.Height;

            // Get our capture item
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();
            if (item == null)
            {
                button.IsChecked = false;
                return;
            }

            // Use the capture item's size for the encoding if desired
            if (UseCaptureItemSizeCheckBox.IsChecked.Value)
            {
                width = (uint)item.Size.Width;
                height = (uint)item.Size.Height;

                // Even if we're using the capture item's real size,
                // we still want to make sure the numbers are even.
                // Some encoders get mad if you give them odd numbers.
                width = EnsureEven(width);
                height = EnsureEven(height);
            }

            // Find a place to put our vidoe for now
            var file = await GetTempFileAsync();

            // Tell the user we've started recording
            MainTextBlock.Text = "recording...";
            MainProgressBar.IsIndeterminate = true;

            // Kick off the encoding
            try
            {
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                using (_encoder = new Encoder(_device, item))
                {
                    await _encoder.EncodeAsync(
                        stream, 
                        width, height, bitrate, 
                        frameRate);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);

                var dialog = new MessageDialog(
                    $"Uh-oh! Something went wrong!\n0x{ex.HResult:X8} - {ex.Message}",
                    "Recording failed");

                await dialog.ShowAsync();

                button.IsChecked = false;
                MainTextBlock.Text = "failure";
                MainProgressBar.IsIndeterminate = false;
                return;
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

        private uint EnsureEven(uint number)
        {
            if (number % 2 == 0)
            {
                return number;
            }
            else
            {
                return number + 1;
            }
        }

        private Direct3D11Device _device;
        private Encoder _encoder;
    }
}
