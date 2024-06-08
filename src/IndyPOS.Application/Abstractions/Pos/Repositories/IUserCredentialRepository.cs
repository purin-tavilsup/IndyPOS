using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IUserCredentialRepository
{
	bool Add(UserCredential userCredential);

	UserCredential GetById(int id);

	UserCredential GetByUsername(string username);

	bool UpdatePassword(UserCredential userCredential);

	bool RemoveById(int userId);
}