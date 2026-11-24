namespace DiceMaterials.Tests
{
    public sealed class FakePiece : IPiece
    {
        public int Id { get; }
        public string Name { get; }
        public int DiceRequired { get; }
        public FakePiece(int id, string name = null, int diceRequired = 1)
        {
            Id = id; Name = name ?? $"P{id}"; DiceRequired = diceRequired;
        }
    }
}
