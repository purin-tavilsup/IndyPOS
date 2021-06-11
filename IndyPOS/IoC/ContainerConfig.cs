using Autofac;
using IndyPOS.Constants;
using IndyPOS.DataServices;
using Prism.Events;
using System.Linq;
using System.Reflection;

namespace IndyPOS.IoC
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<Machine>()
                .As<IMachine>()
                .SingleInstance();

            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>()
                .SingleInstance();

            builder.RegisterType<StoreConstants>()
                .As<IStoreConstants>()
                .SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
                .Where(t => t.Namespace.Contains("UI"))
                .AsSelf()
                .SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
                .Where(t => t.Namespace.Contains("Controllers"))
                .As(t => t.GetInterface("I" + t.Name))
                .SingleInstance();

            builder.RegisterType<InvoicesDataService>()
                .As<IInvoicesDataService>();

            builder.RegisterType<InventoryProductsDataService>()
                .As<IInventoryProductsDataService>();

            builder.RegisterType<StoreConstantsDataService>()
                .As<IStoreConstantsDataService>();

            builder.RegisterType<CustomersDataService>()
                .As<ICustomersDataService>();

            builder.RegisterType<UsersDataService>()
                .As<IUsersDataService>();

            return builder.Build();
        }
    }
}
