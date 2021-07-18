using Autofac;
using IndyPOS.Constants;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Devices;
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

            builder.RegisterType<InvoiceRepository>()
                .As<IInvoiceRepository>();

            builder.RegisterType<InventoryProductRepository>()
                .As<IInventoryProductRepository>();

            builder.RegisterType<StoreConstantRepository>()
                .As<IStoreConstantRepository>();

            builder.RegisterType<CustomerRepository>()
                .As<ICustomerRepository>();

            builder.RegisterType<UserRepository>()
                .As<IUserRepository>();



            builder.RegisterType<BarcodeScanner>()
                .As<IBarcodeScanner>()
                .SingleInstance();

            return builder.Build();
        }
    }
}
