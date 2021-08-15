﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.DataAccess.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }

        public decimal Total { get; set; }

        public int? CustomerId { get; set; }

        public int UserId { get; set; }

        public string DateCreated { get; set; }
    }
}