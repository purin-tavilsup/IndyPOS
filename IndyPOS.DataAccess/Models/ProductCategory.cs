using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models
{
	[ExcludeFromCodeCoverage]
    public class ProductCategory
    {
        public int Id { get; set; }

        public string Category { get; set; }
    }
}
