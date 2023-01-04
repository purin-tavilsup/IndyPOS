using System.Data;

namespace IndyPOS.DataAccess.Interfaces;

public interface IDbConnectionProvider
{
	IDbConnection GetDbConnection();

	void BackupDatabase(string backupDatabaseDirectory);
}