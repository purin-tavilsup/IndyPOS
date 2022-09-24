using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace IndyPOS.Tests.Attributes
{
	internal class AutoMoqDataAttribute : AutoDataAttribute
	{
		public AutoMoqDataAttribute() 
			: base(() => new Fixture().Customize(new AutoMoqCustomization()))
		{
		}
	}
}
