using IndyPOS.Application.UserCredentials.Commands.CreateUserCredential;
using IndyPOS.Application.UserCredentials.Commands.UpdateUserCredential;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UserCredentials;

internal static class UserCredentialExtensions
{
	internal static UserCredentialDto ToDto(this UserCredential entity)
	{
		var dto = new UserCredentialDto
		{
			UserId = entity.UserId,
			Username = entity.Username,
			Password = entity.Password,
			DateCreated = entity.DateCreated,
			DateUpdated = entity.DateUpdated
		};

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