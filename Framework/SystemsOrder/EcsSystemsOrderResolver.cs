using System;
using System.Collections.Generic;

namespace ModuleFramework.SystemsOrder
{
    public class EcsSystemsOrderResolver
    {
        private readonly IEcsSystemsOrderRepository _ecsSystemsOrderRepository;
        
        private readonly HashSet<int> _occupiedOrders = new(100);
        private int _nextFreeOrder = 0;

        public EcsSystemsOrderResolver(IEcsSystemsOrderRepository ecsSystemsOrderRepository)
        {
            _ecsSystemsOrderRepository = ecsSystemsOrderRepository;
        }

        public void ReleaseOrder(int order)
        {
            _occupiedOrders.Remove(order);
        }

        public int GetDefaultOrder()
        {
            if (_nextFreeOrder <= _ecsSystemsOrderRepository.GetLastDefaultOrder)
            {
                var result = _nextFreeOrder;
                _nextFreeOrder++;
                
                _occupiedOrders.Add(result);

                return result;
            }

            for (var i = 0; i < _ecsSystemsOrderRepository.GetLastDefaultOrder; i++)
            {
                if (_occupiedOrders.Contains(i))
                    continue;
                
                _occupiedOrders.Add(i);
                return i;
            }

            throw new Exception($"[ModuleFramework] no unused indexes available. Max. count = {_ecsSystemsOrderRepository.GetLastDefaultOrder}. Increase the number of indexes");
        }

        public bool TryGetCustomOrder(Type type, out int order)
        {
            return _ecsSystemsOrderRepository.TryGetCustomOrder(type, out order);
        }
    }
}