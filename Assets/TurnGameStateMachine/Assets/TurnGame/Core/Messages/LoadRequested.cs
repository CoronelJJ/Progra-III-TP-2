namespace TurnGame.Core
{
    /// <summary>Pedido de cargar el slot indicado (solo válido al comenzar la partida, antes de la primera tirada).</summary>
    public readonly struct LoadRequested
    {
        public string Slot { get; }

        public LoadRequested(string slot)
        {
            Slot = slot;
        }
    }
}
