namespace DiceMaterials.Tests
{
    public sealed class FakeMaterial : IDieMaterial
    {
        public int Id { get; }
        public string Name { get; }
        public MaterialTier Tier { get; }
        public MaterialFamily Family { get; }
        public int Score { get; }
        public float Multiplier { get; }
        public int FinalScore => DieMaterialMath.ComputeFinalScore(Score, Multiplier);

        public FakeMaterial(int id, MaterialTier tier, MaterialFamily family = MaterialFamily.None, int score = 1, float multiplier = 1f)
        {
            Id = id; Tier = tier; Family = family; Score = score; Multiplier = multiplier;
            Name = $"{tier}{id}";
        }
    }
}
