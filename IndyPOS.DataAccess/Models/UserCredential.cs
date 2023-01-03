using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models;

[ExcludeFromCodeCoverage]
public class UserCredential
{
	public int UserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;
}