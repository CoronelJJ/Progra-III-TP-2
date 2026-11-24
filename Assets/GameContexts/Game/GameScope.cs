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
    GameContext gameContext;

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