# Die Material System

Copiar `Assets/Scripts/DiceMaterials` dentro de tu proyecto Unity.

## Estructura

```
DiceMaterials/
├─ Core/                         (C# puro, sin UnityEngine)
│  ├─ Common/                    GENÉRICO: lo comparten los 3 rollers
│  │  ├─ Seed/                   SplitMix64, RollerSlot, RollerSeeds
│  │  └─ Rolling/                IPicker, ShuffleBag, RandomPicker, SelectionMode
│  ├─ Materials/                 ROLLER 1: IDieMaterial, MaterialTier, MaterialFamily, TierRule,
│  │                             DieMaterialMath, IDieMaterialRoller, DieMaterialRoller
│  ├─ DieValues/                 ROLLER 2: IDieValueRoller, DieValueRoller
│  ├─ Pieces/                    ROLLER 3: IPiece, IPieceRoller, PieceRoller
│  └─ Dice/                      El dado (lógica): IDice, Dice  (usa valor + material)
├─ Runtime/                      (Unity)
│  └─ Materials/                 DieMaterialSO, IDieMaterialVisual, TierProportionTable,
│                                DieMaterialDatabase, DieMaterialRollerFactory
├─ Editor/
│  └─ Materials/                 Inspectores de la Database y del SO
└─ Tests/EditMode/               (espeja la estructura de Core)
   ├─ Common/Seed/               SplitMix64Tests, RollerSeedsTests
   ├─ Common/Rolling/            ShuffleBagTests
   ├─ Materials/                 DieMaterialRollerTests, FakeMaterial
   ├─ DieValues/                 DieValueRollerTests
   ├─ Pieces/                    PieceRollerTests, FakePiece
   ├─ Dice/                      DiceTests
   └─ Integration/               MasterSeedIntegrationTests (los 3 rollers + dados desde una seed maestra)
```

Las carpetas de `Runtime` y `Editor` solo existen para Materials: los otros dos rollers y el dado son C# puro.
Los asmdef (`Core`, `Runtime`, `Editor`, `Tests.EditMode`) están en la raíz de cada capa y cubren sus subcarpetas.

## Seed maestra
Una sola seed `ulong` por partida. Cada roller recibe la suya, derivada en un slot fijo:

    var materialSeed = RollerSeeds.Derive(master, RollerSlot.Material);
    var valueSeed    = RollerSeeds.Derive(master, RollerSlot.DieValue);
    var pieceSeed    = RollerSeeds.Derive(master, RollerSlot.Piece);

    var materials = new DieMaterialRoller(pool, rules, materialSeed);
    var values    = new DieValueRoller(maxFace, copiesPerFace, valueSeed);   // Bag por defecto
    var pieces    = new PieceRoller(pieceSet, pieceSeed);

REGLAS: los slots de `RollerSlot` solo se agregan al final; nunca reordenar ni renumerar.
Estado de cada roller reconstruible con (seed, DrawnCount).
En Unity, `DieMaterialRollerFactory.Create(pool, table, masterSeed)` deriva el slot Material por vos.

## Armar una mano
    var values    = valueRoller.GetRolledDieValues(6);
    var materials = materialRoller.GetRolledDieMaterials(6);
    var dice      = values.Select((v, i) => new Dice(v, materials[i])).ToList();

## Rollers
| Roller | Entrada | Salida | Mecánica |
|---|---|---|---|
| DieMaterialRoller | IDieMaterial + TierRule | GetNextDieMaterial / GetRolledDieMaterials(n) | 2 niveles (bolsa de tiers + selector por tier), Bag o Random por tier |
| DieValueRoller | maxFace, copiesPerFace | GetNextDieValue / GetRolledDieValues(n) | Bolsa de caras 1..maxFace (o Random) |
| PieceRoller | IPiece | GetNextPiece / GetRolledPieces(n) | Bolsa estilo Tetris, 1 ficha por pieza |

`GetRolled*(n)` es siempre equivalente a n llamadas a `GetNext*()`; si n supera lo que queda en la bolsa,
se agota, se vuelve a mezclar y se continúa.

## Flujo de materiales
1. Create > Dice > Die Material / Database / Tier Proportion Table.
2. En la Database: "Recolectar materiales" y "Asignar IDs".

## Notas
- La seed solo reproduce la misma secuencia con el mismo contenido (pool, tiers, copias, modos, maxFace, piezas).
- `copiesPerFace = 1` con maxFace = nº de dados hace que cada mano sea una permutación (sin pares/tríos).
- `Dice` vive en el namespace `BoardGame.Model` (como tu script); el resto del Core usa `DiceMaterials`.
