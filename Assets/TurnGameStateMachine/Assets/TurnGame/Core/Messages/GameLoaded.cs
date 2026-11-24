namespace TurnGame.Core
{
    /// <summary>El guardado se aplicó correctamente.</summary>
    public readonly struct GameLoaded
    {
        public string Slot { get; }

        public GameLoaded(string slot)
        {
            Slot = slot;
        }
    }
}
