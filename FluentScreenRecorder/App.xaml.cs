﻿using Microsoft.Toolkit.Uwp.Helpers;
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
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Windows.ApplicationModel.ExtendedExecution.Foreground;

namespace FluentScreenRecorder
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
#if !DEBUG
            AppCenter.Start(APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
            ExtendExecution();            
        }

        private ExtendedExecutionForegroundSession _extendedSession;

        private async void ExtendExecution()
        {
            var session = new ExtendedExecutionForegroundSession { Reason = ExtendedExecutionForegroundReason.Unspecified };
            var result = await session.RequestExtensionAsync();

            if (result == ExtendedExecutionForegroundResult.Allowed)
            {
                _extendedSession = session;                
            }
            else
            {
                session.Dispose();                
            }
        }

        private async Task StartupAsync()
        {
            await WhatsNewDisplayService.ShowIfAppropriateAsync();
        }

        public static class WhatsNewDisplayService
        {
            private static bool shown = false;

            internal static async Task ShowIfAppropriateAsync()
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, async () =>
                    {
                        if (SystemInformation.IsAppUpdated && !shown)
                        {
                            shown = true;
                            var dialog = new ChangelogDialog();
                            await dialog.ShowAsync();
                        }
                    });
            }
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            bool canEnablePrelaunch = Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.CoreApplication", "EnablePrelaunch");
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active 
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                await StartupAsync();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                // On Windows 10 version 1607 or later, this code signals that this app wants to participate in prelaunch
                if (canEnablePrelaunch)
                {
                    TryEnablePrelaunch();
                }

                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private void TryEnablePrelaunch()
        {
            Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);
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

            // Save our state
            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame != null)
            {
                var page = rootFrame.Content as MainPage;

                if (page != null && GraphicsCaptureSession.IsSupported())
                {
                    page.CacheCurrentSettings();
                }
            }

            deferral.Complete();
        }
    }
}
