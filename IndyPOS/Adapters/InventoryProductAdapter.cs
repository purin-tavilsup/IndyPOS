﻿using IndyPOS.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Extensions;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.Adapters
{
    public class InventoryProductAdapter : IInventoryProduct
    {
        private InventoryProductModel _adaptee;

        public InventoryProductAdapter(InventoryProductModel adaptee)
        {
            _adaptee = adaptee;
        }

        public int InventoryProductId
        {
            get => _adaptee.InventoryProductId;
        }

        public string SKU
        {
            get => _adaptee.SKU;
            set => _adaptee.SKU = value;
        }

        public string Barcode
        {
            get => _adaptee.Barcode;
            set => _adaptee.Barcode = value;
        }

        public string Description
        {
            get => _adaptee.Description;
            set => _adaptee.Description = value;
        }

        public string Manufacturer
        {
            get => _adaptee.Manufacturer;
            set => _adaptee.Manufacturer = value;
        }

        public string Brand
        {
            get => _adaptee.Brand;
            set => _adaptee.Brand = value;
        }

        public int Category
        {
            get => _adaptee.Category;
            set => _adaptee.Category = value;
        }

        public decimal? UnitCost
        {
            get
            {
                if (!_adaptee.UnitCost.HasValue())
                    return null;

                if (decimal.TryParse(_adaptee.UnitCost.Trim(), out var value))
                    return value / 100m;

                return null;
            }

            set
            {
                _adaptee.UnitCost = value.HasValue ? (value * 100).ToString() : null;
            }
        }

        public decimal UnitPrice
        {
            get
            {
                if (decimal.TryParse(_adaptee.UnitPrice.Trim(), out var value))
                    return value / 100m;

                return 0m;
            }

            set
            {
                _adaptee.UnitPrice = (value * 100).ToString();
            }
        }

        public int QuantityInStock
        {
            get => _adaptee.QuantityInStock;
            set => _adaptee.QuantityInStock = value;
        }

        public string DateCreated
        {
            get => _adaptee.DateCreated;
        }

        public string DateUpdated
        {
            get => _adaptee.DateUpdated;
        }
    }
}
