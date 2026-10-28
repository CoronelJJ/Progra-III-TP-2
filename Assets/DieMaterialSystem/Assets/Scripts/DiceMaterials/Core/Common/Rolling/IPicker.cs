namespace DiceMaterials
{
    public interface IPicker<out T>
    {
        T Next();
    }
}
