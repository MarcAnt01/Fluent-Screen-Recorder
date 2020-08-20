using CaptureEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;


namespace FluentScreenRecorder
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            //Adjust minimum and default window size
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(400, 260));
            ApplicationView.PreferredLaunchViewSize = new Size(400,260);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            //hide titlebar
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            //Record icon
            RecordIcon.Visibility = Visibility.Visible;
            StopIcon.Visibility = Visibility.Collapsed;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = "Start recording";
            ToolTipService.SetToolTip(MainButton, toolTip);

            _device = Direct3D11Helpers.CreateDevice();

            var settings = GetCachedSettings();

            var names = new List<string>();
            names.Add(nameof(VideoEncodingQuality.HD1080p));
            names.Add(nameof(VideoEncodingQuality.HD720p));
            names.Add(nameof(VideoEncodingQuality.Uhd2160p));
            names.Add(nameof(VideoEncodingQuality.Uhd4320p));
            QualityComboBox.ItemsSource = names;
            QualityComboBox.SelectedIndex = names.IndexOf(settings.Quality.ToString());

            var frameRates = new List<string> { "30fps", "60fps" };
            FrameRateComboBox.ItemsSource = frameRates;
            FrameRateComboBox.SelectedIndex = frameRates.IndexOf($"{settings.FrameRate}fps");

            UseCaptureItemToggleSwitch.IsOn = settings.UseSourceSize;
        }

        private StorageFile _tempFile;
        private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;

            // Get our encoder properties
            var frameRate = uint.Parse(((string)FrameRateComboBox.SelectedItem).Replace("fps", ""));
            var quality = (VideoEncodingQuality)Enum.Parse(typeof(VideoEncodingQuality), (string)QualityComboBox.SelectedItem, false);
            var useSourceSize = UseCaptureItemToggleSwitch.IsOn;

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
            if (useSourceSize)
            {
                width = (uint)item.Size.Width;
                height = (uint)item.Size.Height;

                // Even if we're using the capture item's real size,
                // we still want to make sure the numbers are even.
                // Some encoders get mad if you give them odd numbers.
                width = EnsureEven(width);
                height = EnsureEven(height);
            }

            // Put videos in the temp folder
            var tempFile = await GetTempFileAsync();
            _tempFile = tempFile;

            // Tell the user we've started recording
            RecordIcon.Visibility = Visibility.Collapsed;
            StopIcon.Visibility = Visibility.Visible;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = "Stop recording";
            ToolTipService.SetToolTip(MainButton, toolTip);
            MainTextBlock.Text = "● recording...";
            var originalBrush = MainTextBlock.Foreground;
            MainTextBlock.Foreground = new SolidColorBrush(Colors.Red);

            // Kick off the encoding
            try
            {
                using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                using (_encoder = new Encoder(_device, item))
                {
                    await _encoder.EncodeAsync(
                        stream,
                        width, height, bitrate,
                        frameRate);
                }
                MainTextBlock.Foreground = originalBrush;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);

                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Recording failed",
                    Content = $"Whoops, something went wrong!\n0x{ex.HResult:X8} - {ex.Message}",
                    CloseButtonText = "Ok"
                };
                await errorDialog.ShowAsync();

                button.IsChecked = false;
                MainTextBlock.Text = "failure";
                MainTextBlock.Foreground = originalBrush;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                MainTextBlock.Text = "saving...";
                toolTip.Content = "Start recording";
                ToolTipService.SetToolTip(MainButton, toolTip);

                return;
            }

            // At this point the encoding has finished,
            // tell the user we're now saving
            RecordIcon.Visibility = Visibility.Visible;
            StopIcon.Visibility = Visibility.Collapsed;
            MainTextBlock.Text = "saving...";
            ToolTip newtoolTip = new ToolTip();
            toolTip.Content = "Start recording";
            ToolTipService.SetToolTip(MainButton, toolTip);

            var saveDialog = new ContentDialog();
            saveDialog.Title = "Do you want to save your file?";
            saveDialog.Content = "You can choose among the following options";
            saveDialog.PrimaryButtonText = "Save";
            saveDialog.PrimaryButtonClick += Save_Click;
            saveDialog.PrimaryButtonStyle = App.Current.Resources["AccentButtonStyle"] as Style;
            saveDialog.SecondaryButtonText = "Save as...";
            saveDialog.SecondaryButtonClick += SaveAs_Click;
            saveDialog.CloseButtonText = "Delete";
            saveDialog.CloseButtonClick += Cancel_Click;
            await saveDialog.ShowAsync();
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // If the encoder is doing stuff, tell it to stop
            _encoder?.Dispose();
        }

        private async void Save_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            //move the temp file to Videos Library
            StorageFolder localFolder = KnownFolders.VideosLibrary;
            var newFile = await _tempFile.CopyAsync(localFolder);
            if (newFile == null)
            {
                // Throw out the encoded video
                MainButton.IsChecked = false;
                MainTextBlock.Text = "canceled";

                await _tempFile.DeleteAsync();
                await newFile.DeleteAsync();
            }

            // Tell the user we're done
            MainButton.IsChecked = false;
            MainTextBlock.Text = "done";

        }

        private async void SaveAs_Click(object sender, ContentDialogButtonClickEventArgs e)
        {

            StorageFile newFile = await PickVideoAsync();
            if (newFile == null)
            {
                // Throw out the encoded video
                MainButton.IsChecked = false;
                MainTextBlock.Text = "canceled";

                await _tempFile.DeleteAsync();
                await newFile.DeleteAsync();
            }

            //move the file to the location selected with the picker
            await _tempFile.MoveAndReplaceAsync(newFile);

            // Tell the user we're done
            MainButton.IsChecked = false;
            MainTextBlock.Text = "done";
        }

        private async void Cancel_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            // Throw out the encoded video
            MainButton.IsChecked = false;
            MainTextBlock.Text = "canceled";

            await _tempFile.DeleteAsync();
        }

        private async Task<StorageFile> PickVideoAsync()
        {
            var picker = new FileSavePicker();
            var time = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.SuggestedFileName = $"recordedVideo{time}";
            picker.DefaultFileExtension = ".mp4";
            picker.FileTypeChoices.Add("MP4 Video", new List<string> { ".mp4" });

            var file = await picker.PickSaveFileAsync();
            return file;
        }

        private async Task<StorageFile> GetTempFileAsync()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
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

        private AppSettings GetCurrentSettings()
        {
            var quality = ParseEnumValue<VideoEncodingQuality>((string)QualityComboBox.SelectedItem);
            var frameRate = uint.Parse(((string)FrameRateComboBox.SelectedItem).Replace("fps", ""));
            var useSourceSize = UseCaptureItemToggleSwitch.IsOn;

            return new AppSettings { Quality = quality, FrameRate = frameRate, UseSourceSize = useSourceSize };
        }

        private AppSettings GetCachedSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var result =  new AppSettings
            {
                Quality = VideoEncodingQuality.HD1080p,
                FrameRate = 60,
                UseSourceSize = true
            };
            if (localSettings.Values.TryGetValue(nameof(AppSettings.Quality), out var quality))
            {
                result.Quality = ParseEnumValue<VideoEncodingQuality>((string)quality);
            }
            if (localSettings.Values.TryGetValue(nameof(AppSettings.FrameRate), out var frameRate))
            {
                result.FrameRate = (uint)frameRate;
            }
            if (localSettings.Values.TryGetValue(nameof(AppSettings.UseSourceSize), out var useSourceSize))
            {
                result.UseSourceSize = (bool)useSourceSize;
            }
            return result;
        }
        public void CacheCurrentSettings()
        {
            var settings = GetCurrentSettings();
            CacheSettings(settings);
        }

        private static void CacheSettings(AppSettings settings)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[nameof(AppSettings.Quality)] = settings.Quality.ToString();
            localSettings.Values[nameof(AppSettings.FrameRate)] = settings.FrameRate;
            localSettings.Values[nameof(AppSettings.UseSourceSize)] = settings.UseSourceSize;
        }

        private static T ParseEnumValue<T>(string input)
        {
            return (T)Enum.Parse(typeof(T), input, false);
        }

        struct AppSettings
        {
            public VideoEncodingQuality Quality;
            public uint FrameRate;
            public bool UseSourceSize;
        }

        private IDirect3DDevice _device;
        private Encoder _encoder;
    }
}
