namespace TurnGame.Core
{
    /// <summary>No se pudo cargar (no existe, está corrupto, es de otra seed/contenido, o la partida ya avanzó).</summary>
    public readonly struct LoadFailed
    {
        public string Slot { get; }
        public string Error { get; }

        public LoadFailed(string slot, string error)
        {
            Slot = slot;
            Error = error;
        }
    }
}
