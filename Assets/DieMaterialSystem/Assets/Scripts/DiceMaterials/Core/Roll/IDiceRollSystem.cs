namespace DiceMaterials
{
    public interface IDiceRollSystem
    {
        /// <summary>
        /// Tira <paramref name="count"/> dados (materiales + caras). Sirve para la tirada del turno y para pedir dados extra
        /// (RollDice(1)); pedir de a uno o en lote da exactamente la misma secuencia.
        /// </summary>
        DiceRollResult RollDice(int count);

        /// <summary>Saca la pieza del turno (1 por turno, obligatoria). Se consume al sacarla.</summary>
        IPiece DrawPiece();

        DiceSystemState CaptureState();

        /// <summary>Avanza los rollers hasta el estado guardado. Debe llamarse con rollers recién creados con la misma seed y contenido.</summary>
        void Restore(DiceSystemState state);
    }
}
