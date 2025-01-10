using System;
using System.Collections.Concurrent;
using System.Threading;

static class Scheduler
{
    private static readonly ConcurrentDictionary<string, Timer> timers = new ConcurrentDictionary<string, Timer>();

    public static void ScheduleSwitch(string SW, string start, string end)
    {
        // Parse input time (format: HHmm, e.g., "0600")
        if (!TimeSpan.TryParseExact(start.Insert(2, ":"), "hh\\:mm", null, out TimeSpan startTime) ||
            !TimeSpan.TryParseExact(end.Insert(2, ":"), "hh\\:mm", null, out TimeSpan endTime))
        {
            Console.WriteLine("Invalid time format. Use HHmm (e.g., 0600).");
            return;
        }

        DateTime now = DateTime.Now;
        DateTime scheduledStart = now.Date + startTime; // now.date is today's date at 00:00:00
        DateTime scheduledEnd = now.Date + endTime;

        if (scheduledStart <= now)
        {
            scheduledStart = scheduledStart.AddDays(1);
        }

        if (scheduledEnd <= now)
        {
            scheduledEnd = scheduledEnd.AddDays(1);
        }

        if (scheduledEnd <= scheduledStart)
        {
            scheduledEnd = scheduledEnd.AddDays(1);
        }

        Console.WriteLine(SW);
        Console.WriteLine(scheduledStart);
        Console.WriteLine(scheduledEnd);
    }

    public static void CancelTask(string id)
    {
        if (timers.TryRemove(id, out Timer timer))
        {
            timer.Dispose();
            Console.WriteLine($"Task '{id}' canceled.");
        }
        else
        {
            Console.WriteLine($"No task found with ID '{id}'.");
        }
    }

    private static void ExecuteTask(string id)
    {
        
    }
}