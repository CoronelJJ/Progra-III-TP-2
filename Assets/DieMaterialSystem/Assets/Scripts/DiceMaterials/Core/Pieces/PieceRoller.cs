using System;
using System.Collections.Generic;

namespace DiceMaterials
{
    /// <summary>
    /// Bolsa estilo Tetris: cada pieza sale 1 vez por ciclo; al agotarse se vuelve a mezclar.
    /// Sin guard de borde: la última de un ciclo puede coincidir con la primera del siguiente.
    /// Antes de mezclar, las piezas se ordenan por Id (determinismo ante cualquier orden de entrada).
    /// Recibe la seed YA DERIVADA para este roller (ver RollerSeeds).
    /// </summary>
    public sealed class PieceRoller : IPieceRoller
    {
        private readonly ShuffleBag<IPiece> bag;

        public ulong Seed { get; }
        /// <summary>Cantidad de piezas entregadas desde la construcción.</summary>
        public long DrawnCount { get; private set; }
        public int PieceCount => bag.Size;

        public PieceRoller(IEnumerable<IPiece> pieces, ulong seed)
        {
            if (pieces == null) throw new ArgumentNullException(nameof(pieces));
            Seed = seed;

            var sorted = new List<IPiece>();
            foreach (var p in pieces)
            {
                if (p == null) throw new ArgumentException("La lista de piezas contiene un null.", nameof(pieces));
                sorted.Add(p);
            }
            if (sorted.Count == 0) throw new ArgumentException("Se necesita al menos una pieza.", nameof(pieces));

            sorted.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 1; i < sorted.Count; i++)
                if (sorted[i].Id == sorted[i - 1].Id)
                    throw new ArgumentException(
                        $"Id de pieza duplicado {sorted[i].Id}: '{sorted[i - 1].Name}' y '{sorted[i].Name}'. Rompe el determinismo.",
                        nameof(pieces));

            bag = new ShuffleBag<IPiece>(sorted, seed);
        }

        public IPiece GetNextPiece()
        {
            var piece = bag.Next();
            DrawnCount++;
            return piece;
        }

        public IReadOnlyList<IPiece> GetRolledPieces(int quantity)
        {
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var result = new List<IPiece>(quantity);
            for (int i = 0; i < quantity; i++) result.Add(GetNextPiece());
            return result;
        }
    }
}
