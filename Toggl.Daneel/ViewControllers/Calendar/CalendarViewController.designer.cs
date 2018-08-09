// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
	[Register ("CalendarViewController")]
	partial class CalendarViewController
	{
		[Outlet]
		UIKit.UIButton GetStartedButton { get; set; }

		[Outlet]
		UIKit.UIView OnboardingView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (OnboardingView != null) {
				OnboardingView.Dispose ();
				OnboardingView = null;
			}

			if (GetStartedButton != null) {
				GetStartedButton.Dispose ();
				GetStartedButton = null;
			}
		}
	}
}
