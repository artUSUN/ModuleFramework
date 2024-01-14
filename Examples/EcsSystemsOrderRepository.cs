using System;
using System.Collections.Generic;
using ModuleFramework.Interfaces;
using ModuleFramework.SystemsOrder;

namespace ModuleFramework.Examples
{
    public class EcsSystemsOrderRepository : IEcsSystemsOrderRepository
    {
        public int GetLastDefaultOrder => 9999;

        public bool TryGetCustomOrder(Type type, out int order)
        {
            return Order.TryGetValue(type, out order);
        }

        private static readonly Dictionary<Type, int> Order = new()
        {
            //0 - 9 999 reserved for default systems
            
            //example
            //{typeof(DestroySystem), 99_998},
            
            //99999 reserved by morpeh
        };
    }
}