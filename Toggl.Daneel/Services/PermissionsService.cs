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
        public bool CalendarAuthorizationStatus
            => EKEventStore.GetAuthorizationStatus(EKEntityType.Event) == EKAuthorizationStatus.Authorized;

        public IObservable<bool> RequestCalendarAuthorization(bool force = false)
            => Observable.DeferAsync(async cancellationToken =>
                {
                    var eventStore = new EKEventStore();
                    var result = await eventStore.RequestAccessAsync(EKEntityType.Event);
                    return Observable.Return(result.Item1);
                });

        public void OpenAppSettings()
        {
            UIApplication.SharedApplication.OpenUrl(
                NSUrl.FromString(UIApplication.OpenSettingsUrlString)
            );
        }
    }
}
