using System;
using System.Reactive.Linq;
using MvvmCross;
using Toggl.Foundation.MvvmCross.Services;

namespace Toggl.Giskard.Services
{
    [Preserve(AllMembers = true)]
    public sealed class PermissionsService : IPermissionsService
    {
        public bool CalendarPermissionGranted => false;

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
            => Observable.Return(false);

        public void OpenAppSettings()
        {
        }
    }
}
