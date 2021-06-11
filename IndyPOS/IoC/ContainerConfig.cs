using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using IndyPOS.Events;
using IndyPOS.DataServices;
using Prism.Events;
using System.Reflection;
using IndyPOS.UI;
using IndyPOS.Constants;

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

            //builder.RegisterType<SQLiteDatabase>()
            //    .AsSelf()
            //    .SingleInstance();

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

            //builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS.DataServices"))
            //    .Except<SQLiteDatabase>()
            //    .Where(t => t.Namespace.Contains("IndyPOS.DataServices"))
            //    .As(t => t.GetInterfaces().FirstOrDefault(i => i.Name == "I" + t.Name));

            return builder.Build();
        }
    }
}
