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
        public event EventHandler Deleted2;
        public event EventHandler SaveAs;
        public event EventHandler Shared2;

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button DeleteButton = GetTemplateChild("DeleteButton") as Button;
            if (DeleteButton != null)
            {
                DeleteButton.Click += DeleteButton_Click;
                ToolTip toolTip1 = new ToolTip();
                toolTip1.Content = Strings.Resources.Delete;
                ToolTipService.SetToolTip(DeleteButton, toolTip1);
                AutomationProperties.SetName(DeleteButton, Strings.Resources.Delete);
            }
            
            Button InfoButton = GetTemplateChild("InfoButton") as Button;
            if (InfoButton != null)
            {
                InfoButton.Click += InfoButton_Click;
                ToolTip toolTip2 = new ToolTip();
                toolTip2.Content = Strings.Resources.Info;
                ToolTipService.SetToolTip(InfoButton, toolTip2);
                AutomationProperties.SetName(InfoButton, Strings.Resources.Info);
            }
            
            Button ShareButton = GetTemplateChild("ShareButton") as Button;
            if (ShareButton != null)
            {
                ShareButton.Click += Share_Click;
                ToolTip toolTip3 = new ToolTip();
                toolTip3.Content = Strings.Resources.Share;
                ToolTipService.SetToolTip(ShareButton, toolTip3);
                AutomationProperties.SetName(ShareButton, Strings.Resources.Share);
            }

            Button DeleteButton2 = GetTemplateChild("DeleteButton2") as Button;
            if (DeleteButton2 != null)
            {
                DeleteButton2.Click += DeleteButton2_Click;
                ToolTip toolTip1 = new ToolTip();
                toolTip1.Content = Strings.Resources.Delete;
                ToolTipService.SetToolTip(DeleteButton2, toolTip1);
                AutomationProperties.SetName(DeleteButton2, Strings.Resources.Delete);
            }            
            
            Button SaveAsButton = GetTemplateChild("SaveAsButton") as Button;
            if (SaveAsButton != null)
            {
                SaveAsButton.Click += SaveAs_Click;
                ToolTip toolTip2 = new ToolTip();
                toolTip2.Content = Strings.Resources.SaveAs;
                ToolTipService.SetToolTip(SaveAsButton, toolTip2);
                AutomationProperties.SetName(SaveAsButton, Strings.Resources.SaveAs);
            }
            
            Button ShareButton2 = GetTemplateChild("ShareButton2") as Button;
            if (ShareButton2 != null)
            {
                ShareButton2.Click += Share2_Click;
                ToolTip toolTip3 = new ToolTip();
                toolTip3.Content = Strings.Resources.Share;
                ToolTipService.SetToolTip(ShareButton2, toolTip3);
                AutomationProperties.SetName(ShareButton2, Strings.Resources.Share);

            }           

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
        private void DeleteButton2_Click(object sender, RoutedEventArgs e)
        {
            Deleted2?.Invoke(this, EventArgs.Empty);
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAs?.Invoke(this, EventArgs.Empty);
        }

        private void Share2_Click(object sender, RoutedEventArgs e)
        {
            Shared2?.Invoke(this, EventArgs.Empty);
        }


    }
}