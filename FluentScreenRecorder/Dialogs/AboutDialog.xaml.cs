using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FluentScreenRecorder.Dialogs
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            InitializeComponent();
            VersionNumberTextBlock.Text = GetAppVersion();
        }

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format(" {0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string gitHubIssue = @"https://github.com/MarcAnt01/Fluent-Screen-Recorder/issues/new";
            var uri = new Uri(gitHubIssue);
            var uriOpened = await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        
    }
}
