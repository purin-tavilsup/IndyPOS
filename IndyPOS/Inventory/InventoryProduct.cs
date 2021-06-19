﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Adapters;

namespace IndyPOS.Inventory
{
    public class InventoryProduct : IInventoryProduct
    {
        public int InventoryProductId { get; }

        public string SKU { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Brand { get; set; }

        public int Category { get; set; }

        public decimal? UnitCost { get; set; }

        public decimal UnitPrice { get; set; }

        public int QuantityInStock { get; set; }

        public string Comment { get; set; }

        public int Revision { get; }

        public string DateCreated { get; }

        public string DateUpdated { get; }
    }
}