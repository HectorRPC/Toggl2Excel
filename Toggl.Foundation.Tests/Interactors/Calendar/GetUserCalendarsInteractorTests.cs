﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors.Calendar;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Calendar
{
    public sealed class GetUserCalendarsInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            [Theory]
            [ConstructorData]
            public void ThrowsIfTheArgumentIsNull(bool useCalendarService, bool useUserPreferences)
            {
                var calendarService = useCalendarService ? CalendarService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;

                Action tryingToConstructWithNulls =
                    () => new GetUserCalendarsInteractor(calendarService, userPreferences);

                tryingToConstructWithNulls.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            private static readonly IEnumerable<UserCalendar> calendarsFromService = new List<UserCalendar>
            {
                new UserCalendar("foo", "foo", "Google Calendar"),
                new UserCalendar("bar", "bar", "Google Calendar"),
                new UserCalendar("baz", "baz", "Google Calendar")
            };

            private static readonly IEnumerable<string> selectedCalendars = new List<string> { "foo", "bar" };

            [Fact]
            public async Task ReturnsAllCalendarsFromTheCalendarService()
            {
                var observable = Observable.Return(calendarsFromService);
                CalendarService.GetUserCalendars().Returns(observable);

                var calendars = await InteractorFactory.GetUserCalendars().Execute();
                calendars.Should().HaveCount(calendarsFromService.Count());
            }

            [Fact]
            public async Task SetsTheCalendarsToSelectedWhenTheyWereSelectedByTheUser()
            {

                var observable = Observable.Return(calendarsFromService);
                CalendarService.GetUserCalendars().Returns(observable);
                UserPreferences.EnabledCalendarIds().Returns(selectedCalendars);

                var calendars = await InteractorFactory.GetUserCalendars().Execute();

                calendars.Where(c => c.IsSelected).Should().HaveCount(selectedCalendars.Count());
            }
        }
    }
}
