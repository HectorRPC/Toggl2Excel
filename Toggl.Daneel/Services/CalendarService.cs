using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using EventKit;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Daneel.Services
{
    public sealed class CalendarService : BaseCalendarService
    {
        private readonly EKEventStore eventStore = new EKEventStore();

        public CalendarService(IPermissionsService permissionsService)
            : base (permissionsService)
        {
        }

        public override IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date)
            => Observable.Return(new List<CalendarItem>());

        protected override IEnumerable<UserCalendar> NativeGetUserCalendars()
            => eventStore
                .GetCalendars(EKEntityType.Event)
                .Select(ekCalendar => userCalendarFromEKCalendar(
                    calendar: ekCalendar,
                    selected: selectedIds.Contains(ekCalendar.CalendarIdentifier))
                );
        
        private UserCalendar userCalendarFromEKCalendar(EKCalendar calendar, bool selected)
            => new UserCalendar(
                calendar.CalendarIdentifier,
                calendar.Title,
                calendar.Source.Title,
                selected);
    }
}
