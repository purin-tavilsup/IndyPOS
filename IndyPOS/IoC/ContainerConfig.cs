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

namespace IndyPOS.IoC
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<Machine>()
                .As<IMachine>();

            builder.RegisterType<EventAggregator>()
                .As<IEventAggregator>();

            //builder.RegisterType<MainForm>()
            //    .AsSelf();

            //builder.RegisterType<SalePanel>()
            //    .AsSelf();

            //builder.RegisterType<InventoryPanel>()
            //    .AsSelf();

            //builder.RegisterType<UsersPanel>()
            //    .AsSelf();

            //builder.RegisterType<ReportsPanel>()
            //    .AsSelf();

            //builder.RegisterType<SettingsPanel>()
            //    .AsSelf();

            //builder.RegisterType<CustomerAccountsPanel>()
            //    .AsSelf();

            //builder.RegisterType<AddNewProductForm>()
            //    .AsSelf();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
                .Where(t => t.Namespace.Contains("UI"))
                .AsSelf();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
                .Where(t => t.Namespace.Contains("Controllers"))
                .As(t => t.GetInterface("I" + t.Name));

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS.DataServices"))
                .Where(t => t.Namespace.Contains("DataServices"))
                .As(t => t.GetInterface("I" + t.Name));

            return builder.Build();
        }
    }
}
