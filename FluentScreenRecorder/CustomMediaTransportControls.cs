using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace FluentScreenRecorder
{
    public sealed class CustomMediaTransportControls : MediaTransportControls 
    {
        
        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button DeleteButton = GetTemplateChild("DeleteButton") as Button;
            DeleteButton.Click += DeleteButton_Click;

            base.OnApplyTemplate();
        }

        public event EventHandler Deleted;

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'like' is clicked
            Deleted?.Invoke(this, EventArgs.Empty);
        }


    }
}
