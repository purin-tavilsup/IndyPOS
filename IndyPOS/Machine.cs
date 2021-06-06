using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using IndyPOS.DataServices;
using IndyPOS.Constants;

namespace IndyPOS
{
    public class Machine
    {
        private static Machine _instance;
        private static StoreConstants _storeConstants;
        private MainForm _mainForm;
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;

        public static Machine Instance => 
            _instance ?? (_instance = new Machine());

        public static StoreConstants StoreConstants => 
            _storeConstants ?? (_storeConstants = new StoreConstants(new StoreConstantsDataService()));
        
        public Machine()
        {
            _eventAggregator = new Prism.Events.EventAggregator();
            _invoicesDataService = new InvoicesDataService();
            _inventoryProductsDataService = new InventoryProductsDataService();
        }

        public void Startup()
        {
            try
            {
                var constants = Machine.StoreConstants;

                // TODO: Implement Dependency Injection here. CastleWindsor or AutoFac
            }
            catch (Exception ex)
            {
                // TODO: Handle ex;
            }
        }

        public void Shutdown()
        {
            // TODO: Dispose resources
        }

        public void Launch()
        {
            Startup();
            CreateUI();
            Shutdown();
        }

        private void CreateUI()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // TODO: this should be handled by Dependency Injection
            _mainForm = new MainForm(_eventAggregator, _invoicesDataService, _inventoryProductsDataService);

            System.Windows.Forms.Application.Run(_mainForm);

            _mainForm.BringToFront();
        }
    }
}
