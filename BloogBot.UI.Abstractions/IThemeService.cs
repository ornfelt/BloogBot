// Added by the .NET 9 port - no counterpart in BloogBot. Replica rules do not apply to it.
using System;

namespace BloogBot.UI.Abstractions
{
    // Implemented once per shell: WPF swaps Application.Current.Resources.MergedDictionaries[0],
    // Avalonia sets RequestedThemeVariant and swaps the matching ResourceInclude. MainViewModel
    // only ever sees this interface, which is what keeps BloogBot.UI.Core free of shell types.
    public interface IThemeService
    {
        UiTheme Current { get; }

        void Apply(UiTheme theme);

        event EventHandler ThemeChanged;
    }
}
