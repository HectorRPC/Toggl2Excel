﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Android.Content;
using Android.Support.V4.App;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Giskard.Activities;

namespace Toggl.Giskard.Presenters
{
    public sealed class TogglPresenter : MvxAppCompatViewPresenter
    {
        private readonly HashSet<Type> clearBackStackTypes = new HashSet<Type>
        {
            typeof(MainViewModel),
            typeof(LoginViewModel),
            typeof(SignupViewModel),
            typeof(OnboardingViewModel),
            typeof(TokenResetViewModel),
            typeof(OutdatedAppViewModel)
        };

        public TogglPresenter(IEnumerable<Assembly> androidViewAssemblies)
            : base(androidViewAssemblies)
        {
        }

        protected override Intent CreateIntentForRequest(MvxViewModelRequest request)
        {
            var intent = base.CreateIntentForRequest(request);

            if (clearBackStackTypes.Contains(request.ViewModelType))
            {
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.ClearTask | ActivityFlags.NewTask);
            }

            return intent;
        }

        public override void ChangePresentation(MvxPresentationHint hint)
        {
            switch (hint)
            {
                case ToggleReportsCalendarVisibilityHint calendarHint when CurrentActivity is ReportsActivity reportsActivity:
                    reportsActivity.ToggleCalendarState(calendarHint.ForceHide);
                    return;
            }

            base.ChangePresentation(hint);
        }

        protected override bool CloseFragmentDialog(IMvxViewModel viewModel, MvxDialogFragmentPresentationAttribute attribute)
        {
            var tag = FragmentJavaName(attribute.ViewType);
            var toClose = CurrentFragmentManager?.FindFragmentByTag(tag);
            if (toClose is DialogFragment dialog)
            {
                dialog.DismissAllowingStateLoss();
                return true;
            }

            return false;
        }
    }
}
