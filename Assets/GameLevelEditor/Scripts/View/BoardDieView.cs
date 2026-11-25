using DiceMaterials.Runtime;
using UnityEngine;

/// <summary>
/// Vista mínima de un dado precolocado. No usa pool ni broker, y no cambia de cara en juego:
/// solo refleja el DieData que lee el tablero. Es independiente del Die real.
///
/// Prefab sugerido: raíz con este script + un Cube (MeshRenderer) + un hijo "Face" con SpriteRenderer
/// apoyado sobre la cara de arriba (rotación X = 90).
/// </summary>
public class BoardDieView : MonoBehaviour
{
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer facePlane;

    public DieData Data { get; private set; }

    private void Reset()
    {
        bodyRenderer = GetComponentInChildren<MeshRenderer>();
        facePlane = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(DieData data)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));

        Data = data;
        name = "Die (" + data.Value + ")";

        if (bodyRenderer != null)
        {
            Material mat = data.MaterialVisual.UnityMaterial;
            if (mat != null) bodyRenderer.sharedMaterial = mat; // sharedMaterial: no instancia materiales
            else Debug.LogWarning("[" + name + "] El material '" + data.Material.Name + "' no tiene Material de Unity.", this);
        }

        if (facePlane != null)
            facePlane.sprite = data.GetFaceSprite(data.Face.DefaultStyle);
    }
}
