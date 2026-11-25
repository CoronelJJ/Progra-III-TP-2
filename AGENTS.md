# AGENTS.md

Guidance for AI coding agents working in **Progra III-TP 2**.

## Project Overview

- **Engine:** Unity `6000.6.7f1` (URP `17.6.0`, Input System `1.20.1`, uGUI `2.6.0`)
- **DI Container:** VContainer `1.19.0` (`jp.hadashikick.vcontainer`)
- **Language:** C# (pure C# core + thin Unity/MonoBehaviour integration layer)
- **Testing:** Unity EditMode unit tests (NUnit with mocks/fakes)

---

## Repository & Assembly Structure

### 1. `Assets/TurnGameStateMachine/`
Hierarchical state machine for app flow (`MainMachine`) and turn flow (`GameMachine`), decoupled via a typed `MessageBroker`.
- `TurnGame.Core` (`Core/`): `IState`, `IStateChanger`, `StateMachine`, `IMessageBroker`, `MessageBroker`. **Pure C# — no `UnityEngine` references.**
- `TurnGame.Flow` (`Flow/`): `MainMachine`, `GameMachine` (`internal`), turn states, `MainEvents`, `GameEvents`, `GameConfig`, `GameSession`. **Pure C# — no `UnityEngine` references.**
- `TurnGame.Persistence` (`Persistence/`): Save/load state persistence.
- `TurnGame.Unity` (`Unity/`): Thin `MonoBehaviour` layer (`GameFlowRunner`, `UnityApplicationQuitter`).
- `GameContext` (`Runtime/`): Game/Player context runtime integration.
- `TurnGame.Tests.EditMode` & `TurnGame.Persistence.Tests`: EditMode unit tests.

### 2. `Assets/DieMaterialSystem/`
Controlled randomness (`SplitMix64` + `ShuffleBag`) and die material/value/piece rollers.
- `DiceMaterials.Core` (`Core/`): `SplitMix64`, `RollerSlot`, `RollerSeeds`, `ShuffleBag`, `RandomPicker`, `DieMaterialRoller`, `DieValueRoller`, `PieceRoller`, and `Dice` (in namespace `BoardGame.Model`). **Pure C# — no `UnityEngine` references.**
- `DiceMaterials.Runtime` (`Runtime/`): `DieMaterialSO`, `TierProportionTable`, `DieMaterialDatabase`, `DieMaterialRollerFactory`.
- `DiceMaterials.Editor` (`Editor/`): Custom inspectors for `DieMaterialDatabase` and `DieMaterialSO`.
- `DiceMaterials.VContainer` (`Integration/VContainer/`): VContainer bindings.
- `DiceMaterials.Tests.EditMode` & `DiceMaterials.Tests.EditMode.Runtime`: EditMode unit tests.

### 3. `Assets/DiceSystem/`
Die face styles, `FaceValueSO` (1–12), `DieData`/`DieDataBuilder`, `Die` prefab/pool (`DieVisualBuilder`), save/restore (`DieSaveMapper`, `DieRecreator`), and VContainer player-scope registration (`DiceRegistration`).
- `DieStyle.asmdef` (`Core/`): `FaceStyleHolder`, `IFaceStyleSource`, `IFaceStyleController`, `IDieTypeSource`.

### 4. `Assets/GameContexts/`
- `GameContext` (`IGameContext`, `IDieTypeSource`) and `PlayerContext` (`IPlayerContext`, `IFaceStyleSource`, `IFaceStyleController`).
- `ContextRegistration`: `RegisterGameContext` / `RegisterPlayerContext` VContainer extensions.

---

## Key Architectural Rules & Conventions

1. **Keep Core Assemblies Pure C#**
   - `TurnGame.Core`, `TurnGame.Flow`, and `DiceMaterials.Core` must not reference `UnityEngine` or `UnityEditor`.

2. **State Machine & MessageBroker**
   - **States only sequence**: Subscribe to response events in `Enter`, publish `*Requested` events, wait for `*ed` completion events, and **unsubscribe in `Exit`**. Inactive states must never react to events.
   - Use `IStateChanger.ChangeState(source, target)` so the machine ignores transition requests from inactive states.
   - Transitions are queued and applied in `Update` (never inside `Enter` or event handlers).
   - `GameOverReached` overrides any pending tick transitions.

3. **Controlled RNG (`SplitMix64` + `ShuffleBag`)**
   - Derive each roller's seed from a single `ulong` master seed via `RollerSeeds.Derive(masterSeed, RollerSlot.*)`.
   - **Never reorder or renumber** existing values in `RollerSlot`; only append new slots at the end.
   - Interleaved rolls on one roller must not affect the sequence of other rollers.

4. **Dependency Injection (VContainer)**
   - **Scope hierarchy**: Parent scope (`GameContext`, `GameSettingsSO`) → Child scope per player (`IPlayer`, `IMessageBroker`, `PlayerContext`, `DiceSystem`).
   - **Narrow interfaces**: Always inject narrow interfaces (`IDieTypeSource`, `IFaceStyleSource`, `IDieMaterialProvider`, `IMessageBroker`) rather than `GameContext` or `PlayerContext`.

---

## Running Tests

All automated tests are **EditMode** tests:
- **In Unity Editor**: `Window > General > Test Runner > EditMode > Run All`
- **Test Assemblies**:
  - `TurnGame.Tests.EditMode`
  - `TurnGame.Persistence.Tests`
  - `DiceMaterials.Tests.EditMode`
  - `DiceMaterials.Tests.EditMode.Runtime`
