using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ModuleFramework.Interfaces;
using ModuleFramework.SystemsOrder;
using Scellecs.Morpeh;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace ModuleFramework
{
    public abstract class Module
    {
        private List<ValueTuple<SystemsGroup, int>> _systemsGroups = new(1);

        private EcsSystemsOrderResolver _orderResolver;

        private HashSet<IModuleLoadListener> _loadListeners;
        private HashSet<IModuleUnloadListener> _unloadListeners;
        private HashSet<IModuleActivationListener> _activationListeners;
        private HashSet<IModuleDeactivationListener> _deactivationListeners;
        private HashSet<ISystem> _ecsSystems;
        
        protected bool ActivateAfterLoad;
        protected LifetimeScope Scope;

        protected Module Parent { get; private set; }
        protected readonly List<Module> Children = new();

        public bool IsLoaded { get; private set; }
        public bool IsActive { get; private set; }
        
        public static async UniTask<TModule> Load<TModule>(Module parent, bool activateAfterLoad = false) 
            where TModule : Module, new()
        {
            var module = new TModule
            {
                Parent = parent
            };

            var hasParent = parent != null;

            if (hasParent)
                parent.Children.Add(module);
            
            await module.BeforeScopeCreate();

            var scope = hasParent 
                ? parent.Scope.CreateChild(module.InstallDependenciesToModule) 
                : LifetimeScope.Create(module.InstallDependenciesToModule);

            if (!hasParent)
                Object.DontDestroyOnLoad(scope);

            module.Scope = scope;
            module.ActivateAfterLoad = activateAfterLoad;

            scope.Container.Inject(module);

            await module.AfterScopeCreate();
            
            return module;
        }

        protected virtual async UniTask BeforeScopeCreate()
        {
            await UniTask.CompletedTask;
        }

        private async UniTask AfterScopeCreate()
        {
            _ecsSystems = Scope.Container.Resolve<IEnumerable<ISystem>>().ToHashSet();
            
            _loadListeners = Scope.Container.Resolve<IEnumerable<IModuleLoadListener>>().ToHashSet();
            _unloadListeners = Scope.Container.Resolve<IEnumerable<IModuleUnloadListener>>().ToHashSet();
            _activationListeners = Scope.Container.Resolve<IEnumerable<IModuleActivationListener>>().ToHashSet();
            _deactivationListeners = Scope.Container.Resolve<IEnumerable<IModuleDeactivationListener>>().ToHashSet();
            
            _orderResolver = Scope.Container.Resolve<EcsSystemsOrderResolver>();
            
            RemoveParentDependenciesRecursive(Parent, this);

            await OnLoad();

            foreach (var imp in _loadListeners)
                imp.OnModuleLoad();
            
            CreateSystemsGroup();

            IsLoaded = true;

            if (ActivateAfterLoad)
                await Activate();
        }

        private void CreateSystemsGroup()
        {
            SystemsGroup defaultSystemsGroup = null;
            var hasSystemsWithDefaultOrder = false;
            
            foreach (var system in _ecsSystems)
            {
                if (_orderResolver.TryGetCustomOrder(system.GetType(), out var order))
                {
                    var systemsGroup = World.Default.CreateSystemsGroup();
                    systemsGroup.AddSystem(system);
                    _systemsGroups.Add(new ValueTuple<SystemsGroup, int>(systemsGroup, order));
                    
                    continue;
                }
                
                if (hasSystemsWithDefaultOrder == false)
                {
                    hasSystemsWithDefaultOrder = true;
                    defaultSystemsGroup = World.Default.CreateSystemsGroup();
                }
                    
                defaultSystemsGroup.AddSystem(system);
            }

            if (hasSystemsWithDefaultOrder)
                _systemsGroups.Add(new ValueTuple<SystemsGroup, int>(defaultSystemsGroup, _orderResolver.GetDefaultOrder()));
        }

        public async UniTask Activate()
        {
            if (!IsLoaded)
            {
                ActivateAfterLoad = true;
                return;
            }
            
            await OnActivate();

            foreach (var imp in _activationListeners)
                imp.OnModuleActivate();
            
            foreach (var (systemsGroup, order) in _systemsGroups)
                World.Default.AddSystemsGroup(order, systemsGroup);
            
            IsActive = true;
        }

        public async UniTask Deactivate()
        {
            if (!IsLoaded)
                return;

            if (!IsActive)
                return;
            
            foreach (var module in Children)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                module.Deactivate();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            while (Children.Any(module => module.IsActive))
                await UniTask.NextFrame();
            
            foreach (var imp in _deactivationListeners)
                imp.OnModuleDeactivate();
            
            await OnDeactivate();
            
            IsActive = false;
            
            foreach (var tuple in _systemsGroups)
                World.Default.RemoveSystemsGroup(tuple.Item1);
        }

        public async UniTask Unload()
        {
            if (!IsLoaded)
                return;
            
            if (IsActive)
                await Deactivate();

            for (var i = 0; i < Children.Count; i++)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Children[0].Unload();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            
            while (Children.Any())
                await UniTask.NextFrame();
            
            foreach (var imp in _unloadListeners)
                imp.OnModuleUnload();
            
            await OnUnload();

            Parent?.Children.Remove(this);
            IsLoaded = false;
            
            foreach (var (_, order) in _systemsGroups)
                _orderResolver.ReleaseOrder(order);
            
            _ecsSystems = null;
            _systemsGroups = null;
            
            _loadListeners = null;
            _unloadListeners = null;
            _activationListeners = null;
            _deactivationListeners = null;
            
            Scope.Dispose();
        }

        protected virtual void InstallDependenciesToModule(IContainerBuilder builder) { }

        private void RemoveParentDependenciesRecursive(Module nextParent, Module sourceModule)
        {
            if (nextParent == null)
                return;
            
            sourceModule._ecsSystems.ExceptWith(nextParent._ecsSystems);
            sourceModule._loadListeners.ExceptWith(nextParent._loadListeners);
            sourceModule._unloadListeners.ExceptWith(nextParent._unloadListeners);
            sourceModule._activationListeners.ExceptWith(nextParent._activationListeners);
            sourceModule._deactivationListeners.ExceptWith(nextParent._deactivationListeners);
            
            RemoveParentDependenciesRecursive(nextParent.Parent, sourceModule);
        }
        
        protected virtual async UniTask OnLoad()
        {
            await UniTask.CompletedTask;
        }

        protected virtual async UniTask OnActivate()
        {
            await UniTask.CompletedTask;
        }
        
        protected virtual async UniTask OnDeactivate()
        {
            await UniTask.CompletedTask;
        }
        
        protected virtual async UniTask OnUnload()
        {
            await UniTask.CompletedTask;
        }
    }
}