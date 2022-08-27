﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models
{
    [ExcludeFromCodeCoverage]
    public class SalesReport
    {
		public string Id { get; set; }
		public SalesSummary DaySummary { get; set; }
		public SalesSummary MonthSummary { get; set; }
		public SalesSummary YearSummary { get; set; }
        public DateTime LastUpdateDateTime { get; set; }
    }
}
