using IndyPOS.Facade.Models;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Interfaces
{
    public interface IReportHelper
	{
		Task<SalesReport> UpdateReport(SalesSummary summary);
	}
}
