using System;
using TurnGame.Core;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Vista de un DieData. No decide valores ni tiradas: refleja su dato.
    /// Sin [Inject]: quien lo crea (el pool) le pasa las dependencias en Init.
    /// Init/Release son reentrantes (pool).
    /// </summary>
    public sealed class Die : MonoBehaviour
    {
        [Header("Render")]
        [SerializeField] private MeshRenderer bodyRenderer;
        [Tooltip("Planos de cara (SpriteRenderer) ya colocados y rotados en el prefab.")]
        [SerializeField] private SpriteRenderer[] facePlanes = Array.Empty<SpriteRenderer>();

        [Header("Modo de visualización")]
        [Tooltip("true = todas las caras muestran el sprite. false = solo el plano visible.")]
        [SerializeField] private bool showAllFaces;
        [Tooltip("Plano que se ve cuando showAllFaces es false (puede cambiarse en runtime con ShowPlane).")]
        [SerializeField, Min(0)] private int visiblePlaneIndex;

        private DieData data;
        private IMessageBroker broker;
        private FaceStyle currentStyle;
        private bool showAll;
        private int visiblePlane;
        private Action<FaceStyleChanged> onStyleChanged;

        public bool IsInitialized => data != null;

        /// <summary>Lectura del dato lógico. Para mutar, usar SetFace / SetMaterial de esta clase.</summary>
        public IDieData Data => data;

        /// <summary>
        /// Enlaza el dado con su dato. Puede llamarse varias veces (reciclado): suelta lo anterior primero.
        /// showAllOverride sobrescribe el bool del prefab si no es null.
        /// </summary>
        public void Init(DieData dieData, IMessageBroker messageBroker, IFaceStyleSource styleSource,
                         bool? showAllOverride = null)
        {
            if (dieData == null) throw new ArgumentNullException(nameof(dieData));
            if (messageBroker == null) throw new ArgumentNullException(nameof(messageBroker));
            if (styleSource == null) throw new ArgumentNullException(nameof(styleSource));

            if (IsInitialized) Release();

            data = dieData;
            broker = messageBroker;
            showAll = showAllOverride ?? showAllFaces;
            visiblePlane = visiblePlaneIndex;

            // Estilo vigente: el broker no guarda estado, así que se consulta aquí.
            currentStyle = styleSource.Current;

            onStyleChanged ??= OnStyleChanged;
            broker.Subscribe(onStyleChanged);

            ApplyMaterialVisual();
            RefreshFaces();
        }

        /// <summary>Suelta dato y suscripciones. Llamado por el pool al devolver el dado.</summary>
        public void Release()
        {
            if (broker != null && onStyleChanged != null)
                broker.Unsubscribe(onStyleChanged);

            broker = null;
            data = null;

            for (int i = 0; i < facePlanes.Length; i++)
                if (facePlanes[i] != null) facePlanes[i].sprite = null;

            if (bodyRenderer != null) bodyRenderer.sharedMaterial = null;
        }

        /// <summary>Cambio individual de cara: actualiza el dato y luego la vista.</summary>
        public void SetFace(FaceValueSO newFace)
        {
            EnsureInitialized();
            if (!data.SetFace(newFace)) return;
            RefreshFaces();
        }

        /// <summary>Cambio individual de material: actualiza el dato y luego la vista.</summary>
        public void SetMaterial(DieMaterialSO newMaterial)
        {
            EnsureInitialized();
            if (!data.SetMaterial(newMaterial)) return;
            ApplyMaterialVisual();
        }

        /// <summary>Cambia entre una cara visible y todas, en runtime.</summary>
        public void SetShowAllFaces(bool all)
        {
            if (showAll == all) return;
            showAll = all;
            if (IsInitialized) RefreshFaces();
        }

        /// <summary>Elige qué plano se ve en modo Single (lo decide el sistema de orientación/cámara).</summary>
        public void ShowPlane(int planeIndex)
        {
            if (planeIndex < 0 || planeIndex >= facePlanes.Length)
                throw new ArgumentOutOfRangeException(nameof(planeIndex));
            if (visiblePlane == planeIndex) return;
            visiblePlane = planeIndex;
            if (IsInitialized) RefreshFaces();
        }

        private void OnStyleChanged(FaceStyleChanged msg)
        {
            if (!IsInitialized || msg.Style == currentStyle) return;
            currentStyle = msg.Style;
            RefreshFaces();
        }

        private void ApplyMaterialVisual()
        {
            if (bodyRenderer == null) return;
            var mat = data.MaterialVisual.UnityMaterial;
            if (mat == null)
                Debug.LogWarning($"[{name}] El material '{data.Material.Name}' no tiene Material de Unity.", this);
            bodyRenderer.sharedMaterial = mat; // sharedMaterial: no instancia materiales
        }

        private void RefreshFaces()
        {
            var sprite = data.GetFaceSprite(currentStyle);
            for (int i = 0; i < facePlanes.Length; i++)
            {
                var plane = facePlanes[i];
                if (plane == null) continue;

                bool visible = showAll || i == visiblePlane;
                plane.gameObject.SetActive(visible);
                plane.sprite = visible ? sprite : null;
            }
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException($"[{name}] Die sin inicializar: llama a Init primero.");
        }

        private void OnDestroy()
        {
            if (broker != null && onStyleChanged != null)
                broker.Unsubscribe(onStyleChanged);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (facePlanes == null || facePlanes.Length == 0)
                Debug.LogWarning($"[{name}] Die sin planos de cara asignados.", this);
            else if (visiblePlaneIndex >= facePlanes.Length)
                visiblePlaneIndex = facePlanes.Length - 1;
        }
#endif
    }
}
