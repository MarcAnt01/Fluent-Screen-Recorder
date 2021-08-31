using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FluentScreenRecorder
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        public event EventHandler Deleted;
        public event EventHandler InfoTap;

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button DeleteButton = GetTemplateChild("DeleteButton") as Button;
            DeleteButton.Click += DeleteButton_Click;
            Button InfoButton = GetTemplateChild("InfoButton") as Button;
            InfoButton.Click += InfoButton_Click;

            base.OnApplyTemplate();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'like' is clicked
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            InfoTap?.Invoke(this, EventArgs.Empty);
        }

        
    }
}