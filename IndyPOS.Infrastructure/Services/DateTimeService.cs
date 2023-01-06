using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
	public DateTime Now => DateTime.Now;

	private static DateOnly GetToday()
	{
		return DateOnly.FromDateTime(DateTime.Today);
	}

	public DateOnly GetFirstDayOfWeek()
	{
		var today = GetToday();
		var culture = Thread.CurrentThread.CurrentCulture;
		var diff = today.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;


		if (diff < 0)
			diff += 7;

		return today.AddDays(-diff);
	}

	public DateOnly GetLastDayOfWeek()
	{
		var firstDayOfWeek = GetFirstDayOfWeek();

		return firstDayOfWeek.AddDays(6);
	}

	public DateOnly GetFirstDayOfMonth()
	{
		var today = GetToday();

		return new DateOnly(today.Year, today.Month, 1);
	}

	public DateOnly GetLastDayOfMonth()
	{
		var firstDayOfMonth = GetFirstDayOfMonth();

		return firstDayOfMonth.AddMonths(1).AddDays(-1);
	}

	public DateOnly GetFirstDayOfYear()
	{
		var today = GetToday();

		return new DateOnly(today.Year, 1, 1);
	}

	public DateOnly GetLastDayOfYear()
	{
		var today = GetToday();

		return new DateOnly(today.Year, 12, 31);
	}

	public string ConvertToStartDateString(DateOnly date)
	{
		return $"{date:yyyy-MM-dd} 00:00";
	}

	public string ConvertToStartDateString(DateTime date)
	{
		return $"{date:yyyy-MM-dd} 00:00";
	}

	public string ConvertToEndDateString(DateOnly date)
	{
		return $"{date:yyyy-MM-dd} 24:00";
	}

	public string ConvertToEndDateString(DateTime date)
	{
		return $"{date:yyyy-MM-dd} 24:00";
	}
}