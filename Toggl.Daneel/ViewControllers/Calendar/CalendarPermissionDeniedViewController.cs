using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Daneel.Presentation.Attributes;
using CoreGraphics;
using UIKit;

namespace Toggl.Daneel.ViewControllers.Calendar
{
    [ModalDialogPresentation]
    public sealed partial class CalendarPermissionDeniedViewController
        : ReactiveViewController<CalendarPermissionDeniedViewModel>
    {
        private const float cardHeight = 342;

        public CalendarPermissionDeniedViewController() : base(null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            this.Bind(EnableAccessButton.Tapped(), ViewModel.EnableAccessAction);
            this.Bind(ContinueWithoutAccessButton.Tapped(), ViewModel.ContinueWithoutAccessAction);
        }
    }
}

