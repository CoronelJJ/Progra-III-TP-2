using System;
using System.Globalization;

namespace DiceMaterials
{
    /// <summary>
    /// Orquesta los 3 rollers. Dados (materiales + caras) por lote o de a uno, y la pieza del turno por separado;
    /// la interpretación (jugadas, puntaje, vista) la hace otro sistema.
    /// Los rollers son independientes (seeds derivadas por slot), así que el orden de consumo no altera nada.
    /// </summary>
    public sealed class DiceRollSystem : IDiceRollSystem
    {
        private readonly IDieMaterialRoller materialRoller;
        private readonly IDieValueRoller faceRoller;
        private readonly IPieceRoller pieceRoller;
        private readonly GameSeed seed;
        private readonly ulong fingerprint;

        public GameSeed Seed => seed;
        public ulong Fingerprint => fingerprint;

        public DiceRollSystem(
            IDieMaterialRoller materialRoller,
            IDieValueRoller faceRoller,
            IPieceRoller pieceRoller,
            GameSeed seed,
            ulong contentFingerprint)
        {
            this.materialRoller = materialRoller ?? throw new ArgumentNullException(nameof(materialRoller));
            this.faceRoller = faceRoller ?? throw new ArgumentNullException(nameof(faceRoller));
            this.pieceRoller = pieceRoller ?? throw new ArgumentNullException(nameof(pieceRoller));
            this.seed = seed;
            fingerprint = contentFingerprint;
        }

        public DiceRollResult RollDice(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var materials = materialRoller.GetRolledDieMaterials(count);
            var faces = faceRoller.GetRolledDieValues(count);
            return new DiceRollResult(materials, faces);
        }

        public IPiece DrawPiece() => pieceRoller.GetNextPiece();

        public DiceSystemState CaptureState() => new DiceSystemState
        {
            version = DiceSystemState.CurrentVersion,
            seedText = seed.Text,
            seedValue = seed.Value.ToString(CultureInfo.InvariantCulture),
            fingerprint = fingerprint.ToString(CultureInfo.InvariantCulture),
            materialsDrawn = materialRoller.DrawnCount,
            facesDrawn = faceRoller.DrawnCount,
            piecesDrawn = pieceRoller.DrawnCount,
        };

        public void Restore(DiceSystemState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (state.version != DiceSystemState.CurrentVersion)
                throw new InvalidOperationException($"Versión de estado no soportada: {state.version}.");
            if (state.seedValue != seed.Value.ToString(CultureInfo.InvariantCulture))
                throw new InvalidOperationException("La seed del guardado no coincide con la de esta partida.");
            if (state.fingerprint != fingerprint.ToString(CultureInfo.InvariantCulture))
                throw new InvalidOperationException("El contenido cambió desde el guardado (materiales, proporciones, caras o piezas).");
            if (state.materialsDrawn < 0 || state.facesDrawn < 0 || state.piecesDrawn < 0)
                throw new InvalidOperationException("Contadores inválidos en el estado.");
            if (state.materialsDrawn < materialRoller.DrawnCount ||
                state.facesDrawn < faceRoller.DrawnCount ||
                state.piecesDrawn < pieceRoller.DrawnCount)
                throw new InvalidOperationException("Los rollers ya avanzaron más que el estado guardado: no se puede retroceder.");

            // Todo validado: recién ahora se avanza (no queda un estado a medias).
            while (materialRoller.DrawnCount < state.materialsDrawn) materialRoller.GetNextDieMaterial();
            while (faceRoller.DrawnCount < state.facesDrawn) faceRoller.GetNextDieValue();
            while (pieceRoller.DrawnCount < state.piecesDrawn) pieceRoller.GetNextPiece();
        }
    }
}
