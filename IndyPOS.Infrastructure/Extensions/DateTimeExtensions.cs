namespace IndyPOS.Infrastructure.Extensions;

internal static class DateTimeExtensions
{
	internal static string ToStartDateString(this DateOnly date)
	{
		return $"{date:yyyy-MM-dd} 00:00";
	}

	internal static string ToEndDateString(this DateOnly date)
	{
		return $"{date:yyyy-MM-dd} 24:00";
	}
}