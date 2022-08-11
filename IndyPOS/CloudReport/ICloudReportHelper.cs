using System.Threading.Tasks;

namespace IndyPOS.CloudReport
{
    public interface ICloudReportHelper
	{
		Task PublishSaleReport(int invoiceId);
	}
}
