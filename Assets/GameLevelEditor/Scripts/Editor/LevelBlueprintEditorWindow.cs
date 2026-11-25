using System;
using System.Collections.Generic;
using System.Linq;
using BoardGame.Model;
using DiceMaterials.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BoardGame.Levels.EditorTools
{
    /// <summary>
    /// Ventana para diseñar niveles: tamaño de la grilla, celdas jugables / no jugables,
    /// efectos (autodetectados) y sus parámetros. Edita un LevelBlueprint.
    /// Menú: BoardGame > Level Editor.  Convención visual: la fila de abajo es y = 0.
    /// </summary>
    public class LevelBlueprintEditorWindow : EditorWindow
    {
        private enum Tool { Playable, Unplayable, Effect, EraseEffect, Dice, EraseDice, Inspect }

        private static readonly string[] ToolLabels =
            { "Jugable", "No jugable", "Efecto", "Quitar efecto", "Dados", "Quitar dados", "Inspeccionar" };

        private const int MaxSize = 64;
        private static readonly Vector2Int Invalid = new Vector2Int(-1, -1);
        private static readonly Color ObstacleBackground = new Color(0.33f, 0.43f, 0.48f, 1f);

        [SerializeField] private LevelBlueprint _blueprint;
        [SerializeField] private Tool _tool = Tool.Playable;
        [SerializeField] private string _selectedEffectType;
        [SerializeField] private float _cellPixelSize = 40f;
        [SerializeField] private int _pendingWidth = LevelBlueprint.DefaultSize;
        [SerializeField] private int _pendingHeight = LevelBlueprint.DefaultSize;
        [SerializeField] private Vector2Int _inspected = new Vector2Int(-1, -1);
        [SerializeField] private DieMaterialSO _brushMaterial;
        [SerializeField] private FaceValueSO _brushFace;
        [SerializeField] private int _brushQuantity = 1;

        private Vector2 _gridScroll;
        private Vector2 _sidebarScroll;
        private List<Type> _effectTypes = new List<Type>();
        private List<DieMaterialSO> _editMaterials = new List<DieMaterialSO>();
        private List<FaceValueSO> _faces = new List<FaceValueSO>();
        private EffectBrush _brush;
        private SerializedObject _brushSO;
        private SerializedObject _blueprintSO;
        private bool _painting;
        private Vector2Int _lastPainted = new Vector2Int(-1, -1);
        private int _undoGroup;
        private GUIStyle _cellLabelStyle;
        private GUIStyle _badgeStyle;

        // ------------------------------------------------------------------ apertura

        [MenuItem("BoardGame/Level Editor")]
        private static void OpenFromMenu() => Open(null);

        public static LevelBlueprintEditorWindow Open(LevelBlueprint blueprint)
        {
            var window = GetWindow<LevelBlueprintEditorWindow>("Level Editor");
            window.minSize = new Vector2(640f, 420f);
            if (blueprint != null) window.SetBlueprint(blueprint);
            return window;
        }

        // Doble clic sobre un LevelBlueprint en el Project abre este editor.
        // Se usa Selection (el doble clic selecciona el asset primero) en vez de InstanceIDToObject /
        // EntityIdToObject, que cambian según la versión de Unity.
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            var blueprint = Selection.activeObject as LevelBlueprint;
            if (blueprint == null) return false;
            Open(blueprint);
            return true;
        }

        // ------------------------------------------------------------------ ciclo de vida

        private void OnEnable()
        {
            RefreshEffectTypes();
            RefreshDiceAssets();
            Undo.undoRedoPerformed += OnUndoRedo;

            if (_blueprint == null && Selection.activeObject is LevelBlueprint selected)
                _blueprint = selected;

            if (_blueprint != null) SetBlueprint(_blueprint);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyBrush();
        }

        private void OnUndoRedo()
        {
            if (_blueprint != null)
            {
                _blueprint.EnsureInitialized();
                _blueprintSO = new SerializedObject(_blueprint);
                _pendingWidth = _blueprint.Width;
                _pendingHeight = _blueprint.Height;
            }
            Repaint();
        }

        private void RefreshEffectTypes()
        {
            // TypeCache se actualiza solo en cada recompilación: los efectos nuevos aparecen sin tocar nada.
            _effectTypes = TypeCache.GetTypesDerivedFrom<ICellEffect>()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                            && t != typeof(NullCellEffect)
                            && !typeof(ILockingCellEffect).IsAssignableFrom(t)
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => CellEffectStyle.GetDisplayName(t))
                .ToList();
        }

        private void OnProjectChange()
        {
            RefreshDiceAssets();
            Repaint();
        }

        // Materiales: solo los marcados Usability = Edit. Caras: todos los FaceValueSO del proyecto.
        private void RefreshDiceAssets()
        {
            _editMaterials = AssetDatabase.FindAssets("t:DieMaterialSO")
                .Select(g => AssetDatabase.LoadAssetAtPath<DieMaterialSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.Usability == MaterialUtilization.Edit)
                .OrderBy(m => m.name)
                .ToList();

            _faces = AssetDatabase.FindAssets("t:FaceValueSO")
                .Select(g => AssetDatabase.LoadAssetAtPath<FaceValueSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(f => f != null)
                .OrderBy(f => f.Value)
                .ThenBy(f => f.name)
                .ToList();

            if (_brushMaterial == null || !_editMaterials.Contains(_brushMaterial))
                _brushMaterial = _editMaterials.FirstOrDefault();
            if (_brushFace == null || !_faces.Contains(_brushFace))
                _brushFace = _faces.FirstOrDefault();
        }

        private void SetBlueprint(LevelBlueprint blueprint)
        {
            _blueprint = blueprint;
            _inspected = Invalid;
            _lastPainted = Invalid;

            if (_blueprint != null)
            {
                _blueprint.EnsureInitialized();
                _pendingWidth = _blueprint.Width;
                _pendingHeight = _blueprint.Height;
                _blueprintSO = new SerializedObject(_blueprint);
            }
            else
            {
                _blueprintSO = null;
            }
            Repaint();
        }

        // ------------------------------------------------------------------ pincel

        private Type GetSelectedType() =>
            _effectTypes.FirstOrDefault(t => t.AssemblyQualifiedName == _selectedEffectType);

        private void EnsureBrush()
        {
            Type type = GetSelectedType();
            if (type == null)
            {
                DestroyBrush();
                return;
            }

            if (_brush != null && _brush.effect != null && _brush.effect.GetType() == type) return;

            DestroyBrush();
            _brush = CreateInstance<EffectBrush>();
            _brush.hideFlags = HideFlags.HideAndDontSave;
            _brush.effect = (ICellEffect)Activator.CreateInstance(type);
            _brushSO = new SerializedObject(_brush);
        }

        private void DestroyBrush()
        {
            if (_brush != null) DestroyImmediate(_brush);
            _brush = null;
            _brushSO = null;
        }

        // ------------------------------------------------------------------ GUI principal

        private void OnGUI()
        {
            DrawTopBar();

            if (_blueprint == null)
            {
                EditorGUILayout.HelpBox(
                    "Elige un LevelBlueprint arriba o crea uno nuevo con el botón \"Nuevo\".",
                    MessageType.Info);
                return;
            }

            _blueprint.EnsureInitialized();
            EnsureBlueprintSO();
            _blueprintSO.Update();

            if (_tool == Tool.Effect && GetSelectedType() == null && _effectTypes.Count > 0)
                _selectedEffectType = _effectTypes[0].AssemblyQualifiedName;
            EnsureBrush();

            EditorGUILayout.BeginHorizontal();
            DrawGridArea();
            DrawSidebar();
            EditorGUILayout.EndHorizontal();
        }

        private void EnsureBlueprintSO()
        {
            if (_blueprintSO == null || _blueprintSO.targetObject != _blueprint)
                _blueprintSO = new SerializedObject(_blueprint);
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            var chosen = (LevelBlueprint)EditorGUILayout.ObjectField(
                _blueprint, typeof(LevelBlueprint), false, GUILayout.Width(260f));
            if (EditorGUI.EndChangeCheck()) SetBlueprint(chosen);

            if (GUILayout.Button("Nuevo", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                CreateNewBlueprint();
                GUIUtility.ExitGUI();
            }

            using (new EditorGUI.DisabledScope(_blueprint == null))
            {
                if (GUILayout.Button("Guardar", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    EditorUtility.SetDirty(_blueprint);
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Zoom", EditorStyles.miniLabel);
            _cellPixelSize = GUILayout.HorizontalSlider(_cellPixelSize, 16f, 80f, GUILayout.Width(100f));

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewBlueprint()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Nuevo LevelBlueprint", "NewLevel", "asset", "Elige dónde guardar el blueprint");
            if (string.IsNullOrEmpty(path)) return;

            var blueprint = CreateInstance<LevelBlueprint>();
            blueprint.Initialize(LevelBlueprint.DefaultSize, LevelBlueprint.DefaultSize);
            AssetDatabase.CreateAsset(blueprint, path);
            AssetDatabase.SaveAssets();
            SetBlueprint(blueprint);
        }

        // ------------------------------------------------------------------ grilla

        private void DrawGridArea()
        {
            int w = _blueprint.Width;
            int h = _blueprint.Height;
            float size = _cellPixelSize;

            _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll, GUILayout.ExpandWidth(true));
            Rect area = GUILayoutUtility.GetRect(w * size, h * size,
                GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            HandleGridInput(area, w, h, size);
            if (Event.current.type == EventType.Repaint) DrawCells(area, w, h, size);

            EditorGUILayout.EndScrollView();
        }

        private void HandleGridInput(Rect area, int w, int h, float size)
        {
            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && area.Contains(e.mousePosition))
                    {
                        _painting = true;
                        _lastPainted = Invalid;
                        Undo.IncrementCurrentGroup();
                        Undo.SetCurrentGroupName("Editar nivel");
                        _undoGroup = Undo.GetCurrentGroup();
                        PaintFromMouse(area, w, h, size, e.mousePosition);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_painting)
                    {
                        PaintFromMouse(area, w, h, size, e.mousePosition);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_painting)
                    {
                        _painting = false;
                        Undo.CollapseUndoOperations(_undoGroup);
                        e.Use();
                    }
                    break;
            }
        }

        private void PaintFromMouse(Rect area, int w, int h, float size, Vector2 mouse)
        {
            int x = Mathf.FloorToInt((mouse.x - area.x) / size);
            int row = Mathf.FloorToInt((mouse.y - area.y) / size);
            int y = h - 1 - row; // y = 0 es la fila de abajo

            if (x < 0 || x >= w || y < 0 || y >= h) return;

            var coord = new Vector2Int(x, y);
            if (coord == _lastPainted) return;
            _lastPainted = coord;

            ApplyTool(x, y);
            Repaint();
        }

        private void ApplyTool(int x, int y)
        {
            CellData cell = _blueprint.GetCell(x, y);
            if (cell == null) return;

            bool changed = false;

            switch (_tool)
            {
                case Tool.Playable:
                    if (!cell.playable)
                    {
                        RecordChange("Pintar jugable");
                        cell.playable = true;
                        changed = true;
                    }
                    break;

                case Tool.Unplayable:
                    if (cell.playable || cell.effect != null || cell.HasDice)
                    {
                        RecordChange("Pintar no jugable");
                        cell.playable = false;
                        cell.effect = null; // una celda fuera de la forma no lleva efecto ni dados
                        cell.dice.Clear();
                        changed = true;
                    }
                    break;

                case Tool.Effect:
                    if (cell.playable && _brush != null && _brush.effect != null)
                    {
                        RecordChange("Pintar efecto");
                        cell.effect = CellData.CloneEffect(_brush.effect);
                        cell.dice.Clear(); // sin combo efecto + dados
                        changed = true;
                    }
                    break;

                case Tool.EraseEffect:
                    if (cell.effect != null)
                    {
                        RecordChange("Quitar efecto");
                        cell.effect = null;
                        changed = true;
                    }
                    break;

                case Tool.Dice:
                    if (cell.playable && _brushMaterial != null && _brushFace != null)
                    {
                        RecordChange("Pintar dados");
                        bool append = Event.current != null && Event.current.shift;
                        if (!append) cell.dice.Clear();
                        for (int i = 0; i < Mathf.Max(1, _brushQuantity); i++)
                            cell.dice.Add(new DiceRecipe { material = _brushMaterial, face = _brushFace });
                        cell.effect = null; // sin combo efecto + dados
                        changed = true;
                    }
                    break;

                case Tool.EraseDice:
                    if (cell.HasDice)
                    {
                        RecordChange("Quitar dados");
                        cell.dice.Clear();
                        changed = true;
                    }
                    break;

                case Tool.Inspect:
                    _inspected = new Vector2Int(x, y);
                    GUI.FocusControl(null);
                    break;
            }

            if (changed) EditorUtility.SetDirty(_blueprint);
        }

        private void RecordChange(string label) => Undo.RecordObject(_blueprint, label);

        private void DrawCells(Rect area, int w, int h, float size)
        {
            if (_cellLabelStyle == null)
            {
                _cellLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    CellData cell = _blueprint.GetCell(x, y);
                    var rect = new Rect(area.x + x * size, area.y + (h - 1 - y) * size, size, size);
                    var inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);

                    Color color = !cell.playable
                        ? CellEffectStyle.UnplayableColor
                        : (cell.HasDice ? ObstacleBackground : CellEffectStyle.GetColor(cell.effect));
                    EditorGUI.DrawRect(inner, color);

                    if (cell.playable && cell.effect != null && size >= 28f)
                    {
                        string name = CellEffectStyle.GetDisplayName(cell.effect.GetType());
                        _cellLabelStyle.normal.textColor = color.grayscale > 0.5f ? Color.black : Color.white;
                        GUI.Label(inner, name.Length <= 2 ? name : name.Substring(0, 2), _cellLabelStyle);
                    }

                    DrawDiceInCell(inner, cell);

                    if (_inspected.x == x && _inspected.y == y)
                        DrawOutline(rect, new Color(0f, 0.9f, 1f, 1f));
                }
            }
        }

        // El dado visible desde arriba es el último de la lista; el contador muestra cuántos hay apilados.
        private void DrawDiceInCell(Rect inner, CellData cell)
        {
            if (!cell.playable || !cell.HasDice) return;

            DiceRecipe top = cell.dice[cell.dice.Count - 1];
            if (top == null) return;

            var body = new Rect(inner.x + inner.width * 0.15f, inner.y + inner.height * 0.15f,
                                inner.width * 0.7f, inner.height * 0.7f);
            EditorGUI.DrawRect(body, GetMaterialTint(top.material));

            if (top.face != null)
            {
                Sprite sprite = top.face.GetSprite(top.face.DefaultStyle);
                if (sprite != null) DrawSprite(body, sprite);
            }

            if (cell.dice.Count > 1)
            {
                if (_badgeStyle == null)
                {
                    _badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.LowerRight };
                    _badgeStyle.normal.textColor = Color.white;
                }
                var badge = new Rect(inner.xMax - 26f, inner.yMax - 15f, 26f, 15f);
                EditorGUI.DrawRect(badge, new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(badge, "x" + cell.dice.Count, _badgeStyle);
            }
        }

        private static Color GetMaterialTint(DieMaterialSO material)
        {
            if (material == null) return Color.magenta;
            Material m = material.UnityMaterial;
            if (m == null) return Color.gray;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            if (tex == null) return;
            Rect tr = sprite.textureRect;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }

        private static void DrawOutline(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2f, r.width, 2f), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 2f, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 2f, r.y, 2f, r.height), c);
        }

        // ------------------------------------------------------------------ panel lateral

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300f));
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

            DrawSizeSection();
            EditorGUILayout.Space();
            DrawToolSection();
            EditorGUILayout.Space();

            if (_tool == Tool.Effect) DrawEffectPalette();
            if (_tool == Tool.Dice) DrawDicePalette();
            if (_tool == Tool.Inspect) DrawInspectSection();

            EditorGUILayout.Space();
            DrawBulkSection();
            EditorGUILayout.Space();
            DrawStatsSection();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSizeSection()
        {
            EditorGUILayout.LabelField("Tablero", EditorStyles.boldLabel);

            _pendingWidth = EditorGUILayout.IntField("Ancho", _pendingWidth);
            _pendingHeight = EditorGUILayout.IntField("Alto", _pendingHeight);

            bool differs = _pendingWidth != _blueprint.Width || _pendingHeight != _blueprint.Height;
            using (new EditorGUI.DisabledScope(!differs))
            {
                if (GUILayout.Button("Aplicar tamaño")) ApplyResize();
            }

            EditorGUI.BeginChangeCheck();
            int limit = EditorGUILayout.IntField("Límite de stack base", _blueprint.BaseStackLimit);
            if (EditorGUI.EndChangeCheck())
            {
                RecordChange("Cambiar límite de stack");
                _blueprint.BaseStackLimit = limit;
                EditorUtility.SetDirty(_blueprint);
            }
        }

        private void ApplyResize()
        {
            int newWidth = Mathf.Clamp(_pendingWidth, 1, MaxSize);
            int newHeight = Mathf.Clamp(_pendingHeight, 1, MaxSize);

            bool shrinking = newWidth < _blueprint.Width || newHeight < _blueprint.Height;
            if (shrinking && !EditorUtility.DisplayDialog(
                    "Reducir tablero",
                    "Se perderán las celdas fuera del nuevo tamaño (puedes deshacer con Ctrl+Z). ¿Continuar?",
                    "Continuar", "Cancelar"))
            {
                return;
            }

            RecordChange("Redimensionar nivel");
            _blueprint.Resize(newWidth, newHeight);
            EditorUtility.SetDirty(_blueprint);

            _pendingWidth = _blueprint.Width;
            _pendingHeight = _blueprint.Height;
            if (_inspected.x >= _blueprint.Width || _inspected.y >= _blueprint.Height) _inspected = Invalid;
            _blueprintSO = new SerializedObject(_blueprint);
        }

        private void DrawToolSection()
        {
            EditorGUILayout.LabelField("Herramienta", EditorStyles.boldLabel);
            _tool = (Tool)GUILayout.SelectionGrid((int)_tool, ToolLabels, 2);
            EditorGUILayout.HelpBox("La fila de abajo es y = 0. Arrastra para pintar varias celdas. " +
                                    "Los efectos solo se pintan sobre celdas jugables.", MessageType.None);
        }

        private void DrawEffectPalette()
        {
            EditorGUILayout.LabelField("Efectos disponibles (" + _effectTypes.Count + ")", EditorStyles.boldLabel);

            if (_effectTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No se encontraron efectos. Un efecto debe implementar ICellEffect, no ser abstracto " +
                    "y tener un constructor público sin parámetros.", MessageType.Warning);
                return;
            }

            foreach (Type type in _effectTypes)
            {
                EditorGUILayout.BeginHorizontal();

                Rect swatch = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.Height(22f));
                EditorGUI.DrawRect(new Rect(swatch.x, swatch.y + 3f, 16f, 16f), CellEffectStyle.GetColor(type));

                bool selected = type.AssemblyQualifiedName == _selectedEffectType;
                bool serializable = type.IsDefined(typeof(SerializableAttribute), false);

                string label = CellEffectStyle.GetDisplayName(type) + (serializable ? "" : "  (!)");
                string tooltip = CellEffectStyle.GetDescription(type);
                if (!serializable) tooltip += "\nFalta [Serializable] en esta clase: puede no guardarse en el blueprint.";

                Color previous = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(22f)))
                {
                    _selectedEffectType = type.AssemblyQualifiedName;
                    GUI.FocusControl(null);
                    EnsureBrush();
                }
                GUI.backgroundColor = previous;

                EditorGUILayout.EndHorizontal();
            }

            Type selectedType = GetSelectedType();
            if (_brushSO != null && selectedType != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Parámetros del pincel", EditorStyles.boldLabel);

                _brushSO.Update();
                SerializedProperty effectProp = _brushSO.FindProperty("effect");
                if (effectProp != null && effectProp.hasVisibleChildren)
                    EditorGUILayout.PropertyField(effectProp,
                        new GUIContent(CellEffectStyle.GetDisplayName(selectedType)), true);
                else
                    EditorGUILayout.HelpBox("Este efecto no tiene parámetros.", MessageType.None);
                _brushSO.ApplyModifiedProperties();
            }
        }

        private void DrawDicePalette()
        {
            EditorGUILayout.LabelField("Dados precolocados", EditorStyles.boldLabel);

            if (_editMaterials.Count == 0)
                EditorGUILayout.HelpBox("No hay DieMaterialSO con Usability = Edit.", MessageType.Warning);
            else
            {
                int mi = Mathf.Max(0, _editMaterials.IndexOf(_brushMaterial));
                mi = EditorGUILayout.Popup("Material", mi, _editMaterials.Select(m => m.name).ToArray());
                _brushMaterial = _editMaterials[mi];
            }

            if (_faces.Count == 0)
                EditorGUILayout.HelpBox("No hay FaceValueSO en el proyecto.", MessageType.Warning);
            else
            {
                int fi = Mathf.Max(0, _faces.IndexOf(_brushFace));
                fi = EditorGUILayout.Popup("Cara", fi, _faces.Select(f => f.Value + "  (" + f.name + ")").ToArray());
                _brushFace = _faces[fi];
            }

            _brushQuantity = Mathf.Max(1, EditorGUILayout.IntField("Cantidad", _brushQuantity));
            if (_brushQuantity > _blueprint.BaseStackLimit)
                EditorGUILayout.HelpBox("La cantidad supera el límite de stack base (" + _blueprint.BaseStackLimit + ").",
                    MessageType.Warning);

            EditorGUILayout.HelpBox(
                "Clic: reemplaza los dados de la celda. Shift + clic: agrega encima. " +
                "El primero de la lista es el más cercano al tablero. " +
                "Pintar dados quita el efecto de la celda (no se combinan).", MessageType.None);

            if (GUILayout.Button("Refrescar listas")) RefreshDiceAssets();
        }

        private void DrawInspectDice(CellData cell)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dados (0 = más cerca del tablero)", EditorStyles.boldLabel);

            int removeAt = -1;
            int swapA = -1;
            int swapB = -1;

            for (int i = 0; i < cell.dice.Count; i++)
            {
                DiceRecipe d = cell.dice[i];
                if (d == null) { d = new DiceRecipe(); cell.dice[i] = d; }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Dado " + i, EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▲", GUILayout.Width(24f)) && i < cell.dice.Count - 1) { swapA = i; swapB = i + 1; }
                if (GUILayout.Button("▼", GUILayout.Width(24f)) && i > 0) { swapA = i; swapB = i - 1; }
                if (GUILayout.Button("✕", GUILayout.Width(24f))) removeAt = i;
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var mat = (DieMaterialSO)EditorGUILayout.ObjectField("Material", d.material, typeof(DieMaterialSO), false);
                var face = (FaceValueSO)EditorGUILayout.ObjectField("Cara", d.face, typeof(FaceValueSO), false);
                if (EditorGUI.EndChangeCheck())
                {
                    RecordChange("Editar dado");
                    d.material = mat;
                    d.face = face;
                    EditorUtility.SetDirty(_blueprint);
                }

                if (d.material == null || d.face == null)
                    EditorGUILayout.HelpBox("Dado incompleto: se omitirá al construir el nivel.", MessageType.Warning);
                else if (d.material.Usability != MaterialUsabilityEdit)
                    EditorGUILayout.HelpBox("Este material no está marcado como Edit.", MessageType.Info);

                EditorGUILayout.EndVertical();
            }

            if (removeAt >= 0)
            {
                RecordChange("Quitar dado");
                cell.dice.RemoveAt(removeAt);
                EditorUtility.SetDirty(_blueprint);
            }
            else if (swapA >= 0)
            {
                RecordChange("Reordenar dados");
                DiceRecipe tmp = cell.dice[swapA];
                cell.dice[swapA] = cell.dice[swapB];
                cell.dice[swapB] = tmp;
                EditorUtility.SetDirty(_blueprint);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Agregar dado (pincel)"))
            {
                RecordChange("Agregar dado");
                cell.dice.Add(new DiceRecipe { material = _brushMaterial, face = _brushFace });
                cell.effect = null;
                EditorUtility.SetDirty(_blueprint);
            }
            using (new EditorGUI.DisabledScope(cell.dice.Count == 0))
            {
                if (GUILayout.Button("Quitar todos"))
                {
                    RecordChange("Quitar dados");
                    cell.dice.Clear();
                    EditorUtility.SetDirty(_blueprint);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (cell.dice.Count > _blueprint.BaseStackLimit)
                EditorGUILayout.HelpBox("Hay más dados que el límite de stack base (" + _blueprint.BaseStackLimit + ").",
                    MessageType.Warning);

            var values = new HashSet<int>();
            foreach (DiceRecipe d in cell.dice)
                if (d != null && d.face != null) values.Add(d.face.Value);
            if (values.Count > 1)
                EditorGUILayout.HelpBox("Valores de cara distintos en la misma celda: el editor lo permite, " +
                                        "pero en partida normal no se podrían apilar.", MessageType.Info);
        }

        private const MaterialUtilization MaterialUsabilityEdit = MaterialUtilization.Edit;

        private void DrawInspectSection()
        {
            EditorGUILayout.LabelField("Celda seleccionada", EditorStyles.boldLabel);

            if (!_blueprint.IsInBounds(_inspected.x, _inspected.y))
            {
                EditorGUILayout.HelpBox("Haz clic en una celda de la grilla.", MessageType.Info);
                return;
            }

            CellData cell = _blueprint.GetCell(_inspected.x, _inspected.y);
            EditorGUILayout.LabelField("Coordenada", "(" + _inspected.x + ", " + _inspected.y + ")");

            EditorGUI.BeginChangeCheck();
            bool playable = EditorGUILayout.Toggle("Jugable", cell.playable);
            if (EditorGUI.EndChangeCheck())
            {
                RecordChange("Cambiar jugable");
                cell.playable = playable;
                if (!playable) { cell.effect = null; cell.dice.Clear(); }
                EditorUtility.SetDirty(_blueprint);
                _blueprintSO.Update();
            }

            if (!cell.playable) return;

            DrawInspectDice(cell);
            if (cell.HasDice)
            {
                EditorGUILayout.HelpBox("Una celda con dados no lleva efecto. Quita los dados para asignar uno.",
                    MessageType.None);
                return;
            }

            // Selector de efecto: "Sin efecto" + todos los autodetectados.
            var options = new List<string> { "Sin efecto" };
            options.AddRange(_effectTypes.Select(t => CellEffectStyle.GetDisplayName(t)));

            int current = 0;
            if (cell.effect != null)
            {
                int index = _effectTypes.IndexOf(cell.effect.GetType());
                current = index >= 0 ? index + 1 : 0;
            }

            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUILayout.Popup("Efecto", current, options.ToArray());
            if (EditorGUI.EndChangeCheck() && chosen != current)
            {
                RecordChange("Cambiar efecto");
                cell.effect = chosen == 0 ? null : (ICellEffect)Activator.CreateInstance(_effectTypes[chosen - 1]);
                EditorUtility.SetDirty(_blueprint);
                _blueprintSO.Update();
            }

            if (cell.effect == null) return;

            SerializedProperty effectProp = _blueprintSO.FindProperty("cells")
                .GetArrayElementAtIndex(_blueprint.ToIndex(_inspected.x, _inspected.y))
                .FindPropertyRelative("effect");

            if (effectProp != null && effectProp.hasVisibleChildren)
            {
                EditorGUILayout.PropertyField(effectProp,
                    new GUIContent("Parámetros"), true);
                _blueprintSO.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Este efecto no tiene parámetros.", MessageType.None);
            }
        }

        private void DrawBulkSection()
        {
            EditorGUILayout.LabelField("Acciones rápidas", EditorStyles.boldLabel);

            if (GUILayout.Button("Todo jugable"))
                BulkEdit("Todo jugable", c => c.playable = true);

            if (GUILayout.Button("Todo no jugable"))
                BulkEdit("Todo no jugable", c => { c.playable = false; c.effect = null; c.dice.Clear(); });

            if (GUILayout.Button("Quitar todos los efectos"))
                BulkEdit("Quitar todos los efectos", c => c.effect = null);

            if (GUILayout.Button("Quitar todos los dados"))
                BulkEdit("Quitar todos los dados", c => c.dice.Clear());
        }

        private void BulkEdit(string label, Action<CellData> edit)
        {
            RecordChange(label);
            for (int y = 0; y < _blueprint.Height; y++)
                for (int x = 0; x < _blueprint.Width; x++)
                    edit(_blueprint.GetCell(x, y));
            EditorUtility.SetDirty(_blueprint);
            _blueprintSO = new SerializedObject(_blueprint);
        }

        private void DrawStatsSection()
        {
            int total = _blueprint.Width * _blueprint.Height;
            int playable = _blueprint.CountPlayable();
            EditorGUILayout.LabelField("Celdas jugables", playable + " / " + total);
            EditorGUILayout.LabelField("Dados precolocados",
                _blueprint.CountDice() + " en " + _blueprint.CountCellsWithDice() + " celdas");

            if (playable == 0)
                EditorGUILayout.HelpBox("El nivel no tiene ninguna celda jugable.", MessageType.Warning);
        }
    }
}
