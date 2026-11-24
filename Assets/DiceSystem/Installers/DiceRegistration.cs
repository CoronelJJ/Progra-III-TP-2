using VContainer;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Registro de VContainer para el scope de Player.
    /// Debe existir en el scope (o en un padre): IMessageBroker (del Player), IDieTypeSource (scope general)
    /// e IDieMaterialProvider (tu database de materiales).
    /// </summary>
    public static class DiceRegistration
    {
        public static void RegisterPlayerDice(this IContainerBuilder builder,
                                              FaceValueDatabaseSO faceDatabase,
                                              DiePrefabMapSO prefabMap)
        {
            builder.RegisterInstance(faceDatabase).As<IFaceValueProvider>();
            builder.RegisterInstance(prefabMap);

            //builder.Register<FaceStyleHolder>(Lifetime.Scoped).As<IFaceStyleSource, IFaceStyleController>();
            builder.Register<DieDataBuilder>(Lifetime.Scoped);

            // IDisposable: el contenedor lo dispone al cerrar el scope y destruye los dados.
            builder.Register<DiePool>(Lifetime.Scoped).As<IDieFactory>();
            builder.Register<DieVisualBuilder>(Lifetime.Scoped);

            builder.Register<DieSaveMapper>(Lifetime.Scoped);
            builder.Register<DieRecreator>(Lifetime.Scoped);
        }
    }
}
