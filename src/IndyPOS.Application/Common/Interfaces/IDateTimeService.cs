namespace IndyPOS.Application.Common.Interfaces;

public interface IDateTimeService
{
	DateTime Now { get; }

	DateOnly GetFirstDayOfWeek();

	DateOnly GetLastDayOfWeek();

	DateOnly GetFirstDayOfMonth();

	DateOnly GetLastDayOfMonth();

	DateOnly GetFirstDayOfYear();

	DateOnly GetLastDayOfYear();

	string ConvertToStartDateString(DateTime date);

	string ConvertToEndDateString(DateTime date);

	string ConvertToStartDateString(DateOnly date);

	string ConvertToEndDateString(DateOnly date);
}