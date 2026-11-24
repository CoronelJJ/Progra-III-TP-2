using System;

namespace BoardGame.Model
{
    /// <summary>
    /// Implementación base con comportamiento "neutro" para cada hook.
    /// Los efectos concretos heredan de acá y solo sobreescriben lo que necesitan,
    /// evitando tener que rellenar métodos que no usan.
    ///
    /// [Serializable] es necesario para que Unity pueda guardar los efectos dentro
    /// del LevelBlueprint (vía [SerializeReference]). OJO: este atributo NO se hereda,
    /// así que cada efecto concreto debe llevar el suyo.
    /// </summary>
    [Serializable]
    public abstract class CellEffectBase : ICellEffect
    {
        public virtual void OnPieceEnter(Cell cell, IBoardPiece piece) { }

        public virtual void OnPieceExit(Cell cell, IBoardPiece piece) { }

        public virtual int ModifyValue(int baseValue) => baseValue;

        public virtual bool CanAcceptPiece(Cell cell, IBoardPiece incoming) => true;

        public virtual bool CanAcceptModifier(Cell cell) => true;

        public virtual void OnBombHit(Cell cell, object bombInfo) { }

        public virtual int ModifyStackLimit(int baseLimit) => baseLimit;
    }
}
