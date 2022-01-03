using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Users;

namespace IndyPOS.Controllers
{
    internal class ReportController
	{
		private readonly IUserAccountHelper _accountHelper;
		private readonly IInvoiceRepository _invoicesRepository;
		private readonly IInventoryProductRepository _inventoryProductsRepository;

        public ReportController(IUserAccountHelper accountHelper,
								IInvoiceRepository invoicesRepository,
                                IInventoryProductRepository inventoryProductsRepository)
		{
			_accountHelper = accountHelper;
			_invoicesRepository = invoicesRepository;
			_inventoryProductsRepository = inventoryProductsRepository;
        }
    }
}
