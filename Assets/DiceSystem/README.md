# DiceSystem

Core/        Estilo de cara (enum, mensaje, holder) y tipo de dado.
Faces/       FaceValueSO (1..12), su database y proveedor.
Data/        DieData (lógica pura) y DieDataBuilder (materiales + ints -> DieData).
View/        Die (prefab), pool, mapa tipo->prefab y DieVisualBuilder (DieData -> Die).
Recreation/  Todo lo necesario para recrear dados desde un guardado (ids -> DieData -> Die).
Installers/  Registro de VContainer para el scope de Player.

Requiere ya existentes en tu proyecto: DieMaterialSO, IDieMaterial, IDieMaterialVisual
(con `Material UnityMaterial { get; }`), IMessageBroker.
Falta implementar: IDieMaterialProvider (con tu DieMaterialDatabase) e IDieTypeSource (GameContext general).

Flujo:  Sorteador -> DieDataBuilder -> List<DieData> -> DieVisualBuilder -> List<Die>
Guardar: DieSaveMapper.ToSave(...)   Recrear: DieRecreator.RestoreWithViews(...)
