namespace ORMFramework.Ioc;

public interface IComponentProvider
{
    T GetComponent<T>();
}

