# Documento de referencia (diseño final)

## Principios

1. **La máquina solo secuencia.** No calcula nada del juego (dados, tablero, jugadas): espera señales y avanza.
2. **Transición directa por llamada, el resto por eventos.** Cada estado conoce solo a su(s) siguiente(s)
   (y la pausa); el resto de la comunicación va por `MessageBroker`.
3. **Suscribirse antes de publicar** en cada `Enter`, y desuscribirse en `Exit`.
4. **Las transiciones se aplican en el `Update` de la máquina**, nunca dentro de `Enter`/handlers.
5. **Hechos en pasado** (`PiecePlaced`) y **solicitudes** `...Requested`.
6. **Los estados dependen de `IState`, `IStateChanger` e `IMessageBroker`**, nunca de la máquina concreta.
7. **`GameMachine` solo es alcanzable desde `InGameState`** (clase `internal` en un ensamblado propio).

## Vocabulario

- **Movimiento** (`Move`): acción del jugador. **Jugada** (`Play`): lo que el tablero detecta en `CheckingBoard`.
- **Suspender**: apartar un estado sin llamar a su `Exit` y restaurarlo sin su `Enter` (pausa).

## MainMachine

| Estado | Al entrar publica | Evento | Destino |
|---|---|---|---|
| `BootingState` | `BootRequested` | `BootCompleted` | MainMenu |
| `MainMenuState` | `MainMenuScreenRequested` | `PlayRequested` | GameSetup |
| | | `OptionsRequested` | MenuOptions |
| | | `QuitRequested` | Quitting |
| `MenuOptionsState` | `OptionsScreenRequested` | `OptionsClosed` | MainMenu |
| `GameSetupState` | `GameModeSelectionRequested` | `GameModeSelected` → publica `SetupRequested(config)` y espera | (mismo estado) |
| | | `SetupFinished` (tras elegir modo) | InGame |
| | | `SetupCancelled` | MainMenu |
| `InGameState` | (arranca `GameMachine`) | `GameFinished` | MainMenu |
| | | `QuitToMenuRequested` (solo si `GameMachine` está en `Paused`) | MainMenu |
| `QuittingState` | llama a `IApplicationQuitter.Quit()` | — | terminal |

`GameSetup` guarda el `GameConfig` (inmutable) en `GameSession`; `InGame` se niega a entrar sin él y lo limpia al salir.
`SetupFinished` puede publicarse de forma síncrona dentro del handler de `SetupRequested`.

## GameMachine (interna)

### Flujo normal

| Estado | Al entrar publica | Evento | Destino |
|---|---|---|---|
| `TurnStartingState` | `TurnStartRequested` | `TurnIntroFinished` | RollingElements |
| `RollingElementsState` | `RollElementsRequested` | `ElementsRolled` | BuildingPiece |
| `BuildingPieceState` | `BuildPieceRequested` | `PieceBuilt` | PlacingPiece |
| `PlacingPieceState` | `PlacePieceRequested` | `PiecePlaced` | UpdatingBoard |
| `UpdatingBoardState` | `UpdateBoardRequested` | `BoardUpdated` | CheckingBoard |
| `CheckingBoardState` | `CheckBoardRequested` | `PlayDetected` | ResolvingPlay |
| | | `NoPlayDetected` | TurnEnding |
| `ResolvingPlayState` | `ResolvePlayRequested` | `PlayResolved` | PlayingFeedback |
| `PlayingFeedbackState` | `PlayFeedbackRequested` | `FeedbackFinished` | CheckingBoard |
| `TurnEndingState` | `TurnEndRequested` | `NextTurnReady` | TurnStarting |
| `GameOverScreenState` | `GameOverScreenRequested` | `GameOverDismissed` | publica `GameFinished` (lo atiende `InGame`) |

Los turnos son infinitos. Las jugadas encadenadas (dados apilados) dan varias vueltas
`CheckingBoard → ResolvingPlay → PlayingFeedback → CheckingBoard`; la máquina solo recibe
`PlayDetected`/`NoPlayDetected` en cada vuelta.

`PlacingPiece` solo espera `PiecePlaced`. El feedback previo (preview) es una capa aparte: los scripts de input/tablero
publican `PieceMoved(x, y, isValid)` las veces que haga falta y `PiecePlaced` solo tras la confirmación
(`(PieceMoved)* → PiecePlaced`). La máquina no escucha `PieceMoved`.
No hay cancelación de selección.

### Transiciones globales (las atiende `GameMachine`, no los estados)

| Origen | Evento | Resultado |
|---|---|---|
| Todo estado de flujo (no Paused / InGameOptions / GameOverScreen) | `PauseRequested` | Suspende el estado (sin `Exit`), entra en `Paused`, publica `GamePaused` |
| `Paused` | `ResumeRequested` | Sale de `Paused`, restaura el suspendido (sin `Enter`), publica `GameResumed` |
| `Paused` | `OptionsRequested` | `InGameOptions` (el suspendido sigue aparcado) |
| `InGameOptions` | `OptionsClosed` | `Paused` (no vuelve al estado original) |
| `Paused` | `QuitToMenuRequested` | lo atiende `InGame` → MainMenu |
| Cualquiera salvo `GameOverScreen` (incluye Paused e InGameOptions) | `GameOverReached` | `Exit` del activo y del suspendido; entra en `GameOverScreen` |

### Reglas de la pausa y del game over

- Solo el estado activo puede cambiar de estado. Si el estado **suspendido** pide una transición (su evento llegó
  durante la pausa), se recuerda y se aplica al reanudar, justo después de `GameResumed`.
- `GameOverReached` **descarta todo lo pendiente del tick** (transiciones, pausas, transiciones diferidas),
  llegue antes o después de ellas. Si ya estás en `GameOverScreen`, se ignora (también dos en el mismo tick).
- `GameOverScreen` no es pausable, ni reacciona a flujo normal, ni a `ResumeRequested`/`OptionsRequested`.
- Un `PauseRequested` y una transición en el mismo tick se ejecutan en el orden en que se publicaron.
- Al hacer `Stop()` (salir de `InGame`) se llama `Exit` del estado activo y del suspendido; no queda nada suscrito.

## Eventos

| Evento | Emisor | Receptor |
|---|---|---|
| `BootRequested`, `MainMenuScreenRequested`, `OptionsScreenRequested`, `GameModeSelectionRequested`, `SetupRequested(config)` | estados | sistemas externos (carga, UI, escena) |
| `BootCompleted`, `PlayRequested`, `OptionsRequested`, `OptionsClosed`, `QuitRequested`, `GameModeSelected(mode)`, `SetupFinished`, `SetupCancelled` | UI / sistemas | estados de `MainMachine` / `Paused` |
| `QuitToMenuRequested` | UI de pausa | `InGameState` |
| `TurnStartRequested`, `RollElementsRequested`, `BuildPieceRequested`, `PlacePieceRequested`, `UpdateBoardRequested`, `CheckBoardRequested`, `ResolvePlayRequested`, `PlayFeedbackRequested`, `TurnEndRequested` | estados del turno | sistemas del juego (turnos, dados, input, tablero, feedback) |
| `TurnIntroFinished`, `ElementsRolled`, `PieceBuilt`, `PiecePlaced`, `BoardUpdated`, `PlayDetected`, `NoPlayDetected`, `PlayResolved`, `FeedbackFinished`, `NextTurnReady` | sistemas del juego | estado del turno correspondiente |
| `PieceMoved(x, y, isValid)` | input + validación | sistema de preview (la máquina **no** escucha) |
| `PauseRequested`, `ResumeRequested`, `GameOverReached` | UI / app / reglas | `GameMachine` |
| `GamePaused`, `GameResumed` | `GameMachine` | input, animación, audio |
| `GameOverScreenRequested` | `GameOverScreenState` | UI |
| `GameOverDismissed` | UI | `GameOverScreenState` |
| `GameFinished` | `GameOverScreenState` | `InGameState` |

`OptionsRequested`/`OptionsClosed` se reutilizan entre menú principal y pausa: es seguro porque solo el estado
activo está suscrito. Si prefieres separarlos, bastaría con `InGameOptionsRequested`/`InGameOptionsClosed`.

## Desviaciones menores respecto a la conversación

- `IStateChanger.ChangeState(IState source, IState target)` (antes `ChangeState(IState)`): hace falta la
  fuente para ignorar peticiones de estados inactivos.
- Evento durante la pausa: se **difiere** hasta reanudar (antes se decía "se ignora"); evita que el estado
  suspendido quede esperando para siempre un evento que ya llegó.
- Se añadieron los eventos `...Requested` que publica cada estado al entrar, que no estaban listados.
- `ChangeState` y las acciones globales se aplican al **principio y al final** de `Update`, de modo que un
  evento publicado entre ticks se procesa en el siguiente `Update`.
