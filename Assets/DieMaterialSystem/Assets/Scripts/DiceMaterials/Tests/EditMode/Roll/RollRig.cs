using System.Collections.Generic;
using System.Linq;

namespace DiceMaterials.Tests
{
    /// <summary>Arma un DiceRollSystem completo desde una seed maestra, con contenido de prueba (2S 3A 4B 5C, 7 piezas).</summary>
    internal static class RollRig
    {
        public static List<IDieMaterial> Materials()
        {
            var list = new List<IDieMaterial>();
            int id = 1;
            foreach (var (tier, n) in new[] { (MaterialTier.S, 2), (MaterialTier.A, 3), (MaterialTier.B, 4), (MaterialTier.C, 5) })
                for (int i = 0; i < n; i++) list.Add(new FakeMaterial(id++, tier));
            return list;
        }

        public static TierRule[] Rules() => new[]
        {
            new TierRule(MaterialTier.S, 1), new TierRule(MaterialTier.A, 4),
            new TierRule(MaterialTier.B, 10), new TierRule(MaterialTier.C, 25),
        };

        public static List<IPiece> Pieces(int count = 7)
            => Enumerable.Range(1, count).Select(i => (IPiece)new FakePiece(i, null, i % 4 + 1)).ToList();

        public static DiceRollSystem Build(ulong master, int faceCount = 6, int copies = 4, int pieceCount = 7)
        {
            var seed = new GameSeed(master, master.ToString(), false);
            var mats = Materials();
            var rules = Rules();
            var pieces = Pieces(pieceCount);

            var m = new DieMaterialRoller(mats, rules, RollerSeeds.Derive(master, RollerSlot.Material));
            var v = new DieValueRoller(faceCount, copies, RollerSeeds.Derive(master, RollerSlot.DieValue));
            var p = new PieceRoller(pieces, RollerSeeds.Derive(master, RollerSlot.Piece));
            ulong fp = ContentFingerprint.Compute(mats, rules, faceCount, copies, SelectionMode.Bag, pieces);
            return new DiceRollSystem(m, v, p, seed, fp);
        }

        public static string Signature(DiceRollResult r)
            => string.Join(",", r.Materials.Select(m => m.Id)) + "|" + string.Join(",", r.Faces);

        /// <summary>Un turno: la pieza (1) y los dados (count).</summary>
        public static string Turn(IDiceRollSystem s, int dice)
        {
            var piece = s.DrawPiece();
            return Signature(s.RollDice(dice)) + "|" + piece.Id;
        }
    }
}
