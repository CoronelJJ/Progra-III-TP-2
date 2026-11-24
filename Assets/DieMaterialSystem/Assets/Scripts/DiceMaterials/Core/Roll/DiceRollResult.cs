using System;
using System.Collections.Generic;

namespace DiceMaterials
{
    /// <summary>
    /// Resultado de una tirada de dados: el índice i de Materials y de Faces es el mismo dado.
    /// Se puede pedir de a 1 o en lotes. La pieza NO viene acá: es 1 por turno (DrawPiece).
    /// </summary>
    public sealed class DiceRollResult
    {
        public IReadOnlyList<IDieMaterial> Materials { get; }
        /// <summary>Valores de cara (1..FaceCount).</summary>
        public IReadOnlyList<int> Faces { get; }

        public DiceRollResult(IReadOnlyList<IDieMaterial> materials, IReadOnlyList<int> faces)
        {
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
            Faces = faces ?? throw new ArgumentNullException(nameof(faces));
            if (materials.Count != faces.Count)
                throw new ArgumentException("Materials y Faces deben tener la misma cantidad.");
        }
    }
}
