using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

namespace ModuleFramework
{
    public abstract class ModuleWithSettings<TSettings> : Module
        where TSettings : ScriptableObject
    {
        protected const string DefaultDependenciesFolder = "Assets/Developing/ModuleSettings";

        protected virtual string AddressableSettingsPath =>
            $"{DefaultDependenciesFolder}/{typeof(TSettings).Name}.asset";

        protected AsyncOperationHandle<TSettings> AssetsDependenceHandle { get; private set; }
        protected TSettings Settings { get; private set; }

        protected virtual void InstallDependencies(IContainerBuilder builder)
        {
        }

        protected sealed override async UniTask BeforeScopeCreate()
        {
            await LoadAddressable();
        }

        protected sealed override void InstallDependenciesToModule(IContainerBuilder builder)
        {
            builder.RegisterInstance(Settings);

            InstallDependencies(builder);
        }

        private async UniTask LoadAddressable()
        {
            AssetsDependenceHandle = Addressables.LoadAssetAsync<TSettings>(AddressableSettingsPath);

            await AssetsDependenceHandle.Task;

            Settings = AssetsDependenceHandle.Result;
        }

        protected override UniTask OnUnload()
        {
            if (AssetsDependenceHandle.IsValid())
                Addressables.Release(AssetsDependenceHandle);
            
            return UniTask.CompletedTask;
        }
    }
}