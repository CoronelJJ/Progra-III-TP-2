namespace TurnGame.Core
{
    /// <summary>Identidad de un jugador. Por ahora solo id y nombre.</summary>
    public interface IPlayer
    {
        int Id { get; }
        string Name { get; }
    }
}
