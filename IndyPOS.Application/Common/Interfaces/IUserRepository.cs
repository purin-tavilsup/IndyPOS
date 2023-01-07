using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IUserRepository
{
	int Add(UserAccount user);

	bool Update(UserAccount user);

	UserAccount? GetById(int id);

	IEnumerable<UserAccount> GetAll();

	bool RemoveById(int id);
}