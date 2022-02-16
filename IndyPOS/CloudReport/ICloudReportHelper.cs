using System.Threading.Tasks;

namespace IndyPOS.CloudReport
{
    public interface ICloudReportHelper
	{
		Task PublishToCloud();
	}
}
