using UnityEngine.Events;

namespace Zlitz.General.Management
{
    public interface IObservableValue<T>
    {
        event UnityAction<T> onValueChanged;
    }
}
