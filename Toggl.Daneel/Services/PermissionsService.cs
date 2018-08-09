using System;
using System.Reactive.Linq;
using EventKit;
using Foundation;
using Toggl.Foundation.MvvmCross.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    [Preserve(AllMembers = true)]
    public sealed class PermissionsService : IPermissionsService
    {
        public bool CalendarPermissionGranted
            => EKEventStore.GetAuthorizationStatus(EKEntityType.Event) == EKAuthorizationStatus.Authorized;

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
        {
            if (CalendarPermissionGranted)
                return Observable.Return(true);

            if (force)
            {
                //Fact: If the user changes any permissions through the settings, this app gets restarted
                //and in that case we don't care about the value returned from this method.
                //We care about the returned value in the case, when user opens settings
                //and comes back to this app without altering any permissions. In that case
                //returning the current permission status is the correct behaviour.
                OpenAppSettings();
                return Observable.Return(CalendarPermissionGranted);
            }

            return Observable
                .FromAsync(async _ => await new EKEventStore().RequestAccessAsync(EKEntityType.Event))
                .Select(tuple => tuple.Item1);
        }

        public void OpenAppSettings()
        {
            UIApplication.SharedApplication.OpenUrl(
                NSUrl.FromString(UIApplication.OpenSettingsUrlString)
            );
        }
    }
}
