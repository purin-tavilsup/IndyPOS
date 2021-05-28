﻿using IndyPOS.Adapters;
using IndyPOS.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS
{
    public class StoreConstants
    {
        private readonly IStoreConstantsDataService _storeConstantsDataService;

        public StoreConstants(IStoreConstantsDataService storeConstantsDataService)
        {
            _storeConstantsDataService = storeConstantsDataService;

            LoadConstants();
        }

        public void LoadConstants()
        {
            UserRoles = _storeConstantsDataService.GetUserRoles().ToDictionary(x => x.Id, x => x.Role);

            PaymentTypes = _storeConstantsDataService.GetPaymentTypes().ToDictionary(x => x.Id, x => x.PaymentType);

            ProductCategories = _storeConstantsDataService.GetProductCategories().ToDictionary(x => x.Id, x => x.ProductCategory);
        }

        public IReadOnlyDictionary<int, string> UserRoles { get; private set; }

        public IReadOnlyDictionary<int, string> PaymentTypes { get; private set; }

        public IReadOnlyDictionary<int, string> ProductCategories { get; private set; }
    }
}
