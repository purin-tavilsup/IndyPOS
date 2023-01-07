using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IUserCredentialRepository
{
	bool Add(int userId, string username, string password);

	UserCredential? GetById(int id);

	UserCredential? GetByUsername(string username);

	bool UpdatePasswordById(int userId, string password);

	bool RemoveById(int userId);
}