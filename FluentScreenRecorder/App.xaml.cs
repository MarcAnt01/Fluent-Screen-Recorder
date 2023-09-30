using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Capture;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.ExtendedExecution.Foreground;
using FluentScreenRecorder.ViewModels;
using CaptureEncoder;
using FluentScreenRecorder.Models;
using System.Collections.Generic;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace FluentScreenRecorder
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static RecorderHelper RecorderHelper { get; private set; }
        public static SettingsViewModel Settings { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
#if !DEBUG
            AppCenter.Start(APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            ExtendExecution();

            RecorderHelper = new();
            Settings = new();
        }

        private ExtendedExecutionForegroundSession _extendedSession;

        private async Task ExtendExecution()
        {
            using var session = new ExtendedExecutionForegroundSession { Reason = ExtendedExecutionForegroundReason.Unspecified };
            var result = await session.RequestExtensionAsync();

            if (result == ExtendedExecutionForegroundResult.Allowed)
                _extendedSession = session;
        }

        private void InitApp()
        {
            WhatsNewDisplayService.ShowIfAppropriate();
            SetupSpecs();
        }

        public static class WhatsNewDisplayService
        {
            private static bool shown = false;

            public static void ShowIfAppropriate()
            {
                if (SystemInformation.Instance.IsAppUpdated && !shown)
                {
                    shown = true;
                    var dialog = new ChangelogDialog();
                    _ = dialog.ShowAsync();
                }
            }
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active 
            if (Window.Current.Content is not Frame rootFrame)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                rootFrame.SizeChanged += RootFrame_SizeChanged;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                InitApp();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (!e.PrelaunchActivated)
            {
                // On Windows 10 version 1607 or later, this code signals that this app wants to participate in prelaunch
                if (canEnablePrelaunch)
                    CoreApplication.EnablePrelaunch(true);

                if (rootFrame.Content == null)
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);

                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private void RootFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RecorderHelper.IsRecording)
                return;

            Settings.Size = e.NewSize;
        }

        private void SetupSpecs()
        {
            if (RecorderHelper.Initialized)
                return;

            RecorderHelper.Device = Direct3D11Helpers.CreateDevice();

            RecorderHelper.Resolutions = new List<ResolutionItem>();
            foreach (var resolution in EncoderPresets.Resolutions)
            {
                RecorderHelper.Resolutions.Add(new ResolutionItem()
                {
                    DisplayName = $"{resolution.Width} x {resolution.Height}",
                    Resolution = resolution,
                });
            }
            RecorderHelper.Resolutions.Add(new ResolutionItem()
            {
                DisplayName = Strings.Resources.SourceSizeToggle,
                Resolution = new SizeUInt32() { Width = 0, Height = 0 },
            });

            RecorderHelper.Bitrates = new();
            foreach (var bitrate in EncoderPresets.Bitrates)
            {
                var mbps = (float)bitrate / 1000000;
                RecorderHelper.Bitrates.Add(new BitrateItem()
                {
                    DisplayName = $"{mbps:0.##} Mbps",
                    Bitrate = bitrate,
                });
            }

            RecorderHelper.Framerates = new List<FrameRateItem>();
            foreach (var frameRate in EncoderPresets.FrameRates)
            {
                RecorderHelper.Framerates.Add(new FrameRateItem()
                {
                    DisplayName = $"{frameRate}fps",
                    FrameRate = frameRate,
                });
            }

            RecorderHelper.Initialized = true;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}
