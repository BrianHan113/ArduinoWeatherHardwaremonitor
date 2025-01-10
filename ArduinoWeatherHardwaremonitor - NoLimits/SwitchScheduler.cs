using System;
using System.Collections.Concurrent;
using System.Threading;
using SerialSender;


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

        TimeSpan delayToStart = scheduledStart - now;
        TimeSpan onDuration = scheduledEnd - scheduledStart;
        
        Timer timer = new Timer(_ =>
        {
            Console.WriteLine($"'{SW}' ON at {DateTime.Now:HH:mm:ss}"); 
            ContextMenus.EnqueueData("SCHEDULE" + SW + "ON" + 0x03);
            
            // Schedule the end timer
            Timer endTimer = new Timer(__ =>
            {
                Console.WriteLine($"'{SW}' OFF at {DateTime.Now:HH:mm:ss}");
                ContextMenus.EnqueueData("SCHEDULE" + SW + "OFF" + 0x03);

            }, null, onDuration, Timeout.InfiniteTimeSpan);

        }, null, delayToStart, Timeout.InfiniteTimeSpan);


        if (timers.ContainsKey(SW))
        {
            CancelTask(SW);
        }

        // Store the timer
        if (timers.TryAdd(SW, timer))
        {
            Console.WriteLine($"Task '{SW}' scheduled to start at {scheduledStart:HH:mm:ss} and end at {scheduledEnd:HH:mm:ss}");
        }
    }

    public static void CancelTask(string SW)
    {
        if (timers.TryRemove(SW, out Timer timer))
        {
            timer.Dispose();
            Console.WriteLine($"'{SW}' canceled.");
        }
        else
        {
            Console.WriteLine($"No timer found with ID '{SW}'.");
        }
    }
}