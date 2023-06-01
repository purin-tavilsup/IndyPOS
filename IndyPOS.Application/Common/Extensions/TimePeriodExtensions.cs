using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Extensions;

public static class TimePeriodExtensions
{
	public static IDateRange ToDateRange(this TimePeriod timePeriod)
	{
		DateOnly startDate;
		DateOnly endDate; 

		switch (timePeriod)
		{
			case TimePeriod.ThisMonth:
				startDate = DateTime.Today.FirstDayOfMonth().ToDateOnly();
				endDate = DateTime.Today.LastDayOfMonth().ToDateOnly();
				break;

			case TimePeriod.ThisYear:
				startDate = DateTime.Today.FirstDayOfYear().ToDateOnly();
				endDate = DateTime.Today.LastDayOfYear().ToDateOnly();
				break;

			case TimePeriod.Today:
			default:
				startDate = DateTime.Today.ToDateOnly();
				endDate = DateTime.Today.ToDateOnly();
				break;
		}

		return new DateRange(startDate, endDate);
	}

	private class DateRange : IDateRange
	{
		public DateRange(DateOnly startDate, DateOnly endDate)
		{
			StartDate = startDate;
			EndDate = endDate;
		}

		public DateOnly StartDate { get; }
		public DateOnly EndDate { get; }
	}
}