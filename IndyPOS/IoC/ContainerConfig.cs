using Autofac;
using IndyPOS.Barcode;
using IndyPOS.Common.Interfaces;
using IndyPOS.Common.Utilities;
using IndyPOS.Constants;
using IndyPOS.Cryptography;
using IndyPOS.DataAccess;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.DataAccess.SQLite.Repositories;
using IndyPOS.Devices;
using IndyPOS.Facade.Helpers;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Mappers;
using IndyPOS.Sales;
using IndyPOS.Users;
using LazyCache;
using Prism.Events;
using Serilog;
using Serilog.Formatting.Json;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Configuration = IndyPOS.Configurations.Configuration;

namespace IndyPOS.IoC
{
	public static class ContainerConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

			ConfigureLogger(builder);

			builder.RegisterType<JsonUtility>()
				   .As<IJsonUtility>()
				   .SingleInstance();
			
			builder.RegisterType<Configuration>()
				   .As<IConfiguration>()
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

			builder.RegisterType<CachingService>()
				   .As<IAppCache>()
				   .SingleInstance();

			builder.RegisterType<ReportHelper>()
				   .As<IReportHelper>()
				   .SingleInstance();

			builder.RegisterType<SaleInvoiceMapper>()
				   .As<ISaleInvoiceMapper>()
				   .SingleInstance();

			builder.RegisterType<HttpClient>()
				   .As<HttpClient>()
				   .SingleInstance();

			builder.RegisterType<DataFeedApiHelper>()
				   .As<IDataFeedApiHelper>()
				   .SingleInstance();

			return builder.Build();
        }

		private static void ConfigureLogger(ContainerBuilder builder)
		{
			var logDirectory = ConfigurationManager.AppSettings.Get("LogDirectory");

			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}

			var logFilePath = $"{logDirectory}\\log.json";
			var logger = new LoggerConfiguration().MinimumLevel.Debug()
												  .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
												  .CreateLogger();

			builder.Register<ILogger>((c, p) => logger);
		}
    }
}
