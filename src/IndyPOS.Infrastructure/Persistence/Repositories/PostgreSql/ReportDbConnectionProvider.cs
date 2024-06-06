using System.Data;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace IndyPOS.Infrastructure.Persistence.Repositories.PostgreSql;

public class ReportDbConnectionProvider : IReportDbConnectionProvider
{
	private readonly string _connectionString;
	
	public ReportDbConnectionProvider(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString("RungratPosDb") ?? string.Empty;
	}

	public IDbConnection CreateConnection()
	{
		return new NpgsqlConnection(_connectionString);
	}
}