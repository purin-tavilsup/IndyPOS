﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.DataAccess.Models
{
    public class InventoryProductModel
    {
        public int InventoryProductId { get; set; }

        public string SKU { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }

        public string Manufacturer { get; set; }

        public string Brand { get; set; }

        public int Category { get; set; }

        public string UnitCost { get; set; }

        public string UnitPrice { get; set; }

        public int QuantityInStock { get; set; }

        public string DateCreated { get; set; }

        public string DateUpdated { get; set; }
    }
}
