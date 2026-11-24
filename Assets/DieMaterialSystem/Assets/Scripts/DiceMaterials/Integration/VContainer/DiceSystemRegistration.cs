using System;
using System.Collections.Generic;
using DiceMaterials.Runtime;
using VContainer;

namespace DiceMaterials.Integration
{
    public static class DiceSystemRegistration
    {
        /// <summary>Modo de selección del roller de caras (también entra en la huella del contenido).</summary>
        private const SelectionMode FaceMode = SelectionMode.Bag;

        /// <summary>
        /// Registra los 3 rollers y el IDiceRollSystem (singletons).
        /// Requiere en el contenedor (o su padre), normalmente todos implementados por GameContext:
        ///   ISeedSource, IDieTypeSource, IFaceCopiesSource.
        /// Los pools se pasan ya listos: hoy "todos" (database.Materials / database.Pieces); mañana, filtrados o por paquetes.
        /// </summary>
        public static void RegisterDiceSystem(
            this IContainerBuilder builder,
            IReadOnlyList<IDieMaterial> materialPool,
            TierProportionTable tierTable,
            IReadOnlyList<IPiece> piecePool)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (materialPool == null) throw new ArgumentNullException(nameof(materialPool));
            if (tierTable == null) throw new ArgumentNullException(nameof(tierTable));
            if (piecePool == null) throw new ArgumentNullException(nameof(piecePool));

            builder.Register<IDieMaterialRoller>(
                r => DieMaterialRollerFactory.Create(materialPool, tierTable, r.Resolve<ISeedSource>().Seed.Value),
                Lifetime.Singleton);

            builder.Register<IDieValueRoller>(r =>
            {
                var die = r.Resolve<IDieTypeSource>();
                var copies = r.Resolve<IFaceCopiesSource>();
                var seed = r.Resolve<ISeedSource>().Seed;
                return new DieValueRoller(
                    die.FaceCount, copies.CopiesPerFace,
                    RollerSeeds.Derive(seed.Value, RollerSlot.DieValue), FaceMode);
            }, Lifetime.Singleton);

            builder.Register<IPieceRoller>(
                r => new PieceRoller(piecePool, RollerSeeds.Derive(r.Resolve<ISeedSource>().Seed.Value, RollerSlot.Piece)),
                Lifetime.Singleton);

            builder.Register<IDiceRollSystem>(r =>
            {
                var die = r.Resolve<IDieTypeSource>();
                var copies = r.Resolve<IFaceCopiesSource>();
                var seed = r.Resolve<ISeedSource>().Seed;
                // Los rollers se resuelven primero: si el contenido es inválido, falla acá con un mensaje claro.
                var materials = r.Resolve<IDieMaterialRoller>();
                var faces = r.Resolve<IDieValueRoller>();
                var pieces = r.Resolve<IPieceRoller>();
                ulong fingerprint = ContentFingerprint.Compute(
                    materialPool, tierTable.ToRules(), die.FaceCount, copies.CopiesPerFace, FaceMode, piecePool);
                return new DiceRollSystem(materials, faces, pieces, seed, fingerprint);
            }, Lifetime.Singleton);

            builder.Register<DiceSystemSaveContributor>(Lifetime.Singleton).As<ISaveContributor>();
        }

        /// <summary>
        /// Registra el SaveService con JSON + archivos. Recibe todos los ISaveContributor registrados
        /// (deben estar en el mismo scope o en un padre).
        /// </summary>
        public static void RegisterSaveSystem(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.Register<ISaveSerializer>(_ => new JsonUtilitySaveSerializer(), Lifetime.Singleton);
            builder.Register<ISaveStorage>(_ => new FileSaveStorage(), Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
        }
    }
}
