using System;

namespace ModuleFramework.SystemsOrder
{
    public interface IEcsSystemsOrderRepository
    {
        public int GetLastDefaultOrder { get; }

        public bool TryGetCustomOrder(Type type, out int order);
    }
}