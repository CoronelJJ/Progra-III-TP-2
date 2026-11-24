namespace DiceMaterials.Runtime
{
    /// <summary>Abstracción de creación de vistas. Hoy es un pool; podría ser Instantiate directo.</summary>
    public interface IDieFactory
    {
        /// <summary>Entrega un Die ya inicializado con el dato.</summary>
        Die Get(DieData data);

        /// <summary>Devuelve el Die. Después de esto no debe usarse.</summary>
        void Release(Die die);
    }
}
