using System;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models
{
    [ExcludeFromCodeCoverage]
	public class Invoice
	{
		public string Id { get; set; }
		public string Date { get; set; }
		public DateTime DateTime { get; set; }
		public Product[] Products { get; set; }
		public Payment[] Payments { get; set; }
	}
}
