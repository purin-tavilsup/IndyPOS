using System.Data;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IDbConnectionProvider
{
	IDbConnection GetDbConnection();

	void BackupDatabase(string backupDatabaseDirectory);
}