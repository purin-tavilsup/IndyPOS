using System.Collections.Generic;

namespace IndyPOS.Common.Interfaces
{
    public interface IStoreConstants
    {
        IReadOnlyDictionary<int, string> UserRoles { get; }

        IReadOnlyDictionary<int, string> PaymentTypes { get; }

        IReadOnlyDictionary<int, string> ProductCategories { get; }
    }
}
