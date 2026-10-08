using _Project.Core.Factories;
using Object = UnityEngine.Object;

namespace _Project.Infrastructure.Factories
{
    public class ObjectFactory : IObjectFactory
    {
        public T Create<T>(T original) where T : Object
        {
            return Object.Instantiate(original);
        }
    }
}
