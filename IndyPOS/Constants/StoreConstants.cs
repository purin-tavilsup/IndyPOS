using IndyPOS.DataAccess.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Constants
{
    public class StoreConstants : IStoreConstants
    {
        private readonly IStoreConstantRepository _storeConstantsRepository;

        public StoreConstants(IStoreConstantRepository storeConstantsRepository)
        {
            _storeConstantsRepository = storeConstantsRepository;

            UserRoles = _storeConstantsRepository.GetUserRoles().ToDictionary(x => x.Id, x => x.Role);

            PaymentTypes = _storeConstantsRepository.GetPaymentTypes().ToDictionary(x => x.Id, x => x.Type);

            ProductCategories = _storeConstantsRepository.GetProductCategories().ToDictionary(x => x.Id, x => x.Category);
        }

        public IReadOnlyDictionary<int, string> UserRoles { get; }

        public IReadOnlyDictionary<int, string> PaymentTypes { get; }

        public IReadOnlyDictionary<int, string> ProductCategories { get; }
    }
}
