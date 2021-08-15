using System.Data;

namespace IndyPOS.DataAccess
{
	public interface IDbConnectionProvider
	{
		IDbConnection GetDbConnection();
	}
}
