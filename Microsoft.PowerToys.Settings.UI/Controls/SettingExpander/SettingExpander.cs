﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;

namespace Microsoft.PowerToys.Settings.UI.Controls
{
    public partial class SettingExpander : Expander
    {
        public SettingExpander()
        {
            DefaultStyleKey = typeof(Expander);
            this.Style = (Style)Application.Current.Resources["SettingExpanderStyle"];
            this.RegisterPropertyChangedCallback(Expander.HeaderProperty, OnHeaderChanged);
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyProperty dp)
        {
            SettingExpander self = (SettingExpander)d;
            if (self.Header != null)
            {
                if (self.Header.GetType() == typeof(Setting))
                {
                    Setting selfSetting = (Setting)self.Header;
                    selfSetting.Style = (Style)Application.Current.Resources["ExpanderHeaderSettingStyle"];

                    if (!string.IsNullOrEmpty(selfSetting.Header))
                    {
                        AutomationProperties.SetName(self, selfSetting.Header);
                    }
                }
            }
        }
    }
}
