using System;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Views.Cells.Calendar
{
    public sealed partial class HourSupplementaryView : UICollectionReusableView
    {
        public static readonly NSString Key = new NSString(nameof(HourSupplementaryView));
        public static readonly UINib Nib;

        static HourSupplementaryView()
        {
            Nib = UINib.FromName(nameof(HourSupplementaryView), NSBundle.MainBundle);
        }

        protected HourSupplementaryView(IntPtr handle) : base(handle)
        {
        }
    }
}
