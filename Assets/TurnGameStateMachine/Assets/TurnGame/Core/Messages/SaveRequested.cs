namespace TurnGame.Core
{
    /// <summary>Pedido de guardar la partida en el slot indicado.</summary>
    public readonly struct SaveRequested
    {
        public string Slot { get; }

        public SaveRequested(string slot)
        {
            Slot = slot;
        }
    }
}
