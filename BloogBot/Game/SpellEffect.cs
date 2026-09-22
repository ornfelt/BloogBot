// Ported from BloogBot/BloogBot/Game/SpellEffect.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using BloogBot.Game.Enums;

namespace BloogBot.Game
{
    public class SpellEffect
    {
        public SpellEffect(string icon, int stackCount, EffectType type)
        {
            Icon = icon;
            StackCount = stackCount;
            Type = type;
        }

        public string Icon { get; }

        public int StackCount { get; }

        public EffectType Type { get; }
    }
}
