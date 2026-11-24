namespace TurnGame.Core
{
    /// <summary>La partida se guardó correctamente.</summary>
    public readonly struct GameSaved
    {
        public string Slot { get; }

        public GameSaved(string slot)
        {
            Slot = slot;
        }
    }
}
