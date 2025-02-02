namespace Zlitz.General.Management
{
    public interface IServiceLocator
    {
        public bool Register<T>(T service) where T : class;

        public bool Unregister<T>() where T : class;

        public T Get<T>() where T : class;
    }
}
