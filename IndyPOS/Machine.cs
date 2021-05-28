﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using IndyPOS.DataServices;

namespace IndyPOS
{
    public class Machine
    {
        private static Machine _instance;
        private static StoreConstants _storeConstants;
        private MainForm _mainForm;
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;

        public static Machine Instance => 
            _instance ?? (_instance = new Machine());

        public static StoreConstants StoreConstants => 
            _storeConstants ?? (_storeConstants = new StoreConstants(new StoreConstantsDataService()));
        
        public Machine()
        {
            _eventAggregator = new Prism.Events.EventAggregator();
            _invoicesDataService = new InvoicesDataService();
        }

        public void Startup()
        {
            try
            {

                var constants = Machine.StoreConstants;

                //LoadConfig();

                //Thread.Sleep(500);

                //CreateHardwareControllers();
                //CreateProcesses();

                //Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                //throw ex;
            }
        }

        public void Shutdown()
        {
            //
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

            _mainForm = new MainForm(_eventAggregator, _invoicesDataService);

            System.Windows.Forms.Application.Run(_mainForm);

            _mainForm.BringToFront();
        }
    }
}
