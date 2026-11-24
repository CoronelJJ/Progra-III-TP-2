namespace TurnGame.Core
{
    /// <summary>
    /// Estado de UN jugador. Vive en el scope hijo de Player, con su propio IMessageBroker.
    /// Igual que el general: los sistemas piden interfaces estrechas (IFaceStyleSource, etc.).
    /// </summary>
    public interface IPlayerContext
    {
        IPlayer Player { get; }
    }
}
