using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class ProductCategory
{
	public int Id { get; set; }

	public string Category { get; set; } = string.Empty;
}