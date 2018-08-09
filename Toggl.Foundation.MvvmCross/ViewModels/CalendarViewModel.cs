using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarViewModel : MvxViewModel
    {
        private readonly ITimeService timeService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IPermissionsService permissionsService;
        private readonly IMvxNavigationService navigationService;

        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }

        public RxAction<CalendarItem, Unit> OnItemTapped { get; }

        public CalendarViewModel(
            ITimeService timeService,
            IInteractorFactory interactorFactory,
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.permissionsService = permissionsService;

            OnItemTapped = new RxAction<CalendarItem, Unit>(onItemTapped);

            CalendarItems = new ObservableGroupedOrderedCollection<CalendarItem>(
                indexKey: item => item.StartTime,
                orderingKey: item => item.StartTime,
                groupingKey: _ => 0);
        }

        public override async Task Initialize()
        {
            var today = timeService.CurrentDateTime.Date;
            await fetchCalendarItems(today);
        }

        private IObservable<Unit> onItemTapped(CalendarItem calendarItem)
            => Observable.FromAsync(async cancellationToken =>
            {
                switch (calendarItem.Source)
                {
                    case CalendarItemSource.TimeEntry when calendarItem.TimeEntryId.HasValue:
                        await navigationService.Navigate<EditTimeEntryViewModel, long>(calendarItem.TimeEntryId.Value);
                        break;

                    case CalendarItemSource.Calendar:
                        var workspace = await interactorFactory.GetDefaultWorkspace().Execute();
                        var prototype = calendarItem.AsTimeEntryPrototype(workspace.Id);
                        var timeEntry = await interactorFactory.CreateTimeEntry(prototype).Execute();
                        await navigationService.Navigate<EditTimeEntryViewModel, long>(timeEntry.Id);
                        break;
                }

                await fetchCalendarItems(timeService.CurrentDateTime.Date);

            }).SelectUnit();

        private async Task fetchCalendarItems(DateTime date)
        {
            var calendarItems = await interactorFactory.GetCalendarItemsForDate(date).Execute();
            CalendarItems.ReplaceWith(calendarItems);
        }
    }
}
