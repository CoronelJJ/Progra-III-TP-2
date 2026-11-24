# Puesta en marcha en Unity: sistema de dados + puente con TurnGame

Qué tenés que copiar, qué crear en el Project, qué va en cada escena, y cómo usarlo en un turno.
Requisitos: **Unity 2021.2+** (C# 9) y **VContainer** instalado (su assembly se llama `VContainer`).

---

## 1. Qué trae el zip

```
Assets/Scripts/
├─ DiceMaterials/                  SISTEMA DE DADOS  → copiar ENCIMA de la carpeta existente
│  ├─ Core/                        asmdef DiceMaterials.Core (C# puro, sin UnityEngine)
│  │  ├─ Common/Seed/              SplitMix64, RollerSlot, RollerSeeds, GameSeed, SystemEntropy
│  │  ├─ Common/Hashing/           Fnv1a64Hasher, StableHash
│  │  ├─ Common/Rolling/           ShuffleBag, RandomPicker, SelectionMode
│  │  ├─ Materials/  DieValues/  Pieces/    un roller por carpeta (interfaz + implementación)
│  │  ├─ Roll/                     DiceRollSystem (orquesta los 3), DiceRollResult, DiceSystemState, ContentFingerprint
│  │  └─ Save/                     SaveService, GameSaveData, ISaveContributor, ...
│  ├─ Runtime/                     asmdef DiceMaterials.Runtime (Unity)
│  │  ├─ Common/  Context/  Materials/  Pieces/  Save/
│  ├─ Integration/VContainer/      asmdef DiceMaterials.VContainer: RegisterDiceSystem / RegisterSaveSystem
│  ├─ Editor/                      inspectores (Id solo lectura, botones de las databases)
│  └─ Tests/EditMode, EditMode.Runtime
└─ TurnGame/                       PUENTE CON TU JUEGO
   ├─ Core/                        REEMPLAZA IGameContext.cs  +  NUEVOS Messages/ (6 mensajes)
   ├─ Runtime/                     REEMPLAZA GameContext.cs y GameSettingsSO.cs (en el lugar donde estén hoy)
   ├─ Persistence/                 NUEVO asmdef TurnGame.Persistence: adaptador del broker
   └─ Persistence.Tests/EditMode/  tests del adaptador (usan tu MessageBroker real)
```

## 2. Copiar los archivos

**Si ya creaste assets (materiales, database, tabla), no borres la carpeta `DiceMaterials`:** copiá encima. Así cada script conserva su `.meta` y los assets no quedan con "Missing script".

1. Copiá `Assets/Scripts/DiceMaterials/` sobre la existente.
2. **Borrá estos archivos viejos** (ya no existen en el zip; copiar encima no los elimina):
   - `DiceMaterials/Core/Dice/` (Dice.cs, IDice.cs) *: ese dado se resuelve en otro lado*
   - `DiceMaterials/Core/Roll/RollResult.cs` *(reemplazado por DiceRollResult)*
   - `DiceMaterials/Tests/EditMode/Dice/`
3. `TurnGame/Core/IGameContext.cs`, `TurnGame/Runtime/GameContext.cs`, `TurnGame/Runtime/GameSettingsSO.cs` **reemplazan a tus archivos del mismo nombre**: sobrescribilos donde estén hoy, no los dejes duplicados.
4. `TurnGame/Core/Messages/*.cs` son nuevos: ponelos dentro de la carpeta del asmdef `TurnGame.Core`.
5. `TurnGame/Persistence/` y `TurnGame/Persistence.Tests/` son nuevos (traen sus propios asmdef).

> Si Unity marca "type already defined", queda un archivo viejo en otra ruta: buscalo y borralo.

## 3. Referencias de assembly (Assembly Definition References)

| Assembly | Agregar referencia a |
|---|---|
| **TurnGame.Core** (tu asmdef) | `DiceMaterials.Core` (porque `IGameContext` expone un `GameSeed`). Sigue siendo C# puro |
| El assembly donde estén **GameContext.cs / GameSettingsSO.cs** | `TurnGame.Core`, `DiceMaterials.Core`, `DiceMaterials.Runtime`. (Si no tienen asmdef, van a Assembly-CSharp y ya los ven) |
| El código que **arma la partida** (escena de selección) | `DiceMaterials.Core`, `DiceMaterials.Runtime`, `TurnGame.Core` |
| El código del **GameScope** (escena de juego) | `DiceMaterials.Core`, `DiceMaterials.Runtime`, `DiceMaterials.VContainer`, `TurnGame.Core`, `TurnGame.Persistence`, `VContainer` |
| El código de **turnos** que usa los dados | `DiceMaterials.Core` (y `DiceMaterials.Runtime` si necesita Sprite/Material) |

> `GameSettingsSO` usa UnityEngine: **no puede** estar dentro de `TurnGame.Core` (tiene `noEngineReferences`).

## 4. Assets a crear en el Project

Menú **Assets > Create**:

| Asset | Menú | Cuántos | Qué completar |
|---|---|---|---|
| Material de dado | `Dice > Die Material` | uno por material | tier (S/A/B/C), familia, puntaje, multiplicador (1 = neutro), `Material` de Unity. Los **colores** son tier C con familia Color |
| Database de materiales | `Dice > Die Material Database` | 1 | botones **Recolectar del proyecto** y luego **Asignar IDs** |
| Pieza | `Dice > Piece` | una por pieza | `Dice Required` (cantidad de dados), `Sprite` |
| Database de piezas | `Dice > Piece Database` | 1 | **Recolectar del proyecto** y **Asignar IDs** |
| Tabla de proporciones | `Dice > Tier Proportion Table` | 1 | ya viene S=1, A=4, B=10, C=25, todo en modo Bag. Ajustar con las pruebas |
| Ajustes de partida | `Game > Game Settings` | 1 | tipo de dado (D6/D12) y **Copies Per Face** (por defecto 4) |

**Orden recomendado:** crear materiales y piezas, luego las databases, y en cada database "Recolectar" y "Asignar IDs". El inspector de la database muestra un cuadro amarillo si hay IDs en 0, duplicados, nulos o repetidos ("Reparar duplicados" conserva el primero de la lista).

**Mínimo para que arranque:** al menos 1 material en un tier que tenga regla con copias > 0, y al menos 1 pieza. Los tiers sin materiales se omiten con un warning en consola (`[DieMaterialRoller] ...`).

## 5. Escenas

El sistema **no pone nada visual** en las escenas (no hay prefab de dado ni vista): devuelve datos y otro sistema los interpreta.

### 5.1 Escena de selección: construir el `GameContext`

No hay GameObjects nuevos. Solo el código que decide cómo empieza la partida:

```csharp
using System;
using DiceMaterials;
using DiceMaterials.Runtime;
using TurnGame.Core;

// PARTIDA NUEVA. seedText viene de un input opcional; vacío = aleatoria.
var seed = GameSeed.FromInput(seedText, SystemEntropy.NextUInt64);
var context = new GameContext(settings.DieType, settings.CopiesPerFace, seed);

// CONTINUAR un guardado: se lee SIN aplicar, solo para recuperar la seed.
var reader = new SaveService(Array.Empty<ISaveContributor>(), new JsonUtilitySaveSerializer(), new FileSaveStorage());
if (reader.TryRead(slot, out var data) && data.TryGetSeed(out var savedSeed))
    context = new GameContext(settings.DieType, settings.CopiesPerFace, savedSeed, loadSlot: slot);
```

- `seed.Text` es lo que se le muestra al jugador (para compartir la partida); para una seed aleatoria es un número.
- `TryRead` puede lanzar excepción (guardado ilegible o de una versión más nueva): atrapala y mostrá el error.
- El `GameContext` viaja a la escena de juego con tu mecanismo de traspaso actual.

### 5.2 Escena de juego: un GameObject con el `GameScope`

Crear un GameObject vacío `GameScope`, agregarle el componente que hereda de `LifetimeScope`, y asignar en el inspector `materialDb`, `pieceDb` y `tierTable`.

```csharp
using DiceMaterials;
using DiceMaterials.Integration;
using DiceMaterials.Runtime;
using TurnGame.Core;
using TurnGame.Persistence;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameScope : LifetimeScope
{
    [SerializeField] DieMaterialDatabase materialDb;
    [SerializeField] PieceDatabase pieceDb;
    [SerializeField] TierProportionTable tierTable;

    protected override void Configure(IContainerBuilder builder)
    {
        // Lo que ya tenés: IMessageBroker y el GameContext que llegó de la selección.
        // El GameContext debe registrarse con TODAS estas interfaces:
        builder.RegisterInstance(gameContext)
               .As<IGameContext>().As<IDieTypeSource>().As<IFaceCopiesSource>().As<ISeedSource>();

        // Dados: 3 rollers + IDiceRollSystem. Hoy entran TODOS los materiales y piezas.
        builder.RegisterDiceSystem(materialDb.Materials, tierTable, pieceDb.Pieces);

        // Guardado: SaveService (JSON + archivo) y adaptador del broker.
        builder.RegisterSaveSystem();
        builder.RegisterSaveLoadAdapter();
    }
}
```

Todo en el **mismo scope** (o `GameContext` y el broker en un padre). El orden de los `Register` no importa.

## 6. Usarlo en un turno

```csharp
IPiece piece = dice.DrawPiece();               // 1 por turno, obligatoria y se consume al sacarla
DiceRollResult roll = dice.RollDice(6);        // la cantidad base la decide tu sistema de turnos
// dado i  =  roll.Materials[i]  +  roll.Faces[i]  (cara 1..FaceCount)

DiceRollResult extra = dice.RollDice(1);       // dado extra: de a uno o en lote da la MISMA secuencia
```

- Pieza: `piece.Id`, `piece.Name`, `piece.DiceRequired`; el sprite está en `(piece as IPieceVisual)?.Sprite`.
- Material: puntaje con `Score`, `Multiplier`, `FinalScore`; el `Material` de Unity en `(m as IDieMaterialVisual)?.UnityMaterial`; la familia (`Family`) para las jugadas.
- Los tres sorteos son independientes: pedir dados extra no altera la secuencia de piezas ni al revés.

## 7. Guardado y carga (por mensajes del broker)

| Mensaje | Lo publica | Efecto |
|---|---|---|
| `SaveRequested(slot)` | quien quiera guardar | el adaptador guarda |
| `GameSaved(slot)` / `SaveFailed(slot, error)` | el adaptador | resultado |
| `LoadRequested(slot)` | el arranque de la escena de juego | el adaptador lee y aplica |
| `GameLoaded(slot)` / `LoadFailed(slot, error)` | el adaptador | resultado |

**Cuándo guardar:** solo en el **límite de un turno**, antes de `DrawPiece()` del siguiente. Así no hace falta guardar la mano en curso. Lo guardado es la seed, una huella del contenido y los contadores de cada roller.

```csharp
broker.Publish(new SaveRequested("autosave"));     // al terminar el turno
```

**Cuándo cargar:** al arrancar la partida, **antes de la primera tirada**, desde el primer estado de tu máquina de estados (no desde un `IStartable` suelto: el orden entre entry points no está garantizado):

```csharp
if (!string.IsNullOrEmpty(context.LoadSlot))
    broker.Publish(new LoadRequested(context.LoadSlot));
```

Suscribite a `LoadFailed` y `SaveFailed` para avisar al jugador. `LoadFailed` aparece cuando:
- el guardado no existe o está ilegible,
- la **seed no coincide** o el **contenido cambió** (materiales, tiers, proporciones, caras, copias por cara, piezas),
- la partida ya avanzó más que el guardado (cargar solo vale al comenzar).

**Qué NO se guarda todavía:** la mano en curso, los ajustes (`DieType`, `CopiesPerFace`: si no coinciden al cargar, falla como "contenido cambió") ni el resto de los sistemas. Para sumar uno: un campo `[Serializable]` en `GameSaveData` y un `ISaveContributor` registrado en el mismo scope.

## 8. Verificar

1. **Consola sin errores de compilación.** Si falla algo con `VContainer`, revisá que el assembly se llame así.
2. **Window > General > Test Runner > EditMode > Run All**: todo en verde (los de `DiceMaterials.Tests.*` y `TurnGame.Persistence.Tests`).
3. Databases sin cuadro amarillo; consola sin warnings `[DieMaterialRoller]` inesperados.
4. En Play: dos partidas con la **misma seed** dan la misma secuencia de piezas, dados y materiales.

## 9. Problemas comunes

| Síntoma | Causa |
|---|---|
| Assets con "Missing script" | se borró y recreó la carpeta (cambian los GUID). Copiar encima |
| `VContainerException: ISeedSource is not registered` | el `GameContext` no se registró `.As<ISeedSource>()` (ni `IDieTypeSource`, `IFaceCopiesSource`) |
| `ArgumentException: ... Id ... sin asignar / duplicado` | falta "Asignar IDs" o hay duplicados ("Reparar duplicados") |
| Warning "Tier X ... no hay regla / 0 copias / sin materiales" | el tier no tiene regla, o la regla no tiene materiales, o al revés |
| Warning "múltiplo de 6" | el total de la bolsa de tiers es múltiplo de 6 (patrón más predecible) |
| `LoadFailed` al continuar con otros ajustes | cambió el contenido desde el guardado |
| El adaptador no responde | `RegisterSaveLoadAdapter()` falta, o el broker no es el mismo que usa quien publica |

## 10. Decisiones fijas (para no pisarlas)

- Una seed maestra `ulong` por partida; cada roller deriva la suya por **slot fijo** (`RollerSlot`: solo agregar al final).
- Texto → seed: vacío = aleatoria; solo dígitos = ese número; otro texto = FNV-1a + SplitMix64 (sensible a mayúsculas, con trim).
- Bolsas sin refill anticipado ni guard de borde; los dados no usados se consumen; la pieza es 1 por turno y no hay preview.
- Los IDs (materiales y piezas) los asigna la database y nunca se reutilizan.
