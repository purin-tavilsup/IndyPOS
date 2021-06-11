using IndyPOS.DataServices;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Constants
{
    public class StoreConstants : IStoreConstants
    {
        private readonly IStoreConstantsDataService _storeConstantsDataService;

        public StoreConstants(IStoreConstantsDataService storeConstantsDataService)
        {
            _storeConstantsDataService = storeConstantsDataService;

            UserRoles = _storeConstantsDataService.GetUserRoles().ToDictionary(x => x.Id, x => x.Role);

            PaymentTypes = _storeConstantsDataService.GetPaymentTypes().ToDictionary(x => x.Id, x => x.PaymentType);

            ProductCategories = _storeConstantsDataService.GetProductCategories().ToDictionary(x => x.Id, x => x.ProductCategory);
        }

        public IReadOnlyDictionary<int, string> UserRoles { get; }

        public IReadOnlyDictionary<int, string> PaymentTypes { get; }

        public IReadOnlyDictionary<int, string> ProductCategories { get; }
    }
}
