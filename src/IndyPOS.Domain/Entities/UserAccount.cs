using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserAccount
{
	public int UserId { get; set; }
        
	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public int RoleId { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;
}