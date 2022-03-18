// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Windows.UI.Xaml;

namespace Microsoft.PowerToys.Settings.UI.Controls
{
    public class PageLink
    {
        public string Text { get; set; }

        public Uri Link { get; set; }

        public event RoutedEventHandler Click;

        public void OnClick(object s, RoutedEventArgs e)
        {
            Click?.Invoke(s, e);
        }
    }
}
