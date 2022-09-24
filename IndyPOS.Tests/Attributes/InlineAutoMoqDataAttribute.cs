using AutoFixture.Xunit2;
using Xunit;

namespace IndyPOS.Tests.Attributes
{
	internal class InlineAutoMoqDataAttribute : CompositeDataAttribute
	{
		public InlineAutoMoqDataAttribute(params object[] values)
			: base(new InlineDataAttribute(values), new AutoMoqDataAttribute())
		{
		}
	}
}
