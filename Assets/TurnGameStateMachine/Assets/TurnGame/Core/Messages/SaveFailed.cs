namespace TurnGame.Core
{
    /// <summary>No se pudo guardar.</summary>
    public readonly struct SaveFailed
    {
        public string Slot { get; }
        public string Error { get; }

        public SaveFailed(string slot, string error)
        {
            Slot = slot;
            Error = error;
        }
    }
}
