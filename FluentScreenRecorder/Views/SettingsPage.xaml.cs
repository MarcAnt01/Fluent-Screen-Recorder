﻿using CaptureEncoder;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

namespace FluentScreenRecorder.Views
{

    public sealed partial class SettingsPage : Page
    {

        public SettingsPage()
        {
            InitializeComponent();
            SetupTitleBar();
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;

            this.Loaded += LoadedHandler;

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
            AudioToggleSwitch.IsOn = settings.IntAudio;
            ExtAudioToggleSwitch.IsOn = settings.ExtAudio;
            GalleryToggleSwitch.IsOn = settings.Gallery;
            SystemPlayerToggleSwitch.IsOn = settings.SystemPlayer;
            OverlayToggleSwitch.IsOn = settings.ShowOnTop;

            AppVersionText.Text = $"Version: {GetAppVersion()}";
            AppVersionText1.Text = $"Version: {GetAppVersion()}";
        }

        private AppSettings GetCurrentSettings()
        {
            var resolutionItem = (ResolutionItem)ResolutionComboBox.SelectedItem;
            var width = resolutionItem.Resolution.Width;
            var height = resolutionItem.Resolution.Height;
            var bitrateItem = _bitrates[GetBitrateIndex(GetCachedSettings().Bitrate)];
            var bitrate = bitrateItem.Bitrate;
            var frameRateItem = _frameRates[GetFrameRateIndex(GetCachedSettings().FrameRate)];
            var frameRate = frameRateItem.FrameRate;
            var useSourceSize = UseCaptureItemToggleSwitch.IsOn;
            var intAudio = AudioToggleSwitch.IsOn;
            var extAudio = ExtAudioToggleSwitch.IsOn;
            var gallery = GalleryToggleSwitch.IsOn;
            var systemPlayer = SystemPlayerToggleSwitch.IsOn;
            var showOnTop = OverlayToggleSwitch.IsOn;
            return new AppSettings { Width = width, Height = height, Bitrate = bitrate, FrameRate = frameRate, UseSourceSize = useSourceSize, IntAudio = intAudio, ExtAudio = extAudio, Gallery = gallery, SystemPlayer = systemPlayer, ShowOnTop = showOnTop };

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
                IntAudio = true,
                ExtAudio = false,
                Gallery = true,
                SystemPlayer = false,
                ShowOnTop = false
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

            if (localSettings.Values.TryGetValue(nameof(AppSettings.IntAudio), out var intAudio))
            {
                result.IntAudio = (bool)intAudio;
            }

            if (localSettings.Values.TryGetValue(nameof(AppSettings.ExtAudio), out var extAudio))
            {
                result.ExtAudio = (bool)extAudio;
            }

            if (localSettings.Values.TryGetValue(nameof(AppSettings.Gallery), out var gallery))
            {
                result.Gallery = (bool)gallery;
            }

            if (localSettings.Values.TryGetValue(nameof(AppSettings.SystemPlayer), out var systemPlayer))
            {
                result.SystemPlayer = (bool)systemPlayer;
            }

            if (localSettings.Values.TryGetValue(nameof(AppSettings.ShowOnTop), out var showOnTop))
            {
                result.ShowOnTop = (bool)showOnTop;
            }
            return result;
        }
        public void CacheCurrentSettings()
        {
            var settings = GetCurrentSettings();
            CacheSettings(settings);
        }

        private async void LoadedHandler(object sender, RoutedEventArgs e)
        {
            this.Loaded -= LoadedHandler;
            if (GetCachedSettings().ShowOnTop)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 600);
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
                ToolTip toolTip = new ToolTip();
                toolTip.Content = Strings.Resources.GoToOverlay;
                ToolTipService.SetToolTip(OverlayButton, toolTip);
                AutomationProperties.SetName(OverlayButton, Strings.Resources.GoToOverlay);
            }
        }

        private static void CacheSettings(AppSettings settings)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[nameof(AppSettings.Width)] = settings.Width;
            localSettings.Values[nameof(AppSettings.Height)] = settings.Height;
            localSettings.Values[nameof(AppSettings.Bitrate)] = settings.Bitrate;
            localSettings.Values[nameof(AppSettings.FrameRate)] = settings.FrameRate;
            localSettings.Values[nameof(AppSettings.UseSourceSize)] = settings.UseSourceSize;
            localSettings.Values[nameof(AppSettings.IntAudio)] = settings.IntAudio;
            localSettings.Values[nameof(AppSettings.ExtAudio)] = settings.ExtAudio;
            localSettings.Values[nameof(AppSettings.Gallery)] = settings.Gallery;
            localSettings.Values[nameof(AppSettings.SystemPlayer)] = settings.SystemPlayer;
            localSettings.Values[nameof(AppSettings.ShowOnTop)] = settings.ShowOnTop;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CacheCurrentSettings();
            this.Frame.Navigate(typeof(MainPage));
        }

        private async void MicSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"ms-settings:sound";
            var uri = new Uri(uriToLaunch);
            await Launcher.LaunchUriAsync(uri);
        }

        private async void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"https://paypal.me/pools/c/8Bxl3GiJqn";
            var uri = new Uri(uriToLaunch);
            await Launcher.LaunchUriAsync(uri);
        }

        private async void OverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                var preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                preferences.CustomSize = new Size(400, 600);
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

        struct AppSettings
        {
            public uint Width;
            public uint Height;
            public uint Bitrate;
            public uint FrameRate;
            public bool UseSourceSize;
            public bool IntAudio;
            public bool ExtAudio;
            public bool Gallery;
            public bool SystemPlayer;
            public bool ShowOnTop;
        }
        private List<ResolutionItem> _resolutions;
        private List<BitrateItem> _bitrates;
        private List<FrameRateItem> _frameRates;

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format(" {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }

}
