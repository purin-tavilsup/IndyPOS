using System.Data;

namespace IndyPOS.Application.Common.Interfaces;

public interface IDbConnectionProvider
{
	IDbConnection GetDbConnection();

	void BackupDatabase(string backupDatabaseDirectory);
}