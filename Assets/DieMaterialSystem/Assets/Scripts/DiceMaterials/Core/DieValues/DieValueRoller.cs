using System;
using System.Collections.Generic;

namespace DiceMaterials
{
    /// <summary>
    /// Valores de dado 1..maxFace. Se inyecta al inicio de la partida y no cambia.
    ///
    /// Bag: bolsa con 'copiesPerFace' copias de cada cara (se mezcla y se avanza un cursor;
    ///      al agotarse se vuelve a mezclar, cruzando manos parciales sin problema).
    /// Random: azar uniforme con reposición.
    ///
    /// copiesPerFace = 1 haría que cada mano de N=maxFace dados fuera una permutación
    /// (sin pares ni tríos); por eso es un parámetro explícito y no un valor oculto.
    /// Recibe la seed YA DERIVADA para este roller (ver RollerSeeds).
    /// </summary>
    public sealed class DieValueRoller : IDieValueRoller
    {
        private readonly IPicker<int> picker;

        public int MaxFace { get; }
        public int CopiesPerFace { get; }
        public SelectionMode Mode { get; }
        public ulong Seed { get; }
        /// <summary>Cantidad de valores entregados desde la construcción.</summary>
        public long DrawnCount { get; private set; }

        public DieValueRoller(int maxFace, int copiesPerFace, ulong seed, SelectionMode mode = SelectionMode.Bag)
        {
            if (maxFace < 1) throw new ArgumentOutOfRangeException(nameof(maxFace), "maxFace debe ser >= 1.");
            if (copiesPerFace < 1) throw new ArgumentOutOfRangeException(nameof(copiesPerFace), "copiesPerFace debe ser >= 1.");

            MaxFace = maxFace;
            CopiesPerFace = copiesPerFace;
            Mode = mode;
            Seed = seed;

            if (mode == SelectionMode.Bag)
            {
                var faces = new List<int>(maxFace * copiesPerFace);
                for (int face = 1; face <= maxFace; face++)
                    for (int c = 0; c < copiesPerFace; c++)
                        faces.Add(face);
                picker = new ShuffleBag<int>(faces, seed);
            }
            else
            {
                var faces = new List<int>(maxFace);
                for (int face = 1; face <= maxFace; face++) faces.Add(face);
                picker = new RandomPicker<int>(faces, seed);
            }
        }

        public int GetNextDieValue()
        {
            int value = picker.Next();
            DrawnCount++;
            return value;
        }

        public IReadOnlyList<int> GetRolledDieValues(int quantity)
        {
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var result = new List<int>(quantity);
            for (int i = 0; i < quantity; i++) result.Add(GetNextDieValue());
            return result;
        }
    }
}
