# AGENTS.md

Guía para agentes de IA que trabajen en **Progra III-TP 2**.

## 1. Qué es el proyecto y versión de Unity

- **Proyecto:** Juego por turnos de dados y tablero (Progra III - TP 2).
- **Versión de Unity:** `6000.6.7f1`
- **Paquetes clave:** URP `17.6.0`, Input System `1.20.1`, uGUI `2.6.0`, VContainer `1.19.0` (`jp.hadashikick.vcontainer`).
- **Arquitectura:** Núcleo en C# puro (sin `UnityEngine`) + capa fina de integración en Unity (`MonoBehaviour`, `ScriptableObject`, VContainer).

---

## 2. Mapa del código

| Carpeta | Contenido | Ensamblados (`.asmdef`) |
|---|---|---|
| `Assets/TurnGameStateMachine/` | Máquina de estados jerárquica (`MainMachine` + `GameMachine` `internal`) y `MessageBroker` tipado. Incluye `docs/DESIGN.md` y `docs/TESTING.md`. | `TurnGame.Core`, `TurnGame.Flow`, `TurnGame.Persistence`, `TurnGame.Unity`, `GameContext`, `TurnGame.Tests.EditMode`, `TurnGame.Persistence.Tests` |
| `Assets/DieMaterialSystem/` | Aleatoriedad controlada (`SplitMix64` + `ShuffleBag`), `RollerSeeds`/`RollerSlot`, rollers de materiales/valores/fichas y `Dice` (`BoardGame.Model`). | `DiceMaterials.Core`, `DiceMaterials.Runtime`, `DiceMaterials.Editor`, `DiceMaterials.VContainer`, `DiceMaterials.Tests.EditMode`, `DiceMaterials.Tests.EditMode.Runtime` |
| `Assets/DiceSystem/` | Estilos de cara (`FaceStyleHolder`), `FaceValueSO` (1–12), `DieData`/`DieDataBuilder`, prefab/pool (`DieVisualBuilder`), guardado/recreación (`DieSaveMapper`, `DieRecreator`) e installers VContainer. | `DieStyle` |
| `Assets/GameContexts/` | Contexto global (`GameContext`) y por jugador (`PlayerContext`), más `ContextRegistration` para VContainer. | — |
| `Assets/GameLevelEditor/` | Tablero (`Board`, `Cell`), efectos de celda (`Effects/`), `LevelBlueprint`/`LevelBuilder`, vistas (`BoardView`, `CellView`) y herramientas de editor (`Editor/`). | — |
| `Assets/DataSOs/`, `Level Blueprints/`, `Prefabs/`, `Scenes/`, `Settings/`, `Sprites/` | Assets de Unity (ScriptableObjects, escenas, prefabs, materiales, sprites). | — |

---

## 3. Cómo abrir el proyecto y correr los tests

### Abrir el proyecto
1. Abrir **Unity Hub**.
2. Seleccionar la carpeta `Progra III-TP 2` con **Unity 6000.6.7f1**.

### Correr los tests (EditMode)
Todos los tests son unitarios de **EditMode** (NUnit, sin escenas ni `MonoBehaviour`):

- **Desde el Editor:** `Window > General > Test Runner` → pestaña **EditMode** → **Run All** (o **Run Selected**).
- **Desde CLI (con el Editor cerrado):**
  ```bat
  "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" ^
    -runTests -batchmode -projectPath "F:\facu 2\Materias 2026\2do cuatri\Progra 3\TP2\Progra III-TP 2" ^
    -testPlatform EditMode -testResults "%CD%\results.xml" -logFile -
  ```
- **Ensamblados de tests:**
  - `TurnGame.Tests.EditMode`
  - `TurnGame.Persistence.Tests`
  - `DiceMaterials.Tests.EditMode`
  - `DiceMaterials.Tests.EditMode.Runtime`

---

## 4. Reglas del proyecto

### Dónde viven las validaciones
- Las validaciones de flujo, transiciones, pausa, game over, persistencia y RNG determinista viven en los ensamblados `Tests/EditMode` (NUnit con mocks/fakes y harnesses como `GameMachineHarness` y `MainMachineHarness`).
- En `TurnGame.Tests.EditMode`, las tablas de transiciones (`[TestCase]`) son la especificación ejecutable: si cambia una transición, se actualiza primero la tabla.

### Código de Editor vs. Runtime vs. Core puro
- **Core puro (sin `UnityEngine` ni `UnityEditor`):** `TurnGame.Core`, `TurnGame.Flow` y `DiceMaterials.Core`.
- **Runtime (Unity):** `TurnGame.Unity`, `TurnGame.Persistence`, `GameContext`, `DiceMaterials.Runtime`, `DiceMaterials.VContainer`, `DiceSystem`, `GameContexts` y `GameLevelEditor/Scripts/{Model,Levels,View}`.
- **Solo Editor:** `DiceMaterials.Editor` y `GameLevelEditor/Scripts/Editor/` (inspectores y `LevelBlueprintEditorWindow`).
- **Solo Tests (`UNITY_INCLUDE_TESTS`):** carpetas `Tests/EditMode` y `Persistence.Tests/EditMode`.

### Convenciones
- **Máquina de estados:** Los estados solo secuencian. Suscriben en `Enter`, publican `*Requested`, esperan `*ed` y desuscriben en `Exit`. Usan `IStateChanger.ChangeState(source, target)`. Las transiciones se aplican en `Update` (nunca dentro de `Enter`/handlers); `GameOverReached` descarta transiciones pendientes del tick.
- **Inyección de dependencias (VContainer):** Scope padre (`GameContext`, `GameSettingsSO`) → Scope hijo por jugador (`IPlayer`, `IMessageBroker`, `PlayerContext`, `DiceSystem`). Inyectar siempre **interfaces estrechas** (`IDieTypeSource`, `IFaceStyleSource`, `IDieMaterialProvider`, `IMessageBroker`), nunca el contexto completo.
- **Aleatoriedad controlada:** Derivar subseeds desde una seed maestra `ulong` con `RollerSeeds.Derive(masterSeed, RollerSlot.*)`.

### Qué NO debe tocar el agente
- **No agregar referencias a `UnityEngine` o `UnityEditor`** en `TurnGame.Core`, `TurnGame.Flow` ni `DiceMaterials.Core`.
- **No reordenar ni renumerar** los valores existentes del enum `RollerSlot` (solo agregar nuevos slots al final).
- **No romper `InternalsVisibleTo`** en `TurnGame.Flow/AssemblyInfo.cs` (expone `GameMachine` y sus estados a `TurnGame.Tests.EditMode`).
- **No editar manualmente** `Library/`, `Temp/`, `Logs/`, `UserSettings/` ni archivos `.meta` (dejar que Unity los gestione).
