using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Giskard.Services
{
    public sealed class CalendarService : BaseCalendarService
    {
        public CalendarService(IPermissionsService permissionsService) 
            : base(permissionsService)
        {
        }

        public override IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date)
            => Observable.Return(new List<CalendarItem>());

        protected override IEnumerable<UserCalendar> NativeGetUserCalendars()
            => new List<UserCalendar>();
    }
}
