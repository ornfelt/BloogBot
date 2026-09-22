// Added by the .NET 9 port - no counterpart in BloogBot. Replica rules do not apply to it.
using BloogBot.UI.Abstractions;
using System;
using System.Windows;

namespace BloogBot.UI.Wpf
{
    // The WPF half of IThemeService: Application.Current.Resources.MergedDictionaries[0] is the
    // theme slot, and replacing it repaints every {DynamicResource} in the live window.
    public class WpfThemeService : IThemeService
    {
        UiTheme current = UiTheme.Dark;

        public UiTheme Current => current;

        public event EventHandler ThemeChanged;

        public void Apply(UiTheme theme)
        {
            var uri = new Uri("Themes/" + (theme == UiTheme.Light ? "Light" : "Dark") + ".xaml", UriKind.Relative);
            var dictionary = (ResourceDictionary)Application.LoadComponent(uri);

            var merged = Application.Current.Resources.MergedDictionaries;
            if (merged.Count == 0)
                merged.Add(dictionary);
            else
                merged[0] = dictionary;

            current = theme;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
