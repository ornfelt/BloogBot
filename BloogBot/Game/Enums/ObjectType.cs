/// <summary>
/// This namespace contains the enumeration for different types of game objects.
/// </summary>
namespace BloogBot.Game.Enums
{
    /// <summary>
    /// Represents the different types of objects in the game.
    /// </summary>
    public enum ObjectType : byte
    {
        Item = 1,
        Container = 2,
        Unit = 3,
        Player = 4,
        GameObject = 5,
        DynamicObject = 6,
        Corpse = 7
    }
}
