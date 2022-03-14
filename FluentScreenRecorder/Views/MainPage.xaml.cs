﻿using CaptureEncoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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
using Windows.UI.Popups;
using System.Linq;
using Windows.Media.Capture;
using Windows.Storage.Streams;
using Windows.ApplicationModel.DataTransfer;
using FluentScreenRecorder.Models;

namespace FluentScreenRecorder
{
    public sealed partial class MainPage : Page
    {
        private LoopbackAudioCapture loopbackAudioCapture;
        private Visual visual;
        private ToolTip toolTip;
        private List<byte> BufferList = new();
        MediaPlayer SilentPlayer;
        private AudioEncodingProperties audioEncodingProperties;
        private StorageFile micFile;
        public MediaCapture mediaCapture;
        public StorageFile recordedVideoFile = null;
        private bool lockAdaptiveUI;

        public MainPage()
        {
            InitializeComponent();
            Loaded += LoadedHandler;

            MergingProgressRing.Visibility = Visibility.Collapsed;

            SilentPlayer = new MediaPlayer() { IsLoopingEnabled = true };
            SilentPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/Silence.ogg"));
            SilentPlayer.Play();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(400, 260));

            //hide titlebar
            SetupTitleBar();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;

            //Record icon
            RecordIcon.Visibility = Visibility.Visible;
            StopIcon.Visibility = Visibility.Collapsed;
            RecordingContainer.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            Ellipse.Visibility = Visibility.Collapsed;
            ToolTip toolTip = new ToolTip();
            toolTip.Content = Strings.Resources.RecordingStart;
            ToolTipService.SetToolTip(MainButton, toolTip);
            AutomationProperties.SetName(MainButton, Strings.Resources.RecordingStart);

            App.RecViewModel.Device = Direct3D11Helpers.CreateDevice();

            App.RecViewModel.Resolutions = new List<ResolutionItem>();
            foreach (var resolution in EncoderPresets.Resolutions)
            {
                App.RecViewModel.Resolutions.Add(new ResolutionItem()
                {
                    DisplayName = $"{resolution.Width} x {resolution.Height}",
                    Resolution = resolution,
                });
            }
            App.RecViewModel.Resolutions.Add(new ResolutionItem()
            {
                DisplayName = Strings.Resources.SourceSizeToggle,
                Resolution = new SizeUInt32() { Width = 0, Height = 0 },
            });
            ResolutionComboBox.ItemsSource = App.RecViewModel.Resolutions;
            ResolutionComboBox.SelectedIndex = App.RecViewModel.GetResolutionIndex(App.Settings.Width, App.Settings.Height);

            App.RecViewModel.Bitrates = new();
            foreach (var bitrate in EncoderPresets.Bitrates)
            {
                var mbps = (float)bitrate / 1000000;
                App.RecViewModel.Bitrates.Add(new BitrateItem()
                {
                    DisplayName = $"{mbps:0.##} Mbps",
                    Bitrate = bitrate,
                });
            }
            BitrateComboBox.ItemsSource = App.RecViewModel.Bitrates;
            BitrateComboBox.SelectedIndex = App.RecViewModel.GetBitrateIndex(App.Settings.Bitrate);

            App.RecViewModel.Framerates = new List<FrameRateItem>();
            foreach (var frameRate in EncoderPresets.FrameRates)
            {
                App.RecViewModel.Framerates.Add(new FrameRateItem()
                {
                    DisplayName = $"{frameRate}fps",
                    FrameRate = frameRate,
                });
            }
            FrameRateComboBox.ItemsSource = App.RecViewModel.Framerates;
            FrameRateComboBox.SelectedIndex = App.RecViewModel.GetFrameRateIndex(App.Settings.FrameRate);

            if (AudioToggleSwitch.IsOn)
            {
                InternalAudioCheck();
            }
        }

        private async void InternalAudioCheck()
        {
            try
            {
                loopbackAudioCapture = new LoopbackAudioCapture(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default));
                await loopbackAudioCapture.Start();
                await loopbackAudioCapture.Stop();
                loopbackAudioCapture = null;
            }
            catch (Exception)
            {
                App.Settings.IntAudio = false;
            }
        }


        public bool filesInFolder;

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            Loaded -= LoadedHandler;
            if (OverlayToggleSwitch.IsOn)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                GoToOverlayIcon.Visibility = Visibility.Collapsed;
                ExitOverlayIcon.Visibility = Visibility.Visible;
                ToolTip toolTip = new ToolTip();
                toolTip.Content = Strings.Resources.ExitOverlay;
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.ExitOverlay);
            }
            else
            {
                ExitOverlayIcon.Visibility = Visibility.Collapsed;
                GoToOverlayIcon.Visibility = Visibility.Visible;
                ToolTip toolTip = new ToolTip
                {
                    Content = Strings.Resources.GoToOverlay
                };
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
            }
            await LoadThumbanails();

            if (filesInFolder && GalleryToggleSwitch.IsOn && ((Frame)Window.Current.Content).ActualWidth > 680)
            {
                SecondColumn.Width = new GridLength(4, GridUnitType.Star);
                ThirdColumn.Width = new GridLength(2, GridUnitType.Star);
            }
            else
            {
                FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                SecondColumn.Width = new GridLength(0);
                ThirdColumn.Width = new GridLength(1, GridUnitType.Star);
            }

            // We don't have to create the video folder at startup - just ignore populating the folder view if the folder doesn't exist (yet).
            // Saving a recording will automatically create the folder if missing.

        }

        private async Task LoadThumbanails()
        {
            if (await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder") is StorageFolder videoFolder)
            {
                IReadOnlyList<StorageFile> storageItems = await videoFolder.GetFilesAsync();
                if (storageItems.Count > 0)
                {
                    List<ThumbItem> thumbnailsList = new();
                    foreach (StorageFile file in storageItems)
                    {
                        StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                        BitmapImage bitmap = new();
                        bitmap.SetSource(thumbnail);
                        thumbnailsList.Add(new() { img = bitmap, fileN = file.Name });
                    }
                    thumbnailsList.Reverse();
                    BasicGridView.ItemsSource = thumbnailsList;
                    filesInFolder = true;
                }
            }

        }

        public StorageFile _tempFile;

        private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;
            var folder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

            // Get our encoder properties
            var frameRateItem = (FrameRateItem)FrameRateComboBox.SelectedItem;
            var resolutionItem = (ResolutionItem)ResolutionComboBox.SelectedItem;
            var bitrateItem = (BitrateItem)BitrateComboBox.SelectedItem;


            MediaCapture mediaCapture = null;

            if (AudioToggleSwitch.IsOn)
            {
                loopbackAudioCapture = new LoopbackAudioCapture(MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default));
                loopbackAudioCapture.BufferReadyDelegate = LoopbackBufferReady;
                BufferList.Clear();
            }
            else if (ExtAudioToggleSwitch.IsOn)
            {
                if (await IsMicAllowed())
                {
                    mediaCapture = new MediaCapture();
                    MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
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
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Recording failed",
                        Content = "Permission to use microphone was not given", //TODO: fix this non-english horror
                        CloseButtonText = "OK"
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
                button.IsChecked = false;
                return;
            }
            if (useSourceSize)
            {
                resolutionItem.IsZero();
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
            RecordName.Text = "Stop";
            StopRecIcon.Glyph = "\uE15B";
            RecordingContainer.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
            Ellipse.Visibility = Visibility.Visible;
            toolTip = new ToolTip();
            toolTip.Content = Strings.Resources.RecordingStop;
            ToolTipService.SetToolTip(MainButton, toolTip);
            AutomationProperties.SetName(MainButton, Strings.Resources.RecordingStop);
            MainTextBlock.Text = Strings.Resources.Recording;
            var originalBrush = MainTextBlock.Foreground;
            MainTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            lockAdaptiveUI = true;

            // Kick off the encoding
            try
            {
                using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                using (App.RecViewModel.Encoder = new(App.RecViewModel.Device, item))
                {
                    if (mediaCapture != null)
                    {
                        await mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), micFile);
                    }
                    var encodesuccess = await App.RecViewModel.Encoder.EncodeAsync(stream, width, height, bitrate, frameRate, loopbackAudioCapture);
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


                MainTextBlock.Foreground = originalBrush;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                RecordName.Text = "Record";
                StopRecIcon.Glyph = "\uE7C8";
                RecordingContainer.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
                toolTip.Content = Strings.Resources.RecordingStart;
                ToolTipService.SetToolTip(MainButton, toolTip);
                AutomationProperties.SetName(MainButton, Strings.Resources.RecordingStart);
                await _tempFile.DeleteAsync();

                return;
            }


            // At this point the encoding has finished,
            // tell the user we're now saving

            if (AudioToggleSwitch.IsOn)
            {
                await CompleteRecording(BufferList.ToArray(), width, height, bitrate, frameRate);
            }
            else if (ExtAudioToggleSwitch.IsOn)
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

                MainTextBlock.Text = Strings.Resources.Saving;
                MergingProgressRing.Visibility = Visibility.Visible;
                MainButton.Visibility = Visibility.Collapsed;

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
                        MergingProgressRing.Visibility = Visibility.Collapsed;
                        _tempFile = newFile;

                        MainButton.IsChecked = false;
                        MainTextBlock.Text = "";
                        visual.StopAnimation("Opacity");
                        Ellipse.Visibility = Visibility.Collapsed;
                        RecordIcon.Visibility = Visibility.Visible;
                        StopIcon.Visibility = Visibility.Collapsed;
                        RecordName.Text = "Record";
                        StopRecIcon.Glyph = "\uE7C8";
                        RecordingContainer.Visibility = Visibility.Collapsed;
                        MainContent.Visibility = Visibility.Visible;
                        ToolTip newtoolTip = new ToolTip();
                        toolTip.Content = Strings.Resources.RecordingStart;
                        ToolTipService.SetToolTip(MainButton, toolTip);
                        AutomationProperties.SetName(MainButton, Strings.Resources.RecordingStart);
                        Frame.Navigate(typeof(VideoPreviewPage), _tempFile);
                        //CacheCurrentSettings();

                        var videofolder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

                        MainButton.Visibility = Visibility.Visible;
                        await videoFile.DeleteAsync();
                        await internalAudioFile.DeleteAsync();
                    }));
                });
                lockAdaptiveUI = false;
            }
            else
            {
                MainButton.IsChecked = false;
                MainTextBlock.Text = "";
                visual.StopAnimation("Opacity");
                Ellipse.Visibility = Visibility.Collapsed;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                RecordName.Text = "Record";
                StopRecIcon.Glyph = "\uE7C8";
                RecordingContainer.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
                ToolTip newtoolTip = new ToolTip();
                toolTip.Content = Strings.Resources.RecordingStart;
                ToolTipService.SetToolTip(MainButton, Strings.Resources.RecordingStart);
                AutomationProperties.SetName(MainButton, "Start recording");
                Frame.Navigate(typeof(VideoPreviewPage), _tempFile);
                //CacheCurrentSettings();
                lockAdaptiveUI = false;
            }
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

        public async void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            //Collecting some info before being lost

            if (AudioToggleSwitch.IsOn && loopbackAudioCapture.Started)
            {
                audioEncodingProperties = loopbackAudioCapture.EncodingProperties;
                await loopbackAudioCapture.Stop();
            }

            if (mediaCapture != null)
            {
                await mediaCapture.StopRecordAsync();
            }

            App.RecViewModel.Encoder?.Dispose();
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

                MainTextBlock.Text = Strings.Resources.Saving;
                MergingProgressRing.Visibility = Visibility.Visible;
                MainButton.Visibility = Visibility.Collapsed;

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
                        MergingProgressRing.Visibility = Visibility.Collapsed;
                        _tempFile = newFile;

                        MainButton.IsChecked = false;
                        MainTextBlock.Text = "";
                        visual.StopAnimation("Opacity");
                        Ellipse.Visibility = Visibility.Collapsed;
                        RecordIcon.Visibility = Visibility.Visible;
                        StopIcon.Visibility = Visibility.Collapsed;
                        RecordName.Text = "Record";
                        StopRecIcon.Glyph = "\uE7C8";
                        RecordingContainer.Visibility = Visibility.Collapsed;
                        MainContent.Visibility = Visibility.Visible;
                        ToolTip newtoolTip = new ToolTip();
                        toolTip.Content = Strings.Resources.RecordingStart;
                        ToolTipService.SetToolTip(MainButton, toolTip);
                        AutomationProperties.SetName(MainButton, Strings.Resources.RecordingStart);

                        Frame.Navigate(typeof(VideoPreviewPage), _tempFile);
                        //CacheCurrentSettings();
                        var folder = await KnownFolders.VideosLibrary.TryGetItemAsync("Fluent Screen Recorder");

                        MainButton.Visibility = Visibility.Visible;
                        await videoFile.DeleteAsync();
                        await internalAudioFile.DeleteAsync();
                    }));
                    lockAdaptiveUI = false;
                });
            }
            else
            {
                MainButton.IsChecked = false;
                MainTextBlock.Text = "";
                visual.StopAnimation("Opacity");
                Ellipse.Visibility = Visibility.Collapsed;
                RecordIcon.Visibility = Visibility.Visible;
                StopIcon.Visibility = Visibility.Collapsed;
                RecordName.Text = "Record";
                StopRecIcon.Glyph = "\uE7C8";
                RecordingContainer.Visibility = Visibility.Collapsed;
                MainContent.Visibility = Visibility.Visible;
                ToolTip newtoolTip = new ToolTip();
                toolTip.Content = Strings.Resources.RecordingStart;
                ToolTipService.SetToolTip(MainButton, Strings.Resources.RecordingStart);
                AutomationProperties.SetName(MainButton, "Start recording");
                Frame.Navigate(typeof(VideoPreviewPage), _tempFile);
                //CacheCurrentSettings();
            }
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

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new AboutDialog();
            await dialog.ShowAsync();
        }

        public async void Image_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ThumbItem item = (sender as Image).DataContext as ThumbItem;
            var videoFile = await (await KnownFolders.VideosLibrary.GetFolderAsync("Fluent Screen Recorder")).GetFileAsync(item.fileN);

            if (SystemPlayerToggleSwitch.IsOn)
            {
                await Launcher.LaunchFileAsync(videoFile);
            }
            else
            {
                Frame.Navigate(typeof(PlayerPage), videoFile);
                //CacheCurrentSettings();
            }
        }


        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 260);
                bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                if (modeSwitched)
                {
                    GoToOverlayIcon.Visibility = Visibility.Collapsed;
                    ExitOverlayIcon.Visibility = Visibility.Visible;
                    ToolTip toolTip = new ToolTip();
                    toolTip.Content = Strings.Resources.ExitOverlay;
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
                    ToolTip toolTip = new ToolTip();
                    toolTip.Content = Strings.Resources.GoToOverlay;
                    ToolTipService.SetToolTip(OverlayButton, toolTip);
                    AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!lockAdaptiveUI)
            {
                if (filesInFolder && GalleryToggleSwitch.IsOn && e.NewSize.Width > 680)
                {
                    SecondColumn.Width = new GridLength(4, GridUnitType.Star);
                    ThirdColumn.Width = new GridLength(2, GridUnitType.Star);
                }
                else
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                    SecondColumn.Width = new GridLength(0);
                    ThirdColumn.Width = new GridLength(1, GridUnitType.Star);
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
    }
}