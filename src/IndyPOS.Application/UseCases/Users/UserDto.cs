﻿namespace IndyPOS.Application.UseCases.Users;

public record UserDto(int UserId, 
					  string FirstName,
					  string LastName,
					  int RoleId,
					  string DateCreated,
					  string DateUpdated);