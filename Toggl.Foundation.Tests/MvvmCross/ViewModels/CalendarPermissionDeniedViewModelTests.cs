using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarPermissionDeniedViewModelTests
    {
        public abstract class CalendarPermissionDeniedViewModelTest
            : BaseViewModelTests<CalendarPermissionDeniedViewModel>
        {
            protected override CalendarPermissionDeniedViewModel CreateViewModel()
                => new CalendarPermissionDeniedViewModel(PermissionsService, NavigationService);
        }

        public sealed class TheConstructor : CalendarPermissionDeniedViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool usePermissionsService, bool useNavigationService)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarPermissionDeniedViewModel(
                        usePermissionsService ? PermissionsService : null,
                        useNavigationService ? NavigationService : null
                    );

                tryingToConstructWithEmptyParameters
                    .Should()
                    .Throw<ArgumentNullException>();
            }
        }

        public sealed class TheEnableAccessAction : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task OpensAppSettings()
            {
                await ViewModel.EnableAccessAction.Execute(Unit.Default);

                PermissionsService.Received().OpenAppSettings();
            }
        }

        public sealed class TheContinueWithoutAccessAction : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.ContinueWithoutAccessAction.Execute(Unit.Default);

                await NavigationService.Received().Close(ViewModel);
            }
        }
    }
}
