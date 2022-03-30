using System;
using Windows.UI.Xaml;

namespace Microsoft.PowerToys.Settings.UI
{
    public class SettingsThemeResources : ResourceDictionary
    {
        public SettingsThemeResources()
        {
            MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Microsoft.PowerToys.Settings.UI/Expander.xaml") });
            MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Microsoft.PowerToys.Settings.UI/Settings.xaml") });
            MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Microsoft.PowerToys.Settings.UI/Styles/TextBlock.xaml") });
            MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Microsoft.PowerToys.Settings.UI/Styles/Button.xaml") });
        }
    }
}
