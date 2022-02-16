using Autofac;
using IndyPOS.Constants;
using IndyPOS.Cryptography;
using IndyPOS.DataAccess;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.DataAccess.SQLite.Repositories;
using IndyPOS.Devices;
using IndyPOS.Users;
using Prism.Events;
using System.Reflection;
using IndyPOS.Barcode;
using IndyPOS.CloudReport;
using IndyPOS.Mqtt;
using IndyPOS.Sales;

namespace IndyPOS.IoC
{
    public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

			builder.RegisterType<Config>()
				   .As<IConfig>()
				   .SingleInstance();

            builder.RegisterType<Machine>()
				   .As<IMachine>()
				   .SingleInstance();

            builder.RegisterType<EventAggregator>()
				   .As<IEventAggregator>()
				   .SingleInstance();

            builder.RegisterType<StoreConstants>()
				   .As<IStoreConstants>()
				   .SingleInstance();

			builder.RegisterType<SaleInvoice>()
				   .As<ISaleInvoice>()
				   .SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
				   .Where(t => t.Namespace?.Contains("UI") ?? false)
				   .AsSelf()
				   .SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.Load("IndyPOS"))
				   .Where(t => t.Namespace?.Contains("Controllers") ?? false)
				   .As(t => t.GetInterface("I" + t.Name))
				   .SingleInstance();

            builder.RegisterType<DbConnectionProvider>()
				   .As<IDbConnectionProvider>()
				   .SingleInstance();

            builder.RegisterType<InvoiceRepository>()
				   .As<IInvoiceRepository>();

            builder.RegisterType<InventoryProductRepository>()
				   .As<IInventoryProductRepository>();

            builder.RegisterType<StoreConstantRepository>()
				   .As<IStoreConstantRepository>();

            builder.RegisterType<UserRepository>()
				   .As<IUserRepository>();

			builder.RegisterType<AccountsReceivableRepository>()
				   .As<IAccountsReceivableRepository>();

            builder.RegisterType<BarcodeScanner>()
				   .As<IBarcodeScanner>()
				   .SingleInstance();

			builder.RegisterType<ReceiptPrinter>()
				   .As<IReceiptPrinter>()
				   .SingleInstance();

			builder.RegisterType<CryptographyHelper>()
				   .As<ICryptographyHelper>()
				   .SingleInstance();

            builder.RegisterType<UserAccountHelper>()
                   .As<IUserAccountHelper>()
                   .SingleInstance();

			builder.RegisterType<BarcodeHelper>()
				   .As<IBarcodeHelper>()
				   .SingleInstance();

			builder.RegisterType<MqttClient>()
				   .As<IMqttClient>()
				   .SingleInstance();

			builder.RegisterType<CloudReportHelper>()
				   .As<ICloudReportHelper>()
				   .SingleInstance();

            return builder.Build();
        }
    }
}
