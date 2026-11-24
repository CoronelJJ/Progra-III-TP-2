namespace DiceMaterials.Runtime
{
    /// <summary>Mensaje del broker del Player: el estilo visual de las caras cambió.</summary>
    public readonly struct FaceStyleChanged
    {
        public FaceStyle Style { get; }
        public FaceStyleChanged(FaceStyle style) => Style = style;
    }
}
