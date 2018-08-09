using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.Calendar
{
    public abstract class BaseCalendarService : ICalendarService
    {
        private readonly IPermissionsService permissionsService;

        public BaseCalendarService(IPermissionsService permissionsService)
        {
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.permissionsService = permissionsService;
        }

        public IObservable<IEnumerable<UserCalendar>> GetUserCalendars()
            => Observable.DeferAsync(async (cancellactionToken) =>
            {
                var isAuthorized = await permissionsService.CalendarAuthorizationStatus;
                if (!isAuthorized)
                {
                    return Observable.Throw<IEnumerable<UserCalendar>>(
                        new NotAuthorizedException("You don't have permission to access calendars")
                    );
                }

                var calendars = NativeGetUserCalendars();
                return Observable.Return(calendars);
            });

        public abstract IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date);

        protected abstract IEnumerable<UserCalendar> NativeGetUserCalendars();
    }
}
