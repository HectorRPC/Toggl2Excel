using System;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using ColorHelper = Toggl.Foundation.Helper.Color;

namespace Toggl.Foundation.Calendar
{
    public struct CalendarItem
    {
        public CalendarItemSource Source { get; }

        public TimeSpan Duration { get; }

        public DateTimeOffset StartTime { get; }

        public DateTimeOffset EndTime => StartTime + Duration;

        public string Description { get; }

        public string Color { get; }

        public long? TimeEntryId { get; }

        public CalendarItem(CalendarItemSource source,
            DateTimeOffset startTime,
            TimeSpan duration,
            string description,
            string color = "",
            long? timeEntryId = null)
        {
            Source = source;
            StartTime = startTime;
            Duration = duration;
            Description = description;
            Color = color;
            TimeEntryId = timeEntryId;
        }

        public CalendarItem(IThreadSafeTimeEntry timeEntry)
            : this(CalendarItemSource.TimeEntry,
                timeEntry.Start,
                TimeSpan.FromSeconds(timeEntry.Duration.Value),
                timeEntry.Description,
                timeEntry.Project?.Color ?? ColorHelper.NoProject,
                timeEntry.Id)
        {
        }

        public static CalendarItem From(IThreadSafeTimeEntry timeEntry)
            => new CalendarItem(timeEntry);

        public ITimeEntryPrototype AsTimeEntryPrototype(long workspaceId)
            => new CalendarItemTimeEntryPrototype(this, workspaceId);

        private sealed class CalendarItemTimeEntryPrototype : ITimeEntryPrototype
        {
            public long WorkspaceId { get; }

            public string Description { get; }

            public TimeSpan? Duration { get; }

            public DateTimeOffset StartTime { get; }

            public long? ProjectId => null;

            public long? TaskId => null;

            public long[] TagIds => null;

            public bool IsBillable => false;

            public CalendarItemTimeEntryPrototype(CalendarItem calendarItem, long workspaceId)
            {
                WorkspaceId = workspaceId;
                Duration = calendarItem.Duration;
                StartTime = calendarItem.StartTime;
                Description = calendarItem.Description;
            }
        }
    }
}
