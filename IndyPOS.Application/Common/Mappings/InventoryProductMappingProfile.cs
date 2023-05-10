using AutoMapper;
using IndyPOS.Application.InventoryProducts;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Mappings;

public class InventoryProductMappingProfile : Profile
{
	public InventoryProductMappingProfile()
	{
		CreateMap<InventoryProduct, InventoryProductDto>()
			.ReverseMap();
	}
}