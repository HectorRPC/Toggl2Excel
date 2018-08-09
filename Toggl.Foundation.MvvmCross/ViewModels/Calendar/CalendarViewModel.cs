using System;
using System.Reactive;
using System.Reactive.Linq;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarViewModel : MvxViewModel
    {
        private readonly ITimeService timeService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IPermissionsService permissionsService;
        private readonly IMvxNavigationService navigationService;

        public IObservable<bool> ShouldShowOnboarding { get; }

        public UIAction GetStartedAction { get; }

        public CalendarViewModel(
            ITimeService timeService,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.permissionsService = permissionsService;

            ShouldShowOnboarding = Observable
                .Return(!onboardingStorage.CompletedCalendarOnboarding());

            GetStartedAction = new UIAction(getStarted);
        }

        private IObservable<Unit> getStarted()
            => permissionsService
                .RequestCalendarAuthorization()
                .Do(handlePermissionRequestResult)
                .SelectUnit();

        private void handlePermissionRequestResult(bool permissionGranted)
        {
            if (permissionGranted)
                Console.WriteLine("Great success");
            else
                navigationService.Navigate<CalendarPermissionDeniedViewModel>();
        }
    }
}
