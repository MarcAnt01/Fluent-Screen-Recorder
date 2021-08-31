using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class VideoInfoDialog : ContentDialog
    {
        public VideoInfoDialog()
        {
            
        }

        public VideoInfoDialog(IDictionary<string, object> frameRate, IDictionary<string, object> width, IDictionary <string, object> height)
        {
            this.InitializeComponent();
            
            if (frameRate.TryGetValue("System.Video.FrameRate", out object frameRateValue) && frameRateValue is UInt32 framerate)
            {
                Frameblock.Text = $"Framerate {framerate / 1000d}";
            }

            if (width.TryGetValue("System.Video.FrameWidth", out object frameWidthValue) && frameWidthValue is UInt32 widthvalue)
            {
                Widthblock.Text = $"Width {widthvalue}";
            }

            if (height.TryGetValue("System.Video.FrameHeight", out object frameHeightValue) && frameHeightValue is UInt32 heightvalue)
            {
                Heightblock.Text = $"Height {heightvalue}";
            }




        }
        

       
    }
}
