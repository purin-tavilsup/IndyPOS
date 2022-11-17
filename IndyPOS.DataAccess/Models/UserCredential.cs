using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models;

[ExcludeFromCodeCoverage]
public class UserCredential
{
	public int UserId { get; set; }

	public string Username { get; set; }

	public string Password { get; set; }

	public string DateCreated { get; set; }

	public string DateUpdated { get; set; }
}