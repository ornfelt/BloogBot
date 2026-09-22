// Added by the .NET 9 port - no counterpart in BloogBot. Replica rules do not apply to it.
namespace BloogBot.UI.Abstractions
{
    // The two themes both shells ship. Dark is the first member and the default, so a
    // default(UiTheme) and a missing "Theme" key in botSettings.json both mean Dark.
    public enum UiTheme
    {
        Dark,
        Light,
    }
}
