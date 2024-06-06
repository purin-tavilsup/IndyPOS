namespace IndyPOS.Application.Common.Models;

public class SalesReport : SalesSummary
{
	public Guid Id { get; set; }
	
	public DateTime Created { get; set; }
	
	public int ReferenceId { get; set; }
}