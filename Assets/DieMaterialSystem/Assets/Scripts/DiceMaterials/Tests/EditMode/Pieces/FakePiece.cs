namespace DiceMaterials.Tests
{
    public sealed class FakePiece : IPiece
    {
        public int Id { get; }
        public string Name { get; }
        public FakePiece(int id, string name = null) { Id = id; Name = name ?? $"P{id}"; }
    }
}
