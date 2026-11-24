# Cómo correr y usar los tests

Versión de Unity: **6000.6.7f1**. Tests en **EditMode** (NUnit, sin escenas, sin `MonoBehaviour`).
Ensamblado: `TurnGame.Tests.EditMode` (solo Editor, solo compila con `UNITY_INCLUDE_TESTS`).

## 1. Desde el editor

1. *Window ▸ General ▸ Test Runner*.
2. Pestaña **EditMode**.
3. **Run All**, o selecciona un nodo (carpeta, clase o caso) y **Run Selected**.
4. Usa el buscador para filtrar, por ejemplo `Pause`, `GameOver`, `Transition`.
5. Los casos parametrizados aparecen con el tipo en el nombre, p. ej.
   `Event_MovesToExpectedState(RollingElementsState,ElementsRolled,BuildingPieceState)`.
   Si falla uno, el nombre te dice exactamente qué fila de la tabla se rompió.

Si no ves la ventana o la pestaña está vacía: comprueba que el paquete *Test Framework* está instalado y que
no hay errores de compilación en la consola.

## 2. Desde la línea de comandos (CI)

Con el editor cerrado para ese proyecto:

```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" ^
  -runTests -batchmode -projectPath "C:\ruta\a\TuProyecto" ^
  -testPlatform EditMode -testResults "%CD%\results.xml" -logFile -

# macOS
/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity \
  -runTests -batchmode -projectPath "/ruta/a/TuProyecto" \
  -testPlatform EditMode -testResults "$PWD/results.xml" -logFile -

# Linux
~/Unity/Hub/Editor/6000.6.7f1/Editor/Unity \
  -runTests -batchmode -projectPath "/ruta/a/TuProyecto" \
  -testPlatform EditMode -testResults "$PWD/results.xml" -logFile -
```

- Unity termina solo al acabar los tests; el código de salida es distinto de 0 si alguno falla.
- El resultado queda en `results.xml` (formato NUnit 3).
- Filtros útiles: `-testFilter "GameMachinePauseTests"` o `-testFilter "TurnGame.Tests.Game"`.
- Opcional: `-nographics` en servidores sin GPU.

## 3. Qué cubre cada archivo

| Carpeta / archivo | Qué verifica |
|---|---|
| `Core/MessageBrokerTests` | entrega, tipos aislados, orden, desuscripción, (des)suscribir durante un `Publish`, contadores |
| `Core/StateMachineTests` | transición diferida, orden Exit→Enter, peticiones ajenas ignoradas, cadenas síncronas, bucle infinito detectado, `Suspend`/`Resume` sin `Exit`/`Enter`, transición diferida desde el estado suspendido, `ForceChange`, `Stop` |
| `Core/StateBehaviourTests` | estados aislados con `FakeStateChanger`: suscribir antes de publicar, `Exit` desuscribe, `SetNext` obligatorio, `Quitting`, `InGame` sin config, `GameOverScreen` |
| `Main/MainMachineTransitionTests` | **tabla de transiciones** de `MainMachine`, tabla de eventos ignorados, sin fugas al hacer `Stop` desde cada estado |
| `Main/GameSetupTests` | modo → config → `SetupRequested` → `SetupFinished` (también síncrono), cancelación, segunda selección ignorada |
| `Main/InGameTests` | `InGame` dueño de `GameMachine`, volver al menú (pausa / game over), limpieza de sesión, partida nueva |
| `Game/GameMachineTransitionTests` | **tabla de transiciones** de `GameMachine`; producto cartesiano estado × evento ignorado; `PieceMoved` nunca cambia el estado |
| `Game/GameMachineCycleTests` | `CheckingBoard` con 0, 1, 3 y 10 jugadas encadenadas; 25 turnos seguidos; sin fugas por ciclo |
| `Game/GameMachinePauseTests` | pausa/reanudación desde los 9 estados pausables sin `Enter`/`Exit` extra; no pausable en Paused/InGameOptions/GameOverScreen; opciones; eventos durante la pausa; orden dentro de un tick |
| `Game/GameMachineGameOverTests` | `GameOverReached` desde los 11 estados, `Exit` del activo y del suspendido, prioridad frente a pausa y transiciones pendientes, `GameOverScreen` |
| `Game/GameMachineSynchronousResponseTests` | sistemas que responden dentro del mismo `Publish`; turno totalmente automático; bucle síncrono detectado |

Las dos **tablas de transiciones** (`[TestCase]`) son la especificación ejecutable: si cambias el diseño,
cambia primero la fila.

## 4. Cómo se escribe un test

Hay dos *harness* que ya hacen el trabajo repetitivo (broker real + log de eventos publicados):

```csharp
var h = new GameMachineHarness();            // arranca GameMachine en TurnStarting
h.GoTo(typeof(PlacingPieceState));           // lleva una máquina NUEVA hasta ese estado usando solo eventos

h.Send<PauseRequested>();                    // = publicar + un tick (cómo se usa en el juego)
Assert.IsInstanceOf<PausedState>(h.Current);
Assert.IsInstanceOf<PlacingPieceState>(h.Suspended);

Assert.AreEqual(1, h.Broker.CountOf<GamePaused>());          // ¿se publicó el evento?
Assert.AreEqual(0, h.Broker.TotalSubscriberCount);           // ¿quedó algo suscrito? (fugas)
```

- `Send<T>()` publica y hace un tick. `Queue<T>()` publica **sin** tick, para montar escenarios del mismo tick
  (`Queue<A>(); Queue<B>(); Tick();`).
- `GoTo(...)` solo funciona sobre una máquina recién creada (`SetUp` crea una nueva en cada test).
- `Machine.Current` se compara **por tipo**: `Assert.IsInstanceOf<RollingElementsState>(h.Current)`.
- Para detectar que un estado **no** volvió a entrar, cuenta su evento de solicitud
  (`CountOf<RollElementsRequested>()` no debe subir) o compara `TotalSubscriberCount`.
- `MainMachineHarness` es igual para `MainMachine` e incluye un `FakeApplicationQuitter`.
- Un estado suelto se prueba con `FakeStateChanger` (guarda las peticiones en vez de ejecutarlas).
- El tiempo es determinista: `Update(float deltaTime)` recibe el delta; nunca se lee `Time.deltaTime`.

### Añadir un test para un estado nuevo

1. Añade su fila a la tabla `Event_MovesToExpectedState` (y su entrada en `ValidTriggers` si es de `GameMachine`).
2. Si es pausable, añádelo a las tablas de `GameMachinePauseTests` con su evento de solicitud.
3. Añádelo a `GameMachineHarness.FlowStates` / `GoTo` para que entre en los tests de fugas y de eventos fuera de contexto.
4. Si es un estado de `GameMachine`, el test de game over y el de `Stop` sin suscripciones lo cubren al añadirlo a las listas.

## 5. Notas

- `GameMachine` y sus estados son `internal`; el ensamblado de tests accede por `InternalsVisibleTo`
  (`Flow/AssemblyInfo.cs`). Si renombras el asmdef de tests, actualiza ese atributo.
- Los tests usan solo el subconjunto clásico de NUnit (`Assert.AreEqual`, `IsInstanceOf`, `Throws`, `TestCase`,
  `TestCaseSource`), compatible con el NUnit que trae Unity Test Framework.
- Si cambias el nombre del ensamblado raíz o los namespaces, mantén `TurnGame.Core` y `TurnGame.Flow` en las
  `references` de `TurnGame.Tests.EditMode.asmdef`.
