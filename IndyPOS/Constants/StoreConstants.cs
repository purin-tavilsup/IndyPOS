using IndyPOS.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Interfaces;

namespace IndyPOS.Constants
{
    public class StoreConstants : IStoreConstants
    {
		public StoreConstants(IStoreConstantRepository storeConstantsRepository)
        {
            UserRoles = storeConstantsRepository.GetUserRoles().ToDictionary(x => x.Id, x => x.Role);

            PaymentTypes = storeConstantsRepository.GetPaymentTypes().ToDictionary(x => x.Id, x => x.Type);

            ProductCategories = storeConstantsRepository.GetProductCategories().ToDictionary(x => x.Id, x => x.Category);
        }

        public IReadOnlyDictionary<int, string> UserRoles { get; }

        public IReadOnlyDictionary<int, string> PaymentTypes { get; }

        public IReadOnlyDictionary<int, string> ProductCategories { get; }
    }
}
