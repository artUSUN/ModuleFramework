using ModuleFramework.SystemsOrder;
using VContainer;

namespace ModuleFramework.Examples
{
    public static class ModuleFrameworkInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<EcsSystemsOrderResolver>(Lifetime.Singleton);
            builder.Register<EcsSystemsOrderRepository>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}