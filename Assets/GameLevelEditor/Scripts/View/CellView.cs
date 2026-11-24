using BoardGame.Levels;
using BoardGame.Model;
using UnityEngine;

namespace BoardGame.View
{
    /// <summary>
    /// Representación visual de UNA celda activa. Es solo la "cara" de la Cell del modelo:
    /// no contiene lógica de juego. Va en el prefab de celda (Cube achatado o Quad) junto a un Renderer.
    ///
    /// El color se aplica con un MaterialPropertyBlock: todas las celdas comparten UN solo material
    /// y cada una solo cambia su tinte. Escribe tanto _BaseColor (URP/HDRP) como _Color (Built-in);
    /// el que el shader no tenga simplemente se ignora.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer targetRenderer;

        private MaterialPropertyBlock _block;

        public Cell Cell { get; private set; }

        private void Reset()
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void Bind(Cell cell)
        {
            Cell = cell;
            name = "Cell (" + cell.Coordinate.x + "," + cell.Coordinate.y + ")";
            Refresh();
        }

        /// <summary>Vuelve a pintar la celda según su efecto actual (llamar si el efecto cambia en juego).</summary>
        public void Refresh()
        {
            if (Cell == null) return;
            SetColor(CellEffectStyle.GetColor(Cell.Effect));
        }

        public void SetColor(Color color)
        {
            if (targetRenderer == null) return;

            if (_block == null) _block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_block);
        }
    }
}
