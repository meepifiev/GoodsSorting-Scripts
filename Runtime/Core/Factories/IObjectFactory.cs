using UnityEngine;

namespace _Project.Core.Factories
{
    public interface IObjectFactory
    {
        T Create<T>(T original) where T : Object;
    }
}
