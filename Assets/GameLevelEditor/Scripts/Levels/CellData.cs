using System;
using System.Collections.Generic;
using BoardGame.Model;
using UnityEngine;

namespace BoardGame.Levels
{
    /// <summary>
    /// Datos puros de una celda dentro del blueprint (no es la Cell del modelo).
    /// - playable: si la celda forma parte de la forma del nivel.
    /// - effect: efecto de la celda, o null si no tiene ninguno.
    /// - dice: dados precolocados (obstáculos). El orden es el de la cola: el índice 0 es el más
    ///   cercano al tablero y los siguientes van encima.
    ///
    /// Una celda lleva EFECTO o DADOS, no ambos. Si por edición manual tiene los dos,
    /// el LevelBuilder usa los dados e ignora el efecto.
    /// </summary>
    [Serializable]
    public class CellData
    {
        public bool playable = true;

        // [SerializeReference] permite guardar cualquier clase que implemente ICellEffect,
        // con sus propios campos, de forma polimórfica.
        [SerializeReference] public ICellEffect effect;

        public List<DiceRecipe> dice = new List<DiceRecipe>();

        public bool HasDice => dice != null && dice.Count > 0;

        /// <summary>
        /// Crea una copia independiente de un efecto (mismo tipo y mismos valores serializados).
        /// Se usa al construir el nivel para que cada Cell tenga su propia instancia y
        /// el estado interno de un efecto en partida nunca modifique el blueprint.
        /// </summary>
        public static ICellEffect CloneEffect(ICellEffect source)
        {
            if (source == null) return null;
            Type type = source.GetType();
            string json = JsonUtility.ToJson(source);
            return (ICellEffect)JsonUtility.FromJson(json, type);
        }
    }
}
