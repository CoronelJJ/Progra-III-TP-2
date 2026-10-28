# TurnGame – Máquinas de estado jerárquicas (Unity 6000.6.7f1)

Base de la máquina de estados del juego por turnos: `MainMachine` (flujo de la app) y la submáquina
`GameMachine` (turnos), comunicadas por un `MessageBroker` tipado. Núcleo en C# puro, sin
`MonoBehaviour`, con tests unitarios EditMode.

## Estructura

```
Assets/TurnGame/
├─ Core/                       TurnGame.Core       (sin referencias a UnityEngine)
│  ├─ IState.cs                Enter / Update(dt) / Exit
│  ├─ IStateChanger.cs         lo único que un estado necesita para pedir una transición
│  ├─ StateMachine.cs          cola de transiciones, suspensión (pausa), override (game over)
│  └─ Messaging/               IMessageBroker + MessageBroker (eventos tipados)
├─ Flow/                       TurnGame.Flow       (sin referencias a UnityEngine)
│  ├─ Events/                  MainEvents.cs, GameEvents.cs
│  ├─ Config/                  GameMode, GameConfig (inmutable), GameConfigBuilder, GameSession
│  ├─ Services/                IApplicationQuitter
│  ├─ Common/                  StateBase, TransitionOnEventState<T>
│  ├─ Main/                    MainMachine + Booting, MainMenu, MenuOptions, GameSetup, InGame, Quitting
│  └─ Game/                    GameMachine (internal) + los 12 estados del turno
├─ Unity/                      TurnGame.Unity      (capa fina opcional)
│  ├─ GameFlowRunner.cs        MonoBehaviour que crea el broker, arranca MainMachine y le da Update
│  └─ UnityApplicationQuitter.cs
└─ Tests/EditMode/             TurnGame.Tests.EditMode
   ├─ Support/                 SpyState, FakeStateChanger, RecordingBroker, harnesses
   ├─ Core/                    broker, StateMachine, comportamiento de estados aislados
   ├─ Main/                    tabla de transiciones, GameSetup, InGame
   └─ Game/                    tabla de transiciones, ciclo, pausa, game over, respuestas síncronas
docs/
├─ DESIGN.md                   referencia: estados, eventos, transiciones, reglas
└─ TESTING.md                  cómo correr y extender los tests
```

## Uso rápido

En una escena de arranque, añade `GameFlowRunner` a **un** GameObject. Tus sistemas (UI, dados, tablero,
input, audio) obtienen `GameFlowRunner.Instance.Broker`, se suscriben en `Awake`/`OnEnable` y responden a las
solicitudes:

```csharp
using TurnGame.Core;
using TurnGame.Flow.Events;
using TurnGame.Unity;
using UnityEngine;

public class DiceSystem : MonoBehaviour
{
    IMessageBroker _broker;

    void Awake()
    {
        _broker = GameFlowRunner.Instance.Broker;   // GameFlowRunner corre antes (ExecutionOrder -1000)
        _broker.Subscribe<RollElementsRequested>(OnRollRequested);
    }

    void OnDestroy() => _broker.Unsubscribe<RollElementsRequested>(OnRollRequested);

    void OnRollRequested(RollElementsRequested _)
    {
        // ... tirar dados, animar ...
        _broker.Publish(new ElementsRolled());       // puede ser inmediato o al terminar la animación
    }
}
```

Regla de oro: **la máquina solo secuencia**. Cada estado, al entrar, se suscribe a su respuesta, publica su
solicitud (`...Requested`) y espera el hecho (`...ed`). No calcula nada del juego.

### Pausa y game over desde tus scripts

```csharp
broker.Publish<PauseRequested>();     // congela (ignorado en Paused / InGameOptions / GameOverScreen)
broker.Publish<ResumeRequested>();    // solo desde Paused
broker.Publish<GameOverReached>();    // desde cualquier estado del juego (vidas, intentos, etc.)
```

`GamePaused` / `GameResumed` los publica la máquina: tus sistemas de input, animación y audio deben
congelarse/reanudarse con ellos (la máquina dejando de llamar `Update` no los detiene).


## Decisiones que conviene conocer

- **`IStateChanger.ChangeState(source, target)`** recibe quién pide el cambio: así la máquina ignora peticiones
  de estados que ya no están activos.
- **Eventos durante la pausa no se pierden**: si el estado suspendido pide una transición mientras está
  pausado (p. ej. `ElementsRolled` llega con el juego en pausa), se aplica justo después de reanudar.
- **Los cambios se aplican en `Update`**, nunca dentro de `Enter`/handlers. Cadenas síncronas se resuelven
  en el mismo tick; un bucle síncrono infinito lanza `InvalidOperationException` (límite 1000) en vez de congelar el juego.
- **`GameOverReached` descarta lo pendiente del tick**, también desde pausa/opciones. No publica `GameResumed`;
  trata `GameOverScreenRequested` como el reinicio de tus sistemas.
- **`GameMachine` es `internal`**: solo `InGameState` la crea; el único ensamblado extra con acceso es el de tests.
- **Placeholders a reemplazar**: `GameMode` (valores), `GameConfig` (parámetros) y el payload de `PieceMoved`
  (`X`, `Y`, `IsValid`).
- **`GameFlowRunner` pide pausa al perder el foco** (`OnApplicationFocus(false)`). En el editor también ocurre
  al cambiar de ventana; quítalo si molesta.


