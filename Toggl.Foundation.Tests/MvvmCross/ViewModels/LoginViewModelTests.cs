﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class LoginViewModelTests
    {
        public abstract class LoginViewModelTest : BaseViewModelTests<LoginViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("this is not an email");

            protected Password ValidPassword { get; } = Password.From("T0t4lly s4afe p4$$");
            protected Password InvalidPassword { get; } = Password.From("123");

            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();

            protected override LoginViewModel CreateViewModel()
                => new LoginViewModel(
                    LoginManager,
                    AnalyticsService,
                    OnboardingStorage,
                    NavigationService,
                    PasswordManagerService,
                    ErrorHandlingService,
                    LastTimeUsageStorage,
                    TimeService,
                    SchedulerProvider);
        }

        public sealed class TheConstructor : LoginViewModelTest
        {
            [Xunit.Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool userLoginManager,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool usePasswordManagerService,
                bool useApiErrorHandlingService,
                bool useLastTimeUsageStorage,
                bool useTimeService,
                bool useSchedulerProvider)
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var passwordManagerService = usePasswordManagerService ? PasswordManagerService : null;
                var apiErrorHandlingService = useApiErrorHandlingService ? ErrorHandlingService : null;
                var lastTimeUsageStorage = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new LoginViewModel(loginManager,
                                             analyticsSerivce,
                                             onboardingStorage,
                                             navigationService,
                                             passwordManagerService,
                                             apiErrorHandlingService,
                                             lastTimeUsageStorage,
                                             timeService,
                                             schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheLoginEnabledObservable : LoginViewModelTest
        {
            [Xunit.Theory]
            [InlineData("invalid email address", "123")]
            [InlineData("invalid email address", "T0tally s4afe p4a$$")]
            [InlineData("person@company.com", "123")]
            public void ReturnsFalseWhenEmailOrPasswordIsInvalid(string email, string password)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.LoginEnabled.Subscribe(observer);
                ViewModel.SetEmail(Email.From(email));
                ViewModel.SetPassword(Password.From(password));

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(2, false)
                );
            }

            [Xunit.Theory]
            [InlineData("invalid email address", "123")]
            [InlineData("invalid email address", "T0tally s4afe p4a$$")]
            [InlineData("person@company.com", "123")]
            public async Task ReturnsFalseWhenIsLoading(string email, string password)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.LoginEnabled.Subscribe(observer);

                ViewModel.SetEmail(Email.From(email));
                ViewModel.SetPassword(Password.From(password));
                //Make sure isloading is true
                LoginManager
                    .Login(Arg.Any<Email>(), Arg.Any<Password>())
                    .Returns(Observable.Never<ITogglDataSource>());
                ViewModel.Login();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(2, false)
                );
            }
        }

        public sealed class TheIsPasswordManagerAvailableProperty : LoginViewModelTest
        {
            [FsCheck.Xunit.Property]
            public void ReturnsWhetherThePasswordManagerIsAvailable(bool isAvailable)
            {
                PasswordManagerService.IsAvailable.Returns(isAvailable);

                var viewModel = CreateViewModel();

                viewModel.IsPasswordManagerAvailable.Should().Be(isAvailable);
            }
        }

        public sealed class TheLoginMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheLoginManagerWhenTheEmailAndPasswordAreValid()
            {
                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);

                ViewModel.Login();

                LoginManager.Received().Login(Arg.Is(ValidEmail), Arg.Is(ValidPassword));
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenThePageIsCurrentlyLoading()
            {
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>()).Returns(never);
                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);
                ViewModel.Login();

                ViewModel.Login();

                LoginManager.Received(1).Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            public sealed class WhenLoginSucceeds : LoginViewModelTest
            {
                public WhenLoginSucceeds()
                {
                    ViewModel.SetEmail(ValidEmail);
                    ViewModel.SetPassword(ValidPassword);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Return(DataSource));
                }

                [Fact, LogIfTooSlow]
                public async Task StartsSyncing()
                {
                    ViewModel.Login();

                    await DataSource.Received().StartSyncing();
                }

                [Fact, LogIfTooSlow]
                public void SetsIsNewUserToFalse()
                {
                    ViewModel.Login();

                    OnboardingStorage.Received().SetIsNewUser(false);
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, false),
                        ReactiveTest.OnNext(2, true),
                        ReactiveTest.OnNext(3, false)
                    );
                }

                [Fact, LogIfTooSlow]
                public void NavigatesToTheTimeEntriesViewModel()
                {
                    ViewModel.Login();

                    NavigationService.Received().Navigate<MainViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void TracksTheLoginEvent()
                {
                    ViewModel.Login();

                    AnalyticsService.Received().Login.Track(AuthenticationMethod.EmailAndPassword);
                }

                [FsCheck.Xunit.Property]
                public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                {
                    TimeService.CurrentDateTime.Returns(now);

                    ViewModel.Login();

                    LastTimeUsageStorage.Received().SetLogin(Arg.Is(now));
                }
            }

            public sealed class WhenLoginFails : LoginViewModelTest
            {
                public WhenLoginFails()
                {
                    ViewModel.SetEmail(ValidEmail);
                    ViewModel.SetPassword(ValidPassword);
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, false),
                        ReactiveTest.OnNext(2, true),
                        ReactiveTest.OnNext(3, false)
                    );
                }

                [Fact, LogIfTooSlow]
                public void DoesNotNavigate()
                {
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.Login();

                    NavigationService.DidNotReceive().Navigate<MainViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToIncorrectEmailOrPasswordWhenReceivedUnauthorizedException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    var exception = new UnauthorizedException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, ""),
                        ReactiveTest.OnNext(2, Resources.IncorrectEmailOrPassword)
                    );
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToNothingWhenGoogleLoginWasCanceled()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new GoogleLoginException(true);
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, "")
                    );
                }

                [Fact, LogIfTooSlow]
                public void SetsTheErrorMessageToGenericLoginErrorForAnyOtherException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new Exception();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, ""),
                        ReactiveTest.OnNext(2, Resources.GenericLoginError)
                    );
                }

                [Fact, LogIfTooSlow]
                public void DoesNothingWhenErrorHandlingServiceHandlesTheException()
                {
                    var observer = TestScheduler.CreateObserver<string>();
                    var exception = new Exception();
                    ViewModel.ErrorMessage.Subscribe(observer);
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<Password>())
                        .Returns(Observable.Throw<ITogglDataSource>(exception));
                    ErrorHandlingService.TryHandleDeprecationError(Arg.Any<Exception>())
                        .Returns(true);

                    ViewModel.Login();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, "")
                    );
                }
            }
        }

        public sealed class TheGoogleLoginMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CallsTheLoginManager()
            {
                ViewModel.GoogleLogin();

                LoginManager.Received().LoginWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenThePageIsCurrentlyLoading()
            {
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.LoginWithGoogle().Returns(never);
                ViewModel.GoogleLogin();

                ViewModel.GoogleLogin();

                LoginManager.Received(1).LoginWithGoogle();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToTheTimeEntriesViewModelWhenTheLoginSucceeds()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                NavigationService.Received().Navigate<MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItCompletes()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, false),
                    ReactiveTest.OnNext(2, true),
                    ReactiveTest.OnNext(3, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void TracksGoogleLoginEvent()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                AnalyticsService.Received().Login.Track(AuthenticationMethod.Google);
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItErrors()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLoading.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleLogin();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, false),
                    ReactiveTest.OnNext(2, true),
                    ReactiveTest.OnNext(3, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateWhenTheLoginFails()
            {
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                ViewModel.GoogleLogin();

                NavigationService.DidNotReceive().Navigate<MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotDisplayAnErrormessageWhenTheUserCancelsTheRequestOnTheGoogleService()
            {
                var observer = SchedulerProvider.TestScheduler.CreateObserver<string>();
                ViewModel.ErrorMessage.Subscribe(observer);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Throw<ITogglDataSource>(new GoogleLoginException(true)));

                ViewModel.GoogleLogin();

                SchedulerProvider.TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, "")
                );
            }

            [FsCheck.Xunit.Property]
            public void SavesTheTimeOfLastLogin(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);
                LoginManager.LoginWithGoogle()
                    .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.GoogleLogin();

                LastTimeUsageStorage.Received().SetLogin(Arg.Is(now));
            }
        }

        public sealed class TheTogglePasswordVisibilityMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToFalseWhenItIsTrue()
            {
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsPasswordMaskedToTrueWhenItIsFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.IsPasswordMasked.Subscribe(observer);
                ViewModel.TogglePasswordVisibility();

                ViewModel.TogglePasswordVisibility();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false),
                    ReactiveTest.OnNext(3, true)
                );
            }
        }

        public sealed class TheStartPasswordManagerCommand : LoginViewModelTest
        {
            public TheStartPasswordManagerCommand()
            {
                PasswordManagerService.IsAvailable.Returns(true);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallPasswordManagerWhenThePasswordManagerIsNotAvailable()
            {
                PasswordManagerService.IsAvailable.Returns(false);

                await ViewModel.StartPasswordManager();

                await PasswordManagerService.DidNotReceive().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsThePasswordManagerServiceWhenTheServiceIsAvailable()
            {
                var observable = Observable.Return(new PasswordManagerResult(ValidEmail, ValidPassword));
                PasswordManagerService.GetLoginInformation().Returns(observable);

                await ViewModel.StartPasswordManager();

                await PasswordManagerService.Received().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheLoginCommandWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                await ViewModel.StartPasswordManager();

                await LoginManager.Received().Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheEmailFieldWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Email.Subscribe(observer);

                await ViewModel.StartPasswordManager();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Email.Empty.ToString()),
                    ReactiveTest.OnNext(2, ValidEmail.ToString())
                );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheEmailFieldWhenInvalidCredentialsAreProvided()
            {
                arrangeCallToPasswordManagerWithInvalidCredentials();
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Email.Subscribe(observer);

                await ViewModel.StartPasswordManager();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Email.Empty.ToString()),
                    ReactiveTest.OnNext(2, InvalidEmail.ToString())
                );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsThePasswordFieldWhenValidCredentialsAreProvided()
            {
                arrangeCallToPasswordManagerWithValidCredentials();
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Password.Subscribe(observer);

                await ViewModel.StartPasswordManager();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Password.Empty.ToString()),
                    ReactiveTest.OnNext(2, ValidPassword.ToString())
                );
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetThePasswordFieldWhenInvalidCredentialsAreProvided()
            {
                arrangeCallToPasswordManagerWithInvalidCredentials();
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Password.Subscribe(observer);

                await ViewModel.StartPasswordManager();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Password.Empty.ToString())
                );
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNothingWhenValidCredentialsAreNotProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                await ViewModel.StartPasswordManager();

                await LoginManager.DidNotReceive().Login(Arg.Any<Email>(), Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerButtonClicked()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.DidNotReceive().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.DidNotReceive().Track();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerContainsValidEmail()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                var loginInfo = new PasswordManagerResult(ValidEmail, InvalidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.Received().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.DidNotReceive().Track();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksThePasswordManagerContainsValidPassword()
            {
                PasswordManagerService.IsAvailable.Returns(true);
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                await ViewModel.StartPasswordManager();

                AnalyticsService.PasswordManagerButtonClicked.Received().Track();
                AnalyticsService.PasswordManagerContainsValidEmail.Received().Track();
                AnalyticsService.PasswordManagerContainsValidPassword.Received().Track();
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithValidCredentials()
            {
                var loginInfo = new PasswordManagerResult(ValidEmail, ValidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithInvalidCredentials()
            {
                var loginInfo = new PasswordManagerResult(InvalidEmail, InvalidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }
        }

        public sealed class TheSignupCommand : LoginViewModelTest
        {
            [FsCheck.Xunit.Property]
            public void NavigatesToTheSignupViewModel(
                NonEmptyString emailString, NonEmptyString passwordString)
            {
                var email = Email.From(emailString.Get);
                var password = Password.From(passwordString.Get);
                ViewModel.SetEmail(email);
                ViewModel.SetPassword(password);

                ViewModel.Signup().Wait();

                NavigationService
                    .Received()
                    .Navigate<SignupViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(parameter
                            => parameter.Email.Equals(email)
                            && parameter.Password.Equals(password)
                        )
                    );
            }
        }

        public sealed class ThePrepareMethod : LoginViewModelTest
        {
            //Todo: fix flaky test
            //[FsCheck.Xunit.Property(Replay = "1149693933,296482725")]
            //[FsCheck.Xunit.Property(Replay = "1503434587,296482728")]
            [FsCheck.Xunit.Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var viewModel = CreateViewModel();
                var email = Email.From(emailString.Get);
                var password = Password.Empty;
                var parameter = CredentialsParameter.With(email, password);
                var expectedValues = new[] { Email.Empty.ToString(),  email.TrimmedEnd().ToString() };
                var actualValues = new List<string>();
                viewModel.Email.Subscribe(actualValues.Add);

                viewModel.Prepare(parameter);

                TestScheduler.Start();
                CollectionAssert.AreEqual(expectedValues, actualValues);
            }

            [FsCheck.Xunit.Property]
            public void SetsThePassword(NonEmptyString passwordString)
            {
                var viewModel = CreateViewModel();
                var email = Email.Empty;
                var password = Password.From(passwordString.Get);
                var parameter = CredentialsParameter.With(email, password);
                var expectedValues = new[] { Password.Empty.ToString(),  password.ToString() };
                var actualValues = new List<string>();
                viewModel.Password.Subscribe(actualValues.Add);

                viewModel.Prepare(parameter);

                TestScheduler.Start();
                CollectionAssert.AreEqual(expectedValues, actualValues);
            }
        }
    }
}
