using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

namespace FluentScreenRecorder
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public CustomMediaTransportControls()
        {
            DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        public event EventHandler Deleted;
        public event EventHandler InfoTap;
        public event EventHandler Shared;

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button DeleteButton = GetTemplateChild("DeleteButton") as Button;            
            DeleteButton.Click += DeleteButton_Click;
            ToolTip toolTip1 = new ToolTip();
            toolTip1.Content = Strings.Resources.Delete;
            ToolTipService.SetToolTip(DeleteButton, toolTip1);
            AutomationProperties.SetName(DeleteButton, Strings.Resources.Delete);
            Button InfoButton = GetTemplateChild("InfoButton") as Button;
            InfoButton.Click += InfoButton_Click;
            ToolTip toolTip2 = new ToolTip();
            toolTip2.Content = Strings.Resources.Info;
            ToolTipService.SetToolTip(InfoButton, toolTip2);
            AutomationProperties.SetName(InfoButton, Strings.Resources.Info);
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
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            InfoTap?.Invoke(this, EventArgs.Empty);
        }

        private void Share_Click(object sender, RoutedEventArgs e)
        {
            Shared?.Invoke(this, EventArgs.Empty);
        }

        
    }
}