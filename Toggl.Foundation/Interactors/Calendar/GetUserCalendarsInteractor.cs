using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.Interactors.Calendar
{
    public sealed class GetUserCalendarsInteractor : IInteractor<IObservable<IEnumerable<UserCalendar>>>
    {
        private readonly IUserPreferences userPreferences;
        private readonly ICalendarService calendarService;

        public GetUserCalendarsInteractor(ICalendarService calendarService, IUserPreferences userPreferences)
        {
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));

            this.calendarService = calendarService;
            this.userPreferences = userPreferences;
        }

        public IObservable<IEnumerable<UserCalendar>> Execute()
        {
            var enabledIds = userPreferences.EnabledCalendarIds().ToHashSet();
            
            return calendarService.GetUserCalendars()
                .Select(calendarsWithTheSelectedProperty(enabledIds));
        }

        private Func<IEnumerable<UserCalendar>, IEnumerable<UserCalendar>> calendarsWithTheSelectedProperty(HashSet<string> enabledIds)
            => userCalendars => userCalendars.Select(calendar => calendar.WithSelected(enabledIds.Contains(calendar.Id)));
    }
}
