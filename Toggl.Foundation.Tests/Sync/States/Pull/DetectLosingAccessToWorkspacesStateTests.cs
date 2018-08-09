using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class DetectLosingAccessToWorkspacesStateTests
    {
        public sealed class TheConstructor
        {
            [Fact]
            public void ThrowsWhenArgumentIsNull()
            {
                Action createWithoutArgument = () => new DetectLosingAccessToWorkspacesState(null);

                createWithoutArgument.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod
        {
            private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource =
                Substitute.For<IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace>>();

            private readonly IFetchObservables fetchObservables = Substitute.For<IFetchObservables>();

            [Fact]
            public async Task ReturnsFetchObservablesAsParameterOfTransition()
            {
                prepareDatabase(new[]
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 },
                    new MockWorkspace { Id = 3 }
                });
                prepareFetch(new List<IWorkspace>
                {
                    new MockWorkspace { Id = 1 }
                });
                var state = new DetectLosingAccessToWorkspacesState(dataSource);

                var transition = await state.Start(fetchObservables);
                var parameter = ((Transition<IFetchObservables>)transition).Parameter;

                parameter.Should().Be(fetchObservables);
            }

            [Fact]
            public async Task MarksWorkspacesWhichAreStoredLocallyButAreNotInTheListFromTheServerAsGhosts()
            {
                prepareDatabase(new[]
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 },
                    new MockWorkspace { Id = 3 }
                });
                prepareFetch(new List<IWorkspace>
                {
                    new MockWorkspace { Id = 1 }
                });
                var state = new DetectLosingAccessToWorkspacesState(dataSource);

                var transition = await state.Start(fetchObservables);

                transition.Result.Should().Be(state.Continue);
                await dataSource.Received()
                    .Update(Arg.Is<IThreadSafeWorkspace>(workspace => workspace.Id == 2 && workspace.IsGhost));
                await dataSource.Received()
                    .Update(Arg.Is<IThreadSafeWorkspace>(workspace => workspace.Id == 3 && workspace.IsGhost));
            }

            [Fact]
            public async Task IgnoresWorkspacesWhichAreStoredOnlyLocally()
            {
                prepareDatabase(new[]
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 },
                    new MockWorkspace { Id = -3 }
                });
                prepareFetch(new List<IWorkspace>
                {
                    new MockWorkspace { Id = 1 }
                });
                var state = new DetectLosingAccessToWorkspacesState(dataSource);

                var transition = await state.Start(fetchObservables);

                transition.Result.Should().Be(state.Continue);
                await dataSource.Received()
                    .Update(Arg.Is<IThreadSafeWorkspace>(workspace => workspace.Id == 2 && workspace.IsGhost));
            }

            [Fact]
            public async Task IgnoresGhostWorkspaces()
            {
                prepareDatabase(new[]
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 },
                    new MockWorkspace { Id = 3, IsGhost = true }
                });
                prepareFetch(new List<IWorkspace>
                {
                    new MockWorkspace { Id = 1 }
                });
                var state = new DetectLosingAccessToWorkspacesState(dataSource);

                var transition = await state.Start(fetchObservables);

                transition.Result.Should().Be(state.Continue);
                await dataSource.Received()
                    .Update(Arg.Is<IThreadSafeWorkspace>(workspace => workspace.Id == 2 && workspace.IsGhost));
            }

            [Fact]
            public async Task DoesNotMarkAnyWorkspaceAsGhostWhenNoAccessIsLostSinceLastTime()
            {
                prepareDatabase(new[]
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 },
                    new MockWorkspace { Id = 3, IsGhost = true }
                });
                prepareFetch(new List<IWorkspace>
                {
                    new MockWorkspace { Id = 1 },
                    new MockWorkspace { Id = 2 }
                });
                var state = new DetectLosingAccessToWorkspacesState(dataSource);

                var transition = await state.Start(fetchObservables);

                transition.Result.Should().Be(state.Continue);
                await dataSource.DidNotReceive().Update(Arg.Any<IThreadSafeWorkspace>());
            }

            private void prepareDatabase(IEnumerable<IThreadSafeWorkspace> workspaces)
            {
                dataSource.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>())
                    .Returns(callInfo =>
                        Observable.Return(
                            workspaces.Where<IThreadSafeWorkspace>(callInfo.Arg<Func<IDatabaseWorkspace, bool>>())));
            }

            private void prepareFetch(List<IWorkspace> workspaces)
            {
                fetchObservables.GetList<IWorkspace>().Returns(Observable.Return(workspaces));
            }
        }
    }
}
