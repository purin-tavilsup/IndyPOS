namespace IndyPOS.Application.UserCredentials;

public record UserCredentialDto(
	int UserId,
	string Username,
	string Password,
	string DateCreated,
	string DateUpdated);