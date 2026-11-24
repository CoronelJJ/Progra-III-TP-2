namespace DiceMaterials.Runtime
{
    public interface IFaceValueProvider
    {
        bool TryGet(int value, out FaceValueSO face);
    }
}
