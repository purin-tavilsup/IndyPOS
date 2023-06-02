using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserRole
{
	public int Id { get; set; }

	public string Role { get; set; } = string.Empty;
}