﻿using IndyPOS.Application.UseCases.Users.Create;
using IndyPOS.Application.UseCases.Users.Update;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.UseCases.Users;

internal static class UserExtensions
{
	internal static UserDto ToDto(this UserAccount entity)
	{
		var dto = new UserDto(entity.UserId, 
							  entity.FirstName, 
							  entity.LastName, 
							  entity.RoleId, 
							  entity.DateCreated, 
							  entity.DateUpdated);
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