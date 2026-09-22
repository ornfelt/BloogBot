// Added by the .NET 9 port - no counterpart in BloogBot. Replica rules do not apply to it.
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using BloogBot.UI.Abstractions;
using System;

namespace BloogBot.UI.Avalonia
{
    // The Avalonia half of IThemeService. Two things move together: the ResourceInclude in
    // Application.Current.Resources.MergedDictionaries[0], which carries the fifteen brush keys
    // every {DynamicResource} in the window resolves against, and RequestedThemeVariant, which is
    // what tells FluentTheme to draw its own chrome dark or light. Replacing the include repaints
    // the live window, exactly as swapping the merged dictionary does in the WPF shell.
    public class AvaloniaThemeService : IThemeService
    {
        static readonly Uri BaseUri = new Uri("avares://BloogBot.UI.Avalonia/");

        UiTheme current = UiTheme.Dark;

        public UiTheme Current => current;

        public event EventHandler ThemeChanged;

        public void Apply(UiTheme theme)
        {
            var source = new Uri("avares://BloogBot.UI.Avalonia/Themes/" + (theme == UiTheme.Light ? "Light" : "Dark") + ".axaml");
            var include = new ResourceInclude(BaseUri) { Source = source };

            var merged = Application.Current.Resources.MergedDictionaries;
            if (merged.Count == 0)
                merged.Add(include);
            else
                merged[0] = include;

            Application.Current.RequestedThemeVariant = theme == UiTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;

            current = theme;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
