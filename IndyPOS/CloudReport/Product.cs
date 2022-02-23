namespace IndyPOS.CloudReport
{
    internal class Product
    {
        public int Id { get; set; }

        public string Barcode { get; set; }

        public int Category { get; set; }

        public decimal Total { get; set; }
    }
}
