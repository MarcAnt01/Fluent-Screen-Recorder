using FluentScreenRecorder.Dialogs;
using System;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder.Views
{
    public sealed partial class PlayerPage : Page
    {
        private StorageFile videoFile;

        public PlayerPage(StorageFile file = null)
        {
            this.InitializeComponent();
        }

        public PlayerPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;            
            Window.Current.SetTitleBar(UserLayout);            
            if (e.Parameter is StorageFile file)
            {
                videoFile = file;
                VideoPlayer.Source = MediaSource.CreateFromStorageFile(file);                
            }            
        }

        private async void CustomMediaTransportControls_Deleted(object sender, EventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
            await videoFile.DeleteAsync();
        }

        private async void CustomMediaTransportControls_InfoTap(object sender, EventArgs e)
        {
            var frameRate = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameRate" });
            var width = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameWidth" });
            var height = await videoFile.Properties.RetrievePropertiesAsync(new string[] { "System.Video.FrameHeight" });
            ContentDialog dialog = new VideoInfoDialog(frameRate, width, height);
            await dialog.ShowAsync();
        }

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
