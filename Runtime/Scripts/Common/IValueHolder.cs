namespace Zlitz.General.Management
{
    public interface IValueHolder<T> : IReadOnlyValueHolder<T>
    {
        new T value { get; set; }
    }
}
