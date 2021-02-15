using CaptureEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Composition;
using FluentScreenRecorder.Views;
using FluentScreenRecorder.Dialogs;
using Microsoft.AppCenter.Crashes;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation;
using NAudio.Wave;
using ScreenSenderComponent;
using Windows.Media.Transcoding;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using Windows.System;

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

    public class ThumbItem
    {
        public BitmapImage img { get; set; }
        public String fileN { get; set; }
    }


    public sealed partial class MainPage : Page
    {
        private LoopbackAudioCapture loopbackAudioCapture;
        private Visual visual;
        private ToolTip toolTip;
        private List<byte> BufferList = new List<byte>();
        MediaPlayer SilentPlayer;
        private AudioEncodingProperties audioEncodingProperties;        

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += LoadedHandler;

            MergingProgressRing.Visibility = Visibility.Collapsed;

            SilentPlayer = new MediaPlayer() { IsLoopingEnabled = true };
            SilentPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Silence.ogg"));
            SilentPlayer.Play();            

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
            AutomationProperties.SetName(MainButton, "Start recording");


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

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            this.Loaded -= LoadedHandler;
            var folder = await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder");
            IReadOnlyList<StorageFile> sortedItems = await folder.GetFilesAsync(CommonFileQuery.OrderByDate);
            List<ThumbItem> thumbnailsList = new List<ThumbItem>();
            foreach (StorageFile file in sortedItems)
            {
                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(thumbnail);
                thumbnailsList.Add(new ThumbItem() { img = bitmap , fileN = file.Name});
                
            }
            BasicGridView.ItemsSource = thumbnailsList;

        }


        public StorageFile _tempFile;

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

            if (AudioToggleSwitch.IsOn)
            {
                loopbackAudioCapture = new LoopbackAudioCapture(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default));
                loopbackAudioCapture.BufferReadyDelegate = LoopbackBufferReady;
                BufferList.Clear();
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
            SecondColumn.Width = new GridLength(0);
            ThirdColumn.Width = new GridLength(0);



            visual = ElementCompositionPreview.GetElementVisual(Ellipse);
            var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0, 1);
            animation.InsertKeyFrame(1, 0);
            animation.Duration = TimeSpan.FromMilliseconds(1500);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            visual.StartAnimation("Opacity", animation);

            RecordIcon.Visibility = Visibility.Collapsed;
            StopIcon.Visibility = Visibility.Visible;
            Ellipse.Visibility = Visibility.Visible;
            toolTip = new ToolTip();
            toolTip.Content = "Stop recording";
            ToolTipService.SetToolTip(MainButton, toolTip);
            AutomationProperties.SetName(MainButton, "Stop recording");
            MainTextBlock.Text = "recording...";
            var originalBrush = MainTextBlock.Foreground;
            MainTextBlock.Foreground = new SolidColorBrush(Colors.Red);

            // Kick off the encoding
            try
            {
                using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                using (_encoder = new Encoder(_device, item))
                {
                    var encodesuccess = await _encoder.EncodeAsync(
                    stream,
                    width, height, bitrate,
                    frameRate, loopbackAudioCapture);
                    if (encodesuccess == false)
                    {
                        ContentDialog errorDialog = new ContentDialog
                        {
                            Title = "Recording failed",
                            Content = "Windows cannot encode your video",
                            CloseButtonText = "OK"
                        };
                        await errorDialog.ShowAsync();
                    }

                }
                MainTextBlock.Foreground = originalBrush;

                Ellipse.Visibility = Visibility.Collapsed;
                visual.StopAnimation("Opacity");
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);

                var message = GetMessageForHResult(ex.HResult);
                if (message == null)
                {
                    message = $"Whoops, something went wrong!\n0x{ex.HResult:X8} - {ex.Message}";
                }
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Recording failed",
                    Content = message,
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();

                button.IsChecked = false;
                visual.StopAnimation("Opacity");

                Ellipse.Visibility = Visibility.Collapsed;
                SecondColumn.Width = new GridLength(1, GridUnitType.Star);
                ThirdColumn.Width = new GridLength(1, GridUnitType.Star);
                MainTextBlock.Text = "failure";
                MainTextBlock.Foreground = originalBrush;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                toolTip.Content = "Start recording";
                ToolTipService.SetToolTip(MainButton, toolTip);
                AutomationProperties.SetName(MainButton, "Start recording");
                await _tempFile.DeleteAsync();

                return;
            }

            // At this point the encoding has finished,
            // tell the user we're now saving

            if (!AudioToggleSwitch.IsOn)
            {
                MainButton.IsChecked = false;
                MainTextBlock.Text = "";
                visual.StopAnimation("Opacity");
                Ellipse.Visibility = Visibility.Collapsed;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                ToolTip newtoolTip = new ToolTip();
                toolTip.Content = "Start recording";
                ToolTipService.SetToolTip(MainButton, toolTip);
                AutomationProperties.SetName(MainButton, "Start recording");

                if (PreviewToggleSwitch.IsOn)
                {
                    CoreApplicationView newView = CoreApplication.CreateNewView();
                    int newViewId = 0;
                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var preview = new VideoPreviewPage(_tempFile);
                        ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                        formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
                        CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                        coreTitleBar.ExtendViewIntoTitleBar = true;
                        Window.Current.Content = preview;
                        Window.Current.Activate();
                        newViewId = ApplicationView.GetForCurrentView().Id;
                    });
                    bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);

                }
                else
                {
                    ContentDialog dialog = new SaveDialog(_tempFile);
                    await dialog.ShowAsync();
                }
            }
            else
            {
                CompleteRecording(BufferList.ToArray(), width, height, bitrate, frameRate);
            }
        }

        private async void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            //Collecting some info before being lost
            audioEncodingProperties = loopbackAudioCapture.EncodingProperties;

            // If the encoder is doing stuff, tell it to stop
            if (loopbackAudioCapture.Started)
                await loopbackAudioCapture.Stop();
            _encoder?.Dispose();            
        }

        private unsafe void LoopbackBufferReady(AudioClientBufferDetails details, out int numSamplesRead)
        {
            numSamplesRead = details.NumSamplesToRead;

            byte* buffer = (byte*)details.DataPointer;
            uint byteLength = (uint)details.ByteLength;

            byte[] audioBuffer = new byte[byteLength];
            Unsafe.CopyBlock(ref audioBuffer[0], ref *buffer, byteLength);


            foreach (var b in audioBuffer) BufferList.Add(b);
        }

        public async void CompleteRecording(byte[] audioBuffer, uint width, uint height, uint bitrateInBps, uint frameRate)
        {
            var clip = await MediaClip.CreateFromFileAsync(_tempFile);
            var composition = new MediaComposition();
            composition.Clips.Add(clip);

            StorageFile internalAudioFile = await GetAudioTempFileAsync(audioBuffer);

            var backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(internalAudioFile);
            composition.BackgroundAudioTracks.Add(backgroundTrack);

            var videoFile = _tempFile;

            var newFile = await GetTempFileAsync();

            MainTextBlock.Text = "saving...";
            MergingProgressRing.Visibility = Visibility.Visible;
            MainButton.Visibility = Visibility.Collapsed;

            var merge = composition.RenderToFileAsync(newFile, MediaTrimmingPreference.Fast);
            merge.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    MergingProgressRing.Value = progress;
                }));
            });
            merge.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler( async() =>

                {
                    MergingProgressRing.Value = 0;
                    MergingProgressRing.Visibility = Visibility.Collapsed;
                    _tempFile = newFile;

                    MainButton.IsChecked = false;
                    MainTextBlock.Text = "";
                    visual.StopAnimation("Opacity");
                    Ellipse.Visibility = Visibility.Collapsed;
                    RecordIcon.Visibility = Visibility.Visible;
                    StopIcon.Visibility = Visibility.Collapsed;
                    ToolTip newtoolTip = new ToolTip();
                    toolTip.Content = "Start recording";
                    ToolTipService.SetToolTip(MainButton, toolTip);
                    AutomationProperties.SetName(MainButton, "Start recording");

                    if (PreviewToggleSwitch.IsOn)
                    {
                        CoreApplicationView newView = CoreApplication.CreateNewView();
                        int newViewId = 0;
                        await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            var preview = new VideoPreviewPage(_tempFile);
                            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
                            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                            coreTitleBar.ExtendViewIntoTitleBar = true;
                            Window.Current.Content = preview;
                            Window.Current.Activate();
                            newViewId = ApplicationView.GetForCurrentView().Id;
                        });
                        bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);

                    }
                    else
                    {
                        ContentDialog dialog = new SaveDialog(_tempFile);
                        await dialog.ShowAsync();
                    }

                    MainButton.Visibility = Visibility.Visible;
                    SecondColumn.Width = new GridLength(1, GridUnitType.Star);
                    ThirdColumn.Width = new GridLength(1, GridUnitType.Star);
                    await videoFile.DeleteAsync();
                    await internalAudioFile.DeleteAsync();
                }));
            });

            
        }

        public static async Task<bool> Save(StorageFile file)
        {
            try
            {
                //move the temp file to Videos Library
                StorageFolder FluentScreenRecorderFolder = await KnownFolders.VideosLibrary.CreateFolderAsync("Fluent Screen Recorder", CreationCollisionOption.OpenIfExists);
                await file.MoveAsync(FluentScreenRecorderFolder);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> SaveAs(StorageFile file)
        {
            var newFile = await PickVideoAsync();
            if (newFile == null)
            {
                return false;
            }

            //move the file to the location selected with the picker
            await file.MoveAndReplaceAsync(newFile);
            return true;
        }

        public static async Task<bool> Delete(StorageFile file)
        {
            await file.DeleteAsync();
            return true;
        }


        private static async Task<StorageFile> PickVideoAsync()
        {
            try
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
            catch
            {
                return null;
            }
        }

        private async Task<StorageFile> GetTempFileAsync()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var file = await folder.CreateFileAsync($"{name}.mp4");
            return file;
        }

        private async Task<StorageFile> GetAudioTempFileAsync(byte[] Audiobuffer)
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var file = await folder.CreateFileAsync($"{name}.wav");
            var s = new RawSourceWaveStream(new MemoryStream(Audiobuffer), WaveFormat.CreateIeeeFloatWaveFormat((int)audioEncodingProperties.SampleRate, (int)audioEncodingProperties.ChannelCount));
            using (var writer = new WaveFileWriterRT(await file.OpenStreamForWriteAsync(), s.WaveFormat))
            {
                long outputLength = 0;
                var buffer = new byte[s.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = s.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }
                    outputLength += bytesRead;
                    // Write will throw exception if WAV file becomes too large
                    writer.Write(buffer, 0, bytesRead);
                }
            }
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

        private string GetMessageForHResult(int hresult)
        {
            switch ((uint)hresult)
            {
                // MF_E_TRANSFORM_TYPE_NOT_SET
                case 0xC00DA412:
                    return "The combination of options you've chosen are not supported by your hardware.";
                case 0x80070070:
                    return "There is not enough space for recording in your device. ";
                case 0xC00D4A44:
                    return "The recorder wasn't able to capture enough frames";
                default:
                    return null;
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

            return new AppSettings { Width = width, Height = height, Bitrate = bitrate, FrameRate = frameRate, UseSourceSize = useSourceSize, Preview = preview };

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
                result.UseSourceSize = (bool)useSourceSize;
            }


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

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        private async void Image_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ThumbItem item = (sender as GridViewItem).DataContext as ThumbItem;
            var file = await(await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder")).GetFileAsync(item.fileN);
            await Launcher.LaunchFileAsync(file);
        }
    }
}