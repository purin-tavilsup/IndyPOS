using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using IndyPOS.Application.Tests.Mocks.Customizations;

namespace IndyPOS.Application.Tests.Mocks.Attributes;

public class CustomAutoDataAttribute : AutoDataAttribute
{
	public CustomAutoDataAttribute()
		: base(() => new Fixture().Customize(new CompositeCustomization(new AutoMoqCustomization(),
																        new MapperCustomization())))
	{
	}
}