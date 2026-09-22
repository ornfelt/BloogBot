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
            // An absolute pack URI naming the assembly, not a relative one. hostfxr loads
            // BloogBot.dll as a component rather than as an entry-point app, so
            // Assembly.GetEntryAssembly() is null - and the single-argument
            // Application.LoadComponent resolves a relative URI against it, throwing
            // "Assembly.GetEntryAssembly() returns null". Naming the assembly sidesteps it.
            var uri = new Uri("pack://application:,,,/BloogBot.UI.Wpf;component/Themes/"
                + (theme == UiTheme.Light ? "Light" : "Dark") + ".xaml", UriKind.Absolute);
            // ResourceDictionary.Source, not Application.LoadComponent: the single-argument
            // LoadComponent overload rejects an absolute URI outright (ArgumentException,
            // "Cannot use absolute URI"), while Source is happy with an absolute pack URI and
            // does not consult Assembly.GetEntryAssembly() at all.
            var dictionary = new ResourceDictionary { Source = uri };

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
