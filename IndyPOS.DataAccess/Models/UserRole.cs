using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models;

[ExcludeFromCodeCoverage]
public class UserRole
{
	public int Id { get; set; }

	public string Role { get; set; }
}