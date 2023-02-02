using AutoFixture;
using AutoMapper;
using IndyPOS.Application.Common.Mappings;

namespace IndyPOS.Application.Tests.Mocks.Customizations;

public class MapperCustomization : ICustomization
{
	public void Customize(IFixture fixture)
	{
		var mapperConfig = new MapperConfiguration(x =>
		{
			x.AddProfile(new InventoryProductMappingProfile());
		});

		var mapper = mapperConfig.CreateMapper();

		fixture.Inject(mapper);
	}
}