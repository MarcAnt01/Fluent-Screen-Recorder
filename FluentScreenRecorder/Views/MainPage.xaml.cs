using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.System;
using Windows.Media.Capture;
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using FluentScreenRecorder.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder
{
    public sealed partial class MainPage : Page
    {
        private LoopbackAudioCapture loopbackAudioCapture;
        private Visual visual;
        private ToolTip toolTip;
        private List<byte> BufferList = new();
        private MediaPlayer SilentPlayer;
        private AudioEncodingProperties audioEncodingProperties;
        private StorageFile micFile;
        private MediaCapture mediaCapture;
        private StorageFile recordedVideoFile = null;
        private bool lockAdaptiveUI;
        private bool filesInFolder;
        private ObservableCollection<ThumbItem> thumbnailsList = new();
        private Size _userSize;

        public static MainPage Current;

        public MainPage()
        {
            InitializeComponent();
            Current = this;

            Loaded += LoadedHandler;

            NavigationCacheMode = NavigationCacheMode.Required;

            SilentPlayer = new()
            {
                IsLoopingEnabled = true,
                Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Silence.ogg"))
            };

            SilentPlayer.Play();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(412, 88));

            ResolutionComboBox.ItemsSource = App.RecorderHelper.Resolutions;
            ResolutionComboBox.SelectedIndex = App.RecorderHelper.Resolutions.IndexOf(App.RecorderHelper.Resolutions.FirstOrDefault(r => r.Resolution.Width == App.Settings.Width && r.Resolution.Height == App.Settings.Height));

            FrameRateComboBox.ItemsSource = App.RecorderHelper.Framerates;
            FrameRateComboBox.SelectedIndex = App.RecorderHelper.Framerates.IndexOf(App.RecorderHelper.Framerates.FirstOrDefault(r => r.FrameRate == App.Settings.FrameRate));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupTitleBar();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
            
            base.OnNavigatedTo(e);
        }        

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            Loaded -= LoadedHandler;

            visual = ElementCompositionPreview.GetElementVisual(Ellipse);
            var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0, 1);
            animation.InsertKeyFrame(1, 0);
            animation.Duration = TimeSpan.FromMilliseconds(1500);
            animation.IterationBehavior = AnimationIterationBehavior.Forever;
            visual.StartAnimation("Opacity", animation);

            if (App.Settings.ShowOnTop)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(412, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
                ExitOverlayIcon.Visibility = Visibility.Visible;

                ToolTip toolTip = new()
                {
                    Content = Strings.Resources.ExitOverlay
                };

                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.ExitOverlay);
            }
            else
            {
                ExitOverlayIcon.Visibility = Visibility.Collapsed;
                GoToOverlayIcon.Visibility = Visibility.Visible;
                ToolTip toolTip = new()
                {
                    Content = Strings.Resources.GoToOverlay
                };
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
            }

            await LoadThumbanails();

            if (filesInFolder && App.Settings.Gallery && ((Frame)Window.Current.Content).ActualWidth > 680)
            {
                BasicGridView.Visibility = Visibility.Visible;
            }
            else if (App.Settings.Gallery && !filesInFolder)
            {
                BasicGridView.Visibility = Visibility.Collapsed;
                NoVideosContainer.Visibility = Visibility.Visible;
            } 
            else if (!filesInFolder)
            {
                BasicGridView.Visibility = Visibility.Collapsed;
            }
            else if (!App.Settings.Gallery)
            {
                BasicGridView.Visibility = Visibility.Collapsed;
            }

            // We don't have to create the video folder at startup - just ignore populating the folder view if the folder doesn't exist (yet).
            // Saving a recording will automatically create the folder if missing.

        }

        public async Task LoadThumbanails()
        {
            thumbnailsList.Clear();
            if (await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder") is StorageFolder videoFolder)
            {
                IReadOnlyList<StorageFile> storageItems = await videoFolder.GetFilesAsync();
                if (storageItems.Count > 0)
                {
                    foreach (StorageFile file in storageItems)
                    {
                        StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                        BitmapImage bitmap = new();
                        bitmap.SetSource(thumbnail);
                        thumbnailsList.Add(new() { img = bitmap, fileN = file.Name });
                    }
                    BasicGridView.ItemsSource = thumbnailsList.Reverse();
                    filesInFolder = true;
                    NoVideosContainer.Visibility = Visibility.Collapsed;
                    BasicGridView.Visibility = Visibility.Visible;
                }
                else if (storageItems.Count <= 0)
                {
                    filesInFolder = false;
                    NoVideosContainer.Visibility = Visibility.Visible;
                    BasicGridView.Visibility = Visibility.Collapsed;
                }
            }
        }

        public StorageFile _tempFile;

        private async void OnRecordButtonClick(object sender, RoutedEventArgs e)
        {
            if (!App.RecorderHelper.IsRecording)
            {
                App.RecorderHelper.IsRecording = true;
                await StartRecordingAsync();
            }
            else
            {
                App.RecorderHelper.IsRecording = false;
                await StopRecordingAsync();
            }
        }

        private async Task StartRecordingAsync()
        {
            var folder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

            // Get our encoder properties
            var frameRateItem = App.RecorderHelper.Framerates.FirstOrDefault(f => f.FrameRate == App.Settings.FrameRate);
            var resolutionItem = App.RecorderHelper.Resolutions.FirstOrDefault(r => r.Resolution.Width > App.Settings.Width && r.Resolution.Height > App.Settings.Height);
            var bitrateItem = App.RecorderHelper.Bitrates.FirstOrDefault(b => b.Bitrate == App.Settings.Bitrate);

            MediaCapture mediaCapture = null;   

            if (App.Settings.IntAudio)
            {
                loopbackAudioCapture = new(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default))
                {
                    BufferReadyDelegate = LoopbackBufferReady
                };
                BufferList.Clear();
            }
            else if (App.Settings.ExtAudio)
            {
                if (await IsMicAllowed())
                {
                    mediaCapture = new MediaCapture();
                    MediaCaptureInitializationSettings settings = new()
                    {
                        StreamingCaptureMode = StreamingCaptureMode.Audio
                    };
                    await mediaCapture.InitializeAsync(settings);
                    var tempfolder = ApplicationData.Current.TemporaryFolder;
                    var name = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                    micFile = await tempfolder.CreateFileAsync($"{name}.mp3");
                }
                else
                {
                    ContentDialog errorDialog = new()
                    {
                        Title = Strings.Resources.Failure,
                        Content = Strings.Resources.MicNoPermission,
                        CloseButtonText = Strings.Resources.Ok
                    };
                    await errorDialog.ShowAsync();
                    return;
                }
            }

            var width = resolutionItem.Resolution.Width;
            var height = resolutionItem.Resolution.Height;
            var bitrate = bitrateItem.Bitrate;
            var frameRate = frameRateItem.FrameRate;
            var useSourceSize = resolutionItem.IsZero();
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();

            if (item == null)
            {
                App.RecorderHelper.IsRecording = false;
                return;
            }
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

            if (App.Settings.Timer)
                await Task.Delay(TimeSpan.FromSeconds(3));

            _userSize = App.RecorderHelper.Size;

            // Tell the user we've started recording
            App.RecorderHelper.SetAppSize(new(412, 88), false);
            NotifyRecordingStatusChanges(true);

            // Kick off the encoding
            try
            {
                using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                using (App.RecorderHelper.Encoder = new(App.RecorderHelper.Device, item))
                {
                    if (mediaCapture != null)
                    {
                        await mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), micFile);
                    }
                    if (!await App.RecorderHelper.Encoder.EncodeAsync(stream, width, height, bitrate, frameRate, loopbackAudioCapture, App.Settings.ShowCursor))
                    {
                        ContentDialog errorDialog = new()
                        {
                            Title = Strings.Resources.Failure,
                            Content = "Windows cannot encode your video.",
                            CloseButtonText = Strings.Resources.Ok                           
                        };
                        await errorDialog.ShowAsync();
                    }
                }
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
                ContentDialog recordingErrorDialog = new()
                {
                    Title = Strings.Resources.Failure,
                    Content = message,
                    CloseButtonText = Strings.Resources.Ok
                };                
            
                RecordButton.Visibility = Visibility.Collapsed;
                RecordingContainer.Visibility = Visibility.Collapsed;

                await recordingErrorDialog.ShowAsync();
                App.RecorderHelper.IsRecording = false;

                NotifyRecordingStatusChanges(false);
                await _tempFile.DeleteAsync();

                return;
            }


            // At this point the encoding has finished,
            // tell the user we're now saving

            if (App.Settings.IntAudio)
            {
                await CompleteRecording(BufferList.ToArray(), width, height, bitrate, frameRate);
            }
            else if (App.Settings.ExtAudio)
            {
                var clip = await MediaClip.CreateFromFileAsync(_tempFile);
                var composition = new MediaComposition();
                composition.Clips.Add(clip);

                StorageFile internalAudioFile = micFile;
                mediaCapture.Dispose();
                mediaCapture = null;

                var backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(internalAudioFile);
                composition.BackgroundAudioTracks.Add(backgroundTrack);

                var videoFile = _tempFile;

                var newFile = await GetTempFileAsync();

                var merge = composition.RenderToFileAsync(newFile, MediaTrimmingPreference.Fast);

                merge.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        MergingProgressRing.Value = progress;
                    });
                });

                merge.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        MergingProgressRing.Value = 0;

                        RecordButton.Visibility = Visibility.Visible;
                        ProcessingNotification.Visibility = Visibility.Collapsed;
                        _tempFile = newFile;

                        NotifyRecordingStatusChanges(false);

                        await Save(_tempFile);
                        PreviewFrame.Visibility = Visibility.Visible;
                        PreviewFrame.Content = new VideoPreviewPage(_tempFile);

                        var videofolder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

                        await videoFile.DeleteAsync();
                        await internalAudioFile.DeleteAsync();
                    });
                });
                lockAdaptiveUI = false;
            }
            else
            {
                NotifyRecordingStatusChanges(false);
                await Save(_tempFile);
                PreviewFrame.Visibility = Visibility.Visible;
                PreviewFrame.Content = new VideoPreviewPage(_tempFile);
            }
        }

        private async Task StopRecordingAsync()
        {
            // Collecting some info before being lost

            if (App.Settings.IntAudio && loopbackAudioCapture.Started)
            {
                audioEncodingProperties = loopbackAudioCapture.EncodingProperties;
                await loopbackAudioCapture.Stop();
            }

            if (mediaCapture != null)
            {
                await mediaCapture.StopRecordAsync();
            }

            App.RecorderHelper.Encoder?.Dispose();
        }

        async Task<byte[]> Convert(IRandomAccessStream s)
        {
            var dr = new DataReader(s.GetInputStreamAt(0));
            var bytes = new byte[s.Size];
            await dr.LoadAsync((uint)s.Size);
            dr.ReadBytes(bytes);
            return bytes;
        }

        private async Task<bool> IsMicAllowed()
        {
            bool isMicAvailable = true;
            try
            {
                MediaCapture mediaCapture = new();
                MediaCaptureInitializationSettings settings = new()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };
                await mediaCapture.InitializeAsync(settings);
                mediaCapture.Dispose();
            }
            catch (Exception)
            {
                isMicAvailable = false;
            }

            return isMicAvailable;
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

        public async Task CompleteRecording(byte[] audioBuffer, uint width, uint height, uint bitrateInBps, uint frameRate)
        {
            var clip = await MediaClip.CreateFromFileAsync(_tempFile);
            var composition = new MediaComposition();
            composition.Clips.Add(clip);

            StorageFile internalAudioFile = await GetAudioTempFileAsync(audioBuffer);

            if (internalAudioFile != null)
            {
                var backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(internalAudioFile);
                composition.BackgroundAudioTracks.Add(backgroundTrack);

                var videoFile = _tempFile;

                var newFile = await GetTempFileAsync();

                RecordButton.Visibility = Visibility.Collapsed;
                ProcessingNotification.Visibility = Visibility.Visible;
                RecordingNotification.Visibility = Visibility.Collapsed;
                RecordButton.IsEnabled = false;

                var merge = composition.RenderToFileAsync(newFile, MediaTrimmingPreference.Fast);

                merge.Progress = new AsyncOperationProgressHandler<TranscodeFailureReason, double>(async (info, progress) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                    {
                        MergingProgressRing.Value = progress;
                    }));
                });

                merge.Completed = new AsyncOperationWithProgressCompletedHandler<TranscodeFailureReason, double>(async (info, status) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(async () =>
                    {
                        MergingProgressRing.Value = 0;
                        _tempFile = newFile;

                        RecordButton.IsEnabled = true;
                        NotifyRecordingStatusChanges(false);

                        await Save(_tempFile);
                        PreviewFrame.Visibility = Visibility.Visible;
                        PreviewFrame.Content = new VideoPreviewPage(_tempFile);
                        var folder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

                        await videoFile.DeleteAsync();
                        await internalAudioFile.DeleteAsync();
                    }));
                    lockAdaptiveUI = false;
                });
            }
            else
            {
                NotifyRecordingStatusChanges(true);
                await Save(_tempFile);
                PreviewFrame.Visibility = Visibility.Visible;
                PreviewFrame.Content = new VideoPreviewPage(_tempFile);
            }
        }

        private async Task<bool> Save(StorageFile file)
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
            await file.CopyAndReplaceAsync(newFile);
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
            if (audioEncodingProperties != null)
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
            else
            {
                StorageFile file = null;
                return file;
            }

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
            return (uint)hresult switch
            {
                // MF_E_TRANSFORM_TYPE_NOT_SET
                0xC00DA412 => "The combination of options you've chosen are not supported by your hardware.",
                0x80070070 => "There is not enough space for recording in your device. ",
                0xC00D4A44 => "The recorder wasn't able to capture enough frames.",
                _ => "An unknown error occured while recording.",
            };
        }

        public void NotifyRecordingStatusChanges(bool isRecording)
        {
            if (isRecording)
            {
                RecordingMiniOptions.Visibility = Visibility.Collapsed;
                RecordName.Text = Strings.Resources.Stop;
                RecordButton.SetValue(AutomationProperties.NameProperty, Strings.Resources.Stop);
                StopRecIcon.Glyph = "\uE15B";
                RecordingContainer.Visibility = Visibility.Visible;
                MainContent.Visibility = Visibility.Collapsed;
                SettingsButton.Visibility = Visibility.Collapsed;
                lockAdaptiveUI = true;
            } else
            {
                App.RecorderHelper.SetAppSize(_userSize);
                RecordingMiniOptions.Visibility = Visibility.Visible;
                RecordName.Text = Strings.Resources.Record;
                RecordButton.SetValue(AutomationProperties.NameProperty, Strings.Resources.Record);
                StopRecIcon.Glyph = "\uE7C8";
                RecordButton.Visibility = Visibility.Visible;
                ProcessingNotification.Visibility = Visibility.Collapsed;
                RecordingNotification.Visibility = Visibility.Visible;
                RecordingContainer.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
                SettingsButton.Visibility = Visibility.Visible;
                lockAdaptiveUI = false;
            }
        }

        public async void Image_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ThumbItem item = (sender as Image).DataContext as ThumbItem;
            var videoFile = await (await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder")).GetFileAsync(item.fileN);

            if (App.Settings.SystemPlayer)
                await Launcher.LaunchFileAsync(videoFile);
            else
                Frame.Navigate(typeof(PlayerPage), videoFile);
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new(412, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                if (modeSwitched)
                {
                    GoToOverlayIcon.Visibility = Visibility.Collapsed;
                    ExitOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new()
                    {
                        Content = Strings.Resources.ExitOverlay
                    };
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.ExitOverlay);
                }
            }
            else
            {
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                if (modeSwitched)
                {
                    ExitOverlayIcon.Visibility = Visibility.Collapsed;
                    GoToOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new()
                    {
                        Content = Strings.Resources.GoToOverlay
                    };
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!lockAdaptiveUI)
            {
                if (filesInFolder && App.Settings.Gallery && e.NewSize.Width > 680)
                {
                    BasicGridView.Visibility = Visibility.Visible;
                    NoVideosContainer.Visibility = Visibility.Collapsed;
                }
                else if (App.Settings.Gallery && !filesInFolder)
                {
                    BasicGridView.Visibility = Visibility.Collapsed;
                    NoVideosContainer.Visibility = Visibility.Visible;
                }
                else if (!App.Settings.Gallery)
                {
                    BasicGridView.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetupTitleBar(CoreApplicationViewTitleBar coreAppTitleBar = null)
        {
            var coreTitleBar = coreAppTitleBar ?? CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Get the size of the caption controls area and back button 
            // (returned in logical pixels), and move your content around as necessary.
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);

            // Set XAML element as a draggable region.
            Window.Current.SetTitleBar(UserLayout);

            // Register a handler for when the size of the overlaid caption control changes.
            // For example, when the app moves to a screen with a different DPI.
            coreTitleBar.LayoutMetricsChanged += OnTitleBarLayoutMetricsChanged;
        }

        public void OnTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            SetupTitleBar(sender);
        }

        private async void MicSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"ms-settings:sound";
            var uri = new Uri(uriToLaunch);
            await Launcher.LaunchUriAsync(uri);
        }

        private async void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"https://paypal.me/FilippoFedeli";
            var uri = new Uri(uriToLaunch);
            await Launcher.LaunchUriAsync(uri);
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await recordedVideoFile.DeleteAsync();
            await LoadThumbanails();
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(DataRequested);
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            DataRequest request = e.Request;
            request.Data.Properties.Title = recordedVideoFile.Name;
            request.Data.SetStorageItems(new StorageFile[] { recordedVideoFile });
        }

        private async void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            var frameRate = await recordedVideoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameRate" });
            var width = await recordedVideoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameWidth" });
            var height = await recordedVideoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameHeight" });
            ContentDialog dialog = new VideoInfoDialog(frameRate, width, height);
            await dialog.ShowAsync();
        }

        private async void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {
            var folderLocation = await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder");
            var options = new FolderLauncherOptions();
            options.ItemsToSelect.Add(recordedVideoFile);
            await Launcher.LaunchFolderAsync(folderLocation, options);
        }

        public async void Image_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            ThumbItem item = (sender as Image).DataContext as ThumbItem;
            recordedVideoFile = await (await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder")).GetFileAsync(item.fileN);
        }

        private void ResolutionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.Width = (e.AddedItems[0] as ResolutionItem).Resolution.Width;
            App.Settings.Height = (e.AddedItems[0] as ResolutionItem).Resolution.Height;
        }

        private void FrameRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.FrameRate = (e.AddedItems[0] as FrameRateItem).FrameRate;
        }

        private void BitrateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.Settings.Bitrate = (e.AddedItems[0] as BitrateItem).Bitrate;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void BasicGridView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement).DataContext is ThumbItem item)
            {
                recordedVideoFile = await (await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder")).GetFileAsync(item.fileN);
                BasicGridFlyout.ShowAt(BasicGridView, e.GetPosition(BasicGridView));
            }
        }
    }
}
