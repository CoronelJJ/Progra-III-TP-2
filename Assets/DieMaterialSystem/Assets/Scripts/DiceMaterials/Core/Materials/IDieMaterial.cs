namespace DiceMaterials
{
    /// <summary>Datos lógicos de un material. Sin referencias a Unity.</summary>
    public interface IDieMaterial
    {
        /// <summary>Identidad estable (&gt; 0). Se usa para ordenar de forma determinista antes de mezclar.</summary>
        int Id { get; }
        string Name { get; }
        MaterialTier Tier { get; }
        MaterialFamily Family { get; }
        int Score { get; }
        /// <summary>1 = sin modificador.</summary>
        float Multiplier { get; }
        int FinalScore { get; }
    }
}
