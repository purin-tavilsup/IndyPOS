namespace IndyPOS.DataAccess.Extensions;

internal static class DateTimeExtensions
{
	internal static string ToStartDateString(this DateTime date)
	{
		return $"{date:yyyy-MM-dd} 00:00";
	}

	internal static string ToEndDateString(this DateTime date)
	{
		return $"{date:yyyy-MM-dd} 24:00";
	}
}