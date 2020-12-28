using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    public sealed partial class SaveDialog : ContentDialog
    {
        public StorageFile _tempFile;

        public SaveDialog(StorageFile file)
        {
            this.InitializeComponent();

            _tempFile = file;
        }

        private async void Save_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            await MainPage.Save(_tempFile);
        }

        private async void SaveAs_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            await MainPage.SaveAs(_tempFile);
        }

        private async void Cancel_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            await MainPage.Delete(_tempFile);         
        }        
    }
}