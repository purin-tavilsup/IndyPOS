using System.Globalization;

namespace IndyPOS.DataAccess.Extensions;

internal static class CurrencyMappingExtensions
{
	internal static decimal? ToNullableMoney(this string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		if (decimal.TryParse(value.Trim(), out var result))
			return result / 100m;

		return null;
	}

	internal static decimal ToMoney(this string value)
	{
		if (decimal.TryParse(value.Trim(), out var result))
			return result / 100m;

		return 0m;
	}

	internal static string? ToNullableMoneyString(this decimal? money)
	{
		if (money is null)
			return null;

		var result = Math.Round(money.Value, 2, MidpointRounding.AwayFromZero) * 100m;

		return result.ToString(CultureInfo.InvariantCulture);
	}

	internal static string ToMoneyString(this decimal money)
	{
		var result = Math.Round(money, 2, MidpointRounding.AwayFromZero) * 100m;

		return result.ToString(CultureInfo.InvariantCulture);
	}
}