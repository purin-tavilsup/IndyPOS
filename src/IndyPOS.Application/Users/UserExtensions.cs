using IndyPOS.Application.Users.Commands.CreateUser;
using IndyPOS.Application.Users.Commands.UpdateUser;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Users;

internal static class UserExtensions
{
	internal static UserDto ToDto(this UserAccount entity)
	{
		var dto = new UserDto
		{
			UserId = entity.UserId,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			RoleId = entity.RoleId,
			DateCreated = entity.DateCreated,
			DateUpdated = entity.DateUpdated
		};

		return dto;
	}

	internal static UserAccount ToEntity(this CreateUserCommand command)
	{
		var entity = new UserAccount
		{
			FirstName = command.FirstName,
			LastName = command.LastName,
			RoleId = command.RoleId
		};

		return entity;
	}

	internal static UserAccount ToEntity(this UpdateUserCommand command)
	{
		var entity = new UserAccount
		{
			UserId = command.UserId,
			FirstName = command.FirstName,
			LastName = command.LastName
		};

		return entity;
	}
}