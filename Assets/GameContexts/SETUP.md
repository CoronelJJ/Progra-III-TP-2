# GameContext + PlayerContext: uso y setup en Unity

## 1. Qué hay y cómo se relacionan

```
Scope padre (partida / máquina de estados)
 └─ GameContext  : IGameContext, IDieTypeSource     (config global, fija)

     Scope hijo por jugador  (se crea y se dispone con el jugador)
      ├─ IPlayer (Id, Name)
      ├─ MessageBroker       : IMessageBroker        (UNO POR JUGADOR)
      ├─ PlayerContext       : IPlayerContext, IFaceStyleSource, IFaceStyleController
      └─ DiceSystem          : DieDataBuilder, DiePool, DieVisualBuilder, DieSaveMapper...
```

Regla de oro: **los sistemas no reciben el contexto entero, reciben la interfaz estrecha**.
Ejemplo: `DiePool` pide `IDieTypeSource` e `IFaceStyleSource`, nunca `GameContext` ni `PlayerContext`.
Así los contextos pueden crecer sin tocar a los demás sistemas.

VContainer resuelve hacia arriba: un servicio del scope hijo (`DiePool`) puede pedir algo
registrado en el padre (`IDieTypeSource`). Al revés no.

## 2. Archivos

```
GameContexts/
  Game/        DieType, GameSettingsSO, IGameContext, GameContext
  Player/      IPlayer, Player, IPlayerContext, PlayerContext
  Installers/  ContextRegistration (RegisterGameContext / RegisterPlayerContext)
```

Namespace: `TurnGame.Core` (el mismo del MessageBroker). Dependen de `DiceMaterials.Runtime`
(DiceSystem) por `IDieTypeSource`, `IFaceStyleSource`, `IFaceStyleController` y `FaceStyleHolder`.

## 3. Cambio necesario en el DiceSystem

`PlayerContext` ya implementa `IFaceStyleSource`/`IFaceStyleController` (usa `FaceStyleHolder` por dentro).
Para no tener dos holders, en `Installers/DiceRegistration.cs` **borra esta línea**:

```csharp
builder.Register<FaceStyleHolder>(Lifetime.Scoped).As<IFaceStyleSource, IFaceStyleController>();
```

Conserva `Core/FaceStyleHolder.cs` (contiene las dos interfaces y la clase que usa el PlayerContext).

Opcional, para precalentar el pool: cambia el registro del pool a
`builder.Register<DiePool>(Lifetime.Scoped).As<IDieFactory>().AsSelf();` y llama `Prewarm(n)`
al crear el scope. Con solo `As<IDieFactory>()` no se puede acceder a `Prewarm`.

## 4. Assets a crear en Unity

1. **GameSettings**: `Create > Game > Game Settings`. Elige `DieType` (D6 o D12).
2. **FaceValueSO** del 1 al 12: `Create > Dice > Face Value`. Cada uno con su `value`, el
   `defaultStyle` y las variantes (estilo + sprite). Pon al menos el sprite del estilo por defecto.
3. **FaceValueDatabase**: `Create > Dice > Face Value Database`. Arrastra los 12 FaceValueSO.
4. **DieMaterialSO**: los que ya tienes.
5. **DiePrefabMap**: `Create > Dice > Die Prefab Map`. Una entrada por tipo (6 → prefab d6, 12 → prefab d12).

### Prefab del Die
```
Die (componente Die)
 ├─ Body        Cubo con MeshRenderer                → campo "Body Renderer"
 ├─ Face_Top    GameObject con SpriteRenderer         ┐
 ├─ Face_...    (uno por cara que quieras mostrar)    ├ campo "Face Planes"
```
- Cada plano pegado a su cara con un offset mínimo (≈0.001) para evitar z-fighting.
- Cada plano debe mirar hacia afuera. Ejemplo cara superior: rotación X = 90.
  Comprueba en la vista de escena que se lee bien, y que el 6 y el 9 no quedan invertidos.
- Escala del plano: según el pixels-per-unit de tus sprites, para que ocupen la cara.
- `Show All Faces` (bool): marcado = todos los planos muestran el sprite; sin marcar = solo `Visible Plane Index`.

## 5. Registro (VContainer)

### Scope padre
```csharp
using TurnGame.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private GameSettingsSO settings;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterGameContext(settings);
        // ... otros sistemas generales
    }
}
```

### Scope hijo de un jugador
```csharp
using DiceMaterials.Runtime;
using TurnGame.Core;
using VContainer;
using VContainer.Unity;

public sealed class PlayerScopeFactory
{
    private readonly LifetimeScope parent;
    private readonly FaceValueDatabaseSO faces;
    private readonly DiePrefabMapSO prefabs;
    private readonly IDieMaterialProvider materials; // tu database de materiales

    public PlayerScopeFactory(LifetimeScope parent, FaceValueDatabaseSO faces,
                              DiePrefabMapSO prefabs, IDieMaterialProvider materials)
    {
        this.parent = parent; this.faces = faces; this.prefabs = prefabs; this.materials = materials;
    }

    public LifetimeScope Create(IPlayer player)
    {
        return parent.CreateChild(builder =>
        {
            builder.RegisterPlayerContext(player);          // IPlayer, broker, PlayerContext
            builder.RegisterInstance(materials).As<IDieMaterialProvider>();
            builder.RegisterPlayerDice(faces, prefabs);     // DiceSystem
        }, $"Player_{player.Id}");
    }
}
```
Si tu versión de VContainer no tiene `CreateChild(Action<IContainerBuilder>, string)`, usa la
sobrecarga con `IInstaller`, o `IObjectResolver.CreateScope(builder => { ... })` si no usas LifetimeScope.

Crear un jugador: `factory.Create(new Player(1, "Ana"))`.

## 6. Uso

Los servicios se resuelven por constructor dentro del scope del jugador:

```csharp
public sealed class RoundSystem
{
    public RoundSystem(DieDataBuilder dataBuilder, DieVisualBuilder visualBuilder,
                       IFaceStyleController style) { ... }
}
```

Flujo de dados:
```
sorteador -> (materiales, ints) -> DieDataBuilder.Build -> List<DieData>
          -> DieVisualBuilder.Build -> List<Die>
```

Cambiar el estilo de las caras (solo de ESE jugador):
```csharp
style.SetStyle(FaceStyle.Pips);   // publica FaceStyleChanged en el broker del jugador
```

Leer el estilo actual: inyecta `IFaceStyleSource` y usa `.Current`.

Leer la config global: inyecta `IDieTypeSource` (`.FaceCount`) o `IGameContext` (`.DieType`).

## 7. Cierre del scope

Al salir del estado o eliminar al jugador, dispone el scope hijo:
```csharp
playerScope.Dispose();
```
El contenedor dispone `DiePool` (destruye todos los dados, activos o no). Cada `Die` se
desuscribe del broker al liberarse o destruirse, así que no quedan suscripciones colgadas.

## 8. Cómo extender los contextos

Para añadir estado nuevo (ej. puntaje del jugador):
1. Crea una interfaz estrecha: `IScoreSource` (lectura) y, si hace falta, `IScoreController` (escritura).
2. Haz que `PlayerContext` (o una clase que este envuelva, como hoy con `FaceStyleHolder`) la implemente.
3. Agrégala al `.As<...>()` en `RegisterPlayerContext`.
4. Los sistemas piden `IScoreSource`, nunca `PlayerContext`.

Lo mismo con `GameContext` para estado global. Si una clase de estado crece mucho, sepárala como se hizo
con `FaceStyleHolder` y deja el contexto como fachada.

## 9. Errores comunes

| Síntoma | Causa probable |
|---|---|
| `VContainerException: No such registration of type IDieTypeSource` | Falta `RegisterGameContext` en el scope padre, o el scope del jugador no es hijo de él. |
| `No such registration ... IMessageBroker` | Falta `RegisterPlayerContext` (registra el broker del jugador). |
| `No such registration ... IDieMaterialProvider` | Falta registrar tu database de materiales en el scope del jugador. |
| `DiePrefabMap '...' no tiene prefab para dados de N caras` | El `DiePrefabMap` no tiene entrada para ese `DieType`. |
| Los dados de un jugador cambian cuando cambia otro | Dos jugadores comparten broker: cada scope hijo debe registrar el suyo. |
| Cambio de estilo no se ve | El dado no está inicializado (fuera del pool) o el `FaceValueSO` no tiene sprite para ese estilo ni para el por defecto. |
| Dos `IFaceStyleSource` ambiguos | Quedó la línea de `FaceStyleHolder` en `DiceRegistration` (ver sección 3). |

## 10. Pendiente de tu lado

- Implementar `IDieMaterialProvider` con tu database de materiales.
- Confirmar que `IDieMaterialVisual` expone `Material UnityMaterial { get; }`.
- Decidir dónde se crea el `PlayerScopeFactory` (la máquina de estados o su composition root).
