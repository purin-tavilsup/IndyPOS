using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace IndyPOS.Application.Tests.Mocks.Attributes;

public class AutoMoqDataAttribute : AutoDataAttribute
{
	public AutoMoqDataAttribute() 
		: base(() => new Fixture().Customize(new AutoMoqCustomization()))
	{
	}
}