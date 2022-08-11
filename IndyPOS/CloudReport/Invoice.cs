using System.Collections.Generic;

namespace IndyPOS.CloudReport
{
    internal class Invoice
    {
        public int InvoiceId { get; set; }

        public IEnumerable<Product> Products { get; set; }

        public IEnumerable<Payment> Payment { get; set; }

        public string DateCreated { get; set; }
    }
}
