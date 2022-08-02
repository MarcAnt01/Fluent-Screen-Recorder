using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

namespace FluentScreenRecorder
{
    public sealed class CustomMediaTransportControls2 : MediaTransportControls
    {
        public CustomMediaTransportControls2()
        {
            DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        public event EventHandler Deleted_Preview;
        public event EventHandler SaveAs_Preview;
        public event EventHandler Shared_Preview;

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button DeleteButton = GetTemplateChild("DeleteButton") as Button;
            DeleteButton.Click += DeleteButton_Click;
            ToolTip toolTip1 = new ToolTip();
            toolTip1.Content = Strings.Resources.Delete;
            ToolTipService.SetToolTip(DeleteButton, toolTip1);
            AutomationProperties.SetName(DeleteButton, Strings.Resources.Delete);
            Button SaveAsButton = GetTemplateChild("SaveAsButton") as Button;
            //SaveAs.Click += SaveAs_Click;
            ToolTip toolTip2 = new ToolTip();
            toolTip2.Content = Strings.Resources.SaveAs;
            ToolTipService.SetToolTip(SaveAsButton, toolTip2);
            AutomationProperties.SetName(SaveAsButton, Strings.Resources.SaveAs);
            Button ShareButton = GetTemplateChild("ShareButton") as Button;
            ShareButton.Click += Share_Click;
            ToolTip toolTip3 = new ToolTip();
            toolTip3.Content = Strings.Resources.Share;
            ToolTipService.SetToolTip(ShareButton, toolTip3);
            AutomationProperties.SetName(ShareButton, Strings.Resources.Share);

            base.OnApplyTemplate();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Deleted_Preview?.Invoke(this, EventArgs.Empty);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs_Preview?.Invoke(this, EventArgs.Empty);
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            Shared_Preview?.Invoke(this, EventArgs.Empty);
        }


    }
}