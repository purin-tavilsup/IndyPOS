namespace IndyPOS.Application.UserCredentials;

public class UserCredentialDto
{
	public int UserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;
}