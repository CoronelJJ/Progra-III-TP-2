using System;
using System.Collections.Generic;
using System.Linq;
using BoardGame.Model;
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
        private enum Tool { Playable, Unplayable, Effect, EraseEffect, Inspect }

        private static readonly string[] ToolLabels =
            { "Jugable", "No jugable", "Efecto", "Quitar efecto", "Inspeccionar" };

        private const int MaxSize = 64;
        private static readonly Vector2Int Invalid = new Vector2Int(-1, -1);

        [SerializeField] private LevelBlueprint _blueprint;
        [SerializeField] private Tool _tool = Tool.Playable;
        [SerializeField] private string _selectedEffectType;
        [SerializeField] private float _cellPixelSize = 40f;
        [SerializeField] private int _pendingWidth = LevelBlueprint.DefaultSize;
        [SerializeField] private int _pendingHeight = LevelBlueprint.DefaultSize;
        [SerializeField] private Vector2Int _inspected = new Vector2Int(-1, -1);

        private Vector2 _gridScroll;
        private Vector2 _sidebarScroll;
        private List<Type> _effectTypes = new List<Type>();
        private EffectBrush _brush;
        private SerializedObject _brushSO;
        private SerializedObject _blueprintSO;
        private bool _painting;
        private Vector2Int _lastPainted = new Vector2Int(-1, -1);
        private int _undoGroup;
        private GUIStyle _cellLabelStyle;

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
#if UNITY_6000_3_OR_NEWER
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            var blueprint = EditorUtility.EntityIdToObject(entityId) as LevelBlueprint;
#else
[OnOpenAsset(1)]
private static bool OnOpenAsset(int instanceId, int line)
{
    var blueprint = EditorUtility.EntityIdToObject(instanceId) as LevelBlueprint;
#endif
            if (blueprint == null) return false;
            Open(blueprint);
            return true;
        }

        // ------------------------------------------------------------------ ciclo de vida

        private void OnEnable()
        {
            RefreshEffectTypes();
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
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => CellEffectStyle.GetDisplayName(t))
                .ToList();
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
                    if (cell.playable || cell.effect != null)
                    {
                        RecordChange("Pintar no jugable");
                        cell.playable = false;
                        cell.effect = null; // una celda fuera de la forma no lleva efecto
                        changed = true;
                    }
                    break;

                case Tool.Effect:
                    if (cell.playable && _brush != null && _brush.effect != null)
                    {
                        RecordChange("Pintar efecto");
                        cell.effect = CellData.CloneEffect(_brush.effect);
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
                        : CellEffectStyle.GetColor(cell.effect);
                    EditorGUI.DrawRect(inner, color);

                    if (cell.playable && cell.effect != null && size >= 28f)
                    {
                        string name = CellEffectStyle.GetDisplayName(cell.effect.GetType());
                        _cellLabelStyle.normal.textColor = color.grayscale > 0.5f ? Color.black : Color.white;
                        GUI.Label(inner, name.Length <= 2 ? name : name.Substring(0, 2), _cellLabelStyle);
                    }

                    if (_inspected.x == x && _inspected.y == y)
                        DrawOutline(rect, new Color(0f, 0.9f, 1f, 1f));
                }
            }
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
                if (!playable) cell.effect = null;
                EditorUtility.SetDirty(_blueprint);
                _blueprintSO.Update();
            }

            if (!cell.playable) return;

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
                BulkEdit("Todo no jugable", c => { c.playable = false; c.effect = null; });

            if (GUILayout.Button("Quitar todos los efectos"))
                BulkEdit("Quitar todos los efectos", c => c.effect = null);
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

            if (playable == 0)
                EditorGUILayout.HelpBox("El nivel no tiene ninguna celda jugable.", MessageType.Warning);
        }
    }
}
