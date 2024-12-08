using IndyPOS.Application.UseCases.UserCredentials.Create;
using IndyPOS.Application.UseCases.UserCredentials.Update;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UseCases.UserCredentials;

internal static class UserCredentialExtensions
{
	internal static UserCredentialDto ToDto(this UserCredential entity)
	{
		var dto = new UserCredentialDto(entity.UserId,
										entity.Username,
										entity.Password,
										entity.DateCreated,
										entity.DateUpdated);
		return dto;
	}

	internal static UserCredential ToEntity(this CreateUserCredentialCommand command)
	{
		var entity = new UserCredential
		{
			UserId = command.UserId,
			Username = command.Username,
			Password = command.Password
		};

		return entity;
	}

	internal static UserCredential ToEntity(this UpdateUserCredentialCommand command)
	{
		var entity = new UserCredential
		{
			UserId = command.UserId,
			Password = command.Password
		};

		return entity;
	}
}