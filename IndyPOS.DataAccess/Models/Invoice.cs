using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models
{
    [ExcludeFromCodeCoverage]
    public class Invoice
    {
        public int InvoiceId { get; set; }

        public decimal Total { get; set; }

        public int? CustomerId { get; set; }

        public int UserId { get; set; }

        public string DateCreated { get; set; }
    }
}
