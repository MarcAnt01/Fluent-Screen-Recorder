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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Documents;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Composition;
using Windows.UI.WindowManagement;
using FluentScreenRecorder.Views;
using FluentScreenRecorder.Dialogs;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.AppCenter.Crashes;

namespace FluentScreenRecorder
{
    class ResolutionItem
    {
        public string DisplayName { get; set; }
        public SizeUInt32 Resolution { get; set; }

        public bool IsZero() { return Resolution.Width == 0 || Resolution.Height == 0; }
    }

    class BitrateItem
    {
        public string DisplayName { get; set; }
        public uint Bitrate { get; set; }
    }

    class FrameRateItem
    {
        public string DisplayName { get; set; }
        public uint FrameRate { get; set; }
    }


    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            //Adjust minimum and default window size
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(400, 250));
            ApplicationView.PreferredLaunchViewSize = new Size(400, 250);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            //hide titlebar
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            //Record icon
            RecordIcon.Visibility = Visibility.Visible;
            StopIcon.Visibility = Visibility.Collapsed;
            Ellipse.Visibility = Visibility.Collapsed;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = "Start recording";
            ToolTipService.SetToolTip(MainButton, toolTip);            

            _device = Direct3D11Helpers.CreateDevice();

            var settings = GetCachedSettings();

            _resolutions = new List<ResolutionItem>();
            foreach (var resolution in EncoderPresets.Resolutions)
            {
                _resolutions.Add(new ResolutionItem()
                {
                    DisplayName = $"{resolution.Width} x {resolution.Height}",
                    Resolution = resolution,
                });
            }
            ResolutionComboBox.ItemsSource = _resolutions;
            ResolutionComboBox.SelectedIndex = GetResolutionIndex(settings.Width, settings.Height);

            _bitrates = new List<BitrateItem>();
            foreach (var bitrate in EncoderPresets.Bitrates)
            {
                var mbps = (float)bitrate / 1000000;
                _bitrates.Add(new BitrateItem()
                {
                    DisplayName = $"{mbps:0.##} Mbps",
                    Bitrate = bitrate,
                });
            }
            BitrateComboBox.ItemsSource = _bitrates;
            BitrateComboBox.SelectedIndex = GetBitrateIndex(settings.Bitrate);

            _frameRates = new List<FrameRateItem>();
            foreach (var frameRate in EncoderPresets.FrameRates)
            {
                _frameRates.Add(new FrameRateItem()
                {
                    DisplayName = $"{frameRate}fps",
                    FrameRate = frameRate,
                });
            }
            FrameRateComboBox.ItemsSource = _frameRates;
            FrameRateComboBox.SelectedIndex = GetFrameRateIndex(settings.FrameRate);

            UseCaptureItemToggleSwitch.IsOn = settings.UseSourceSize;
            PreviewToggleSwitch.IsOn = settings.Preview;
        }

        public static StorageFile _tempFile;
        private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;

            // Get our encoder properties
            var frameRateItem = (FrameRateItem)FrameRateComboBox.SelectedItem;
            var resolutionItem = (ResolutionItem)ResolutionComboBox.SelectedItem;
            var bitrateItem = (BitrateItem)BitrateComboBox.SelectedItem;

            if (UseCaptureItemToggleSwitch.IsOn)
            {
                resolutionItem.IsZero();
            }

            var width = resolutionItem.Resolution.Width;
            var height = resolutionItem.Resolution.Height;
            var bitrate = bitrateItem.Bitrate;
            var frameRate = frameRateItem.FrameRate;
            if (UseCaptureItemToggleSwitch.IsOn)
            {
                resolutionItem.IsZero();
            }

            // Get our capture item
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();
            if (item == null)
            {
                button.IsChecked = false;
                return;
            }

            // Use the capture item's size for the encoding if desired
            if (UseCaptureItemToggleSwitch.IsOn)
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
            
            var visual = ElementCompositionPreview.GetElementVisual(Ellipse);
            var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0, 1);
            animation.InsertKeyFrame(1, 0);
            animation.Duration = TimeSpan.FromMilliseconds(1500);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;                        
            visual.StartAnimation("Opacity", animation);

            RecordIcon.Visibility = Visibility.Collapsed;
            StopIcon.Visibility = Visibility.Visible;
            Ellipse.Visibility = Visibility.Visible;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = "Stop recording";
            ToolTipService.SetToolTip(MainButton, toolTip);
            MainTextBlock.Text = "recording...";
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
                Crashes.TrackError(ex);
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
                visual.StopAnimation("Opacity");

                Ellipse.Visibility = Visibility.Collapsed;
                MainTextBlock.Text = "failure";
                MainTextBlock.Foreground = originalBrush;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;                
                toolTip.Content = "Start recording";
                ToolTipService.SetToolTip(MainButton, toolTip);

                return;
            }

            // At this point the encoding has finished,
            // tell the user we're now saving

            MainButton.IsChecked = false;
            MainTextBlock.Text = "";
            visual.StopAnimation("Opacity");
            Ellipse.Visibility = Visibility.Collapsed;
            RecordIcon.Visibility = Visibility.Visible;
            StopIcon.Visibility = Visibility.Collapsed;            
            ToolTip newtoolTip = new ToolTip();
            toolTip.Content = "Start recording";
            ToolTipService.SetToolTip(MainButton, toolTip);

            if (PreviewToggleSwitch.IsOn)
            {
                AppWindow appWindow = await AppWindow.TryCreateAsync();
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                // Create a Frame and navigate to the Page you want to show in the new window.
                Frame appWindowContentFrame = new Frame();
                VideoPreviewPage.Appwindowref = appWindow;
                VideoPreviewPage.VideoFile = _tempFile;
                appWindowContentFrame.Navigate(typeof(VideoPreviewPage));

                ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);
                await appWindow.TryShowAsync();
            }
            else
            {
                ContentDialog dialog = new SaveDialog();
                await dialog.ShowAsync();                
            }            
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            // If the encoder is doing stuff, tell it to stop
            _encoder?.Dispose();
        }

        public static async void Save()
        {
            //move the temp file to Videos Library
            StorageFolder localFolder = KnownFolders.VideosLibrary;
            var newFile = await _tempFile.CopyAsync(localFolder);            
            if (newFile == null)
            {
                await _tempFile.DeleteAsync();                
            }                        
        }

        public  static async void SaveAs()
        {
            StorageFile newFile = await PickVideoAsync();
            if (newFile == null)
            {
                // Throw out the encoded video
                await _tempFile.DeleteAsync();
            }
            else
            {
                //move the file to the location selected with the picker
                await _tempFile.MoveAndReplaceAsync(newFile);                
            }
        }

        public static async void Cancel()
        {
            await _tempFile.DeleteAsync();            
        }

        private void Saved()
        {
            MainTextBlock.Text = "Saved";
        }       


        private static async Task<StorageFile> PickVideoAsync()
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

        public static void Share()
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            //DataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            DataTransferManager.ShowShareUI();
        }


        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = _tempFile.Name;
            request.Data.SetStorageItems(new StorageFile[] { _tempFile });
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
            var resolutionItem = (ResolutionItem)ResolutionComboBox.SelectedItem;
            var width = resolutionItem.Resolution.Width;
            var height = resolutionItem.Resolution.Height;
            var bitrateItem = (BitrateItem)BitrateComboBox.SelectedItem;
            var bitrate = bitrateItem.Bitrate;
            var frameRateItem = (FrameRateItem)FrameRateComboBox.SelectedItem;
            var frameRate = frameRateItem.FrameRate;
            var useSourceSize = UseCaptureItemToggleSwitch.IsOn;
            var preview = PreviewToggleSwitch.IsOn;

            return new AppSettings { Width = width, Height = height, Bitrate = bitrate, FrameRate = frameRate, UseSourceSize = useSourceSize , Preview = preview };

        }

        private AppSettings GetCachedSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var result = new AppSettings
            {
                Width = 1920,
                Height = 1080,
                Bitrate = 18000000,
                FrameRate = 60,
                UseSourceSize = true,
                Preview = true
            };
            // Resolution
            if (localSettings.Values.TryGetValue(nameof(AppSettings.Width), out var width) &&
                localSettings.Values.TryGetValue(nameof(AppSettings.Height), out var height))
            {
                result.Width = (uint)width;
                result.Height = (uint)height;
            }
            
            else if (localSettings.Values.TryGetValue("Quality", out var quality))
            {
                var videoQuality = ParseEnumValue<VideoEncodingQuality>((string)quality);

                var temp = MediaEncodingProfile.CreateMp4(videoQuality);
                result.Width = temp.Video.Width;
                result.Height = temp.Video.Height;
            }
            // Frame rate
            if (localSettings.Values.TryGetValue(nameof(AppSettings.FrameRate), out var frameRate))
            {
                result.FrameRate = (uint)frameRate;
            }

            if (localSettings.Values.TryGetValue(nameof(AppSettings.UseSourceSize), out var useSourceSize))
            {
                result.UseSourceSize = (bool)useSourceSize;            }
            

            if (localSettings.Values.TryGetValue(nameof(AppSettings.Preview), out var preview))
            {
                result.Preview = (bool)preview;
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
            localSettings.Values[nameof(AppSettings.Width)] = settings.Width;
            localSettings.Values[nameof(AppSettings.Height)] = settings.Height;
            localSettings.Values[nameof(AppSettings.Bitrate)] = settings.Bitrate;
            localSettings.Values[nameof(AppSettings.FrameRate)] = settings.FrameRate;
            localSettings.Values[nameof(AppSettings.UseSourceSize)] = settings.UseSourceSize;
            localSettings.Values[nameof(AppSettings.Preview)] = settings.Preview;
        }

        private int GetResolutionIndex(uint width, uint height)
        {
            for (var i = 0; i < _resolutions.Count; i++)
            {
                var resolution = _resolutions[i];
                if (resolution.Resolution.Width == width &&
                    resolution.Resolution.Height == height)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetBitrateIndex(uint bitrate)
        {
            for (var i = 0; i < _bitrates.Count; i++)
            {
                if (_bitrates[i].Bitrate == bitrate)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetFrameRateIndex(uint frameRate)
        {
            for (var i = 0; i < _frameRates.Count; i++)
            {
                if (_frameRates[i].FrameRate == frameRate)
                {
                    return i;
                }
            }
            return -1;
        }

        private static T ParseEnumValue<T>(string input)
        {
            return (T)Enum.Parse(typeof(T), input, false);
        }

        struct AppSettings
        {
            public uint Width;
            public uint Height;
            public uint Bitrate;
            public uint FrameRate;
            public bool UseSourceSize;
            public bool Preview;
        }

        private IDirect3DDevice _device;
        private Encoder _encoder;
        private List<ResolutionItem> _resolutions;
        private List<BitrateItem> _bitrates;
        private List<FrameRateItem> _frameRates;

        public static string GetAppVersion()
        {

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            TextBlock gHRepoTB = new TextBlock();
            Hyperlink hyperlink1 = new Hyperlink();
            Run run1 = new Run();
            run1.Text = "View GitHub repository";
            hyperlink1.NavigateUri = new Uri("https://github.com/MarcAnt01/Fluent-Screen-Recorder");
            hyperlink1.Inlines.Add(run1);
            gHRepoTB.Inlines.Add(hyperlink1);

            TextBlock privacyPolicyTB = new TextBlock();
            privacyPolicyTB.Margin = new Thickness(0,10,0,10);
            Hyperlink hyperlink2 = new Hyperlink();
            Run run2 = new Run();
            run2.Text = "Privacy Policy";
            hyperlink2.NavigateUri = new Uri("https://github.com/MarcAnt01/Fluent-Screen-Recorder/blob/master/PRIVACY.md");
            hyperlink2.Inlines.Add(run2);
            privacyPolicyTB.Inlines.Add(hyperlink2);

            TextBlock versionTB = new TextBlock();
            versionTB.Text = "Version";
            versionTB.Margin = new Thickness(0,0,3,0);
            TextBlock versionNumberTB = new TextBlock();
            string version = GetAppVersion();
            versionNumberTB.Text = version;
            versionNumberTB.FontWeight = Windows.UI.Text.FontWeights.Bold;
            StackPanel versionPanel = new StackPanel();
            versionPanel.Children.Add(versionTB);
            versionPanel.Children.Add(versionNumberTB);
            versionPanel.Orientation = Orientation.Horizontal;

            StackPanel aboutPanel = new StackPanel();
            aboutPanel.Children.Add(gHRepoTB);
            aboutPanel.Children.Add(privacyPolicyTB);
            aboutPanel.Children.Add(versionPanel);

            ContentDialog aboutDialog = new ContentDialog();
            aboutDialog.Title = "About";
            aboutDialog.Content = aboutPanel;
            aboutDialog.PrimaryButtonText = "Report a bug";
            aboutDialog.PrimaryButtonClick += ReportBug_Click;
            aboutDialog.PrimaryButtonStyle = App.Current.Resources["AccentButtonStyle"] as Style;
            aboutDialog.CloseButtonText = "Close";
            await aboutDialog.ShowAsync();
        }

        private async void ReportBug_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            string gitHubIssue = @"https://github.com/MarcAnt01/Fluent-Screen-Recorder/issues/new";
            var uri = new Uri(gitHubIssue);
            var uriOpened = await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}