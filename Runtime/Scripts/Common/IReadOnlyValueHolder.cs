namespace Zlitz.General.Management
{
    public interface IReadOnlyValueHolder<T>
    {
        T value { get; }
    }
}
