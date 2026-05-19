namespace SCGLolTimerBot.Helper;

public static class CalendarWeekHelper
{
    public static (DateTime MinDate, DateTime MaxDate) GetCalendarWeek(int year, int calendarWeek)
    {
        // ISO-8601 Kalender verwenden
        DateTime firstThursday = new DateTime(year, 1, 4);

        // Montag der ersten ISO-Woche bestimmen
        int diff = DayOfWeek.Monday - firstThursday.DayOfWeek;
        if (diff > 0)
            diff -= 7;

        DateTime firstMonday = firstThursday.AddDays(diff);

        // Montag der gewünschten KW
        DateTime minDate = firstMonday.AddDays((calendarWeek - 1) * 7);

        // Sonntag der gewünschten KW
        DateTime maxDate = minDate.AddDays(6);

        return (minDate, maxDate);
    }
}