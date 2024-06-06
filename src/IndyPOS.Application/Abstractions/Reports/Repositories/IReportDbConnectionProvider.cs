using System.Data;

namespace IndyPOS.Application.Abstractions.Reports.Repositories;

public interface IReportDbConnectionProvider
{
	IDbConnection CreateConnection();
}