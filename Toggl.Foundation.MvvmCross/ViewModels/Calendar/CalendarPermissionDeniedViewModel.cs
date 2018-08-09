using System;
using System.Reactive;
using System.Reactive.Linq;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarPermissionDeniedViewModel : MvxViewModel
    {
        private readonly IPermissionsService permissionsService;
        private readonly IMvxNavigationService navigationService;

        public UIAction EnableAccessAction { get; }

        public UIAction ContinueWithoutAccessAction { get; }

        public CalendarPermissionDeniedViewModel(
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.navigationService = navigationService;
            this.permissionsService = permissionsService;

            EnableAccessAction = new UIAction(enableAccess);
            ContinueWithoutAccessAction = new UIAction(continueWithoutAccessAction);
        }

        private IObservable<Unit> continueWithoutAccessAction()
            => Observable
                .FromAsync(async () => await navigationService.Close(this))
                .SelectUnit();

        private IObservable<Unit> enableAccess()
        {
            permissionsService.OpenAppSettings();
            return Observable.Return(Unit.Default);
        }
    }
}
