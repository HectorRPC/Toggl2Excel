using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    public sealed class DeleteNonReferencedProjectGhostsState : ISyncState
    {
        private readonly IProjectsSource projectsDataSource;

        private readonly ITimeEntriesSource timeEntriesDataSource;

        public StateResult FinishedDeleting { get; } = new StateResult();

        public DeleteNonReferencedProjectGhostsState(IProjectsSource projectsDataSource, ITimeEntriesSource timeEntriesDataSource)
        {
            Ensure.Argument.IsNotNull(projectsDataSource, nameof(projectsDataSource));
            Ensure.Argument.IsNotNull(timeEntriesDataSource, nameof(timeEntriesDataSource));

            this.projectsDataSource = projectsDataSource;
            this.timeEntriesDataSource = timeEntriesDataSource;
        }

        public IObservable<ITransition> Start()
            => projectsDataSource.GetAll(project => project.SyncStatus == SyncStatus.RefetchingNeeded)
                .SelectMany(CommonFunctions.Identity)
                .SelectMany(notReferencedByAnyTimeEntryOrNull)
                .Where(project => project != null)
                .ToList()
                .SelectMany(projectsDataSource.DeleteAll)
                .Select(FinishedDeleting.Transition());

        private IObservable<IThreadSafeProject> notReferencedByAnyTimeEntryOrNull(IThreadSafeProject project)
            => timeEntriesDataSource.GetAll(timeEntry => timeEntry.ProjectId == project.Id)
                .Select(referencingTimeEntries => referencingTimeEntries.Any())
                .Select(isReferencedByAnyTimeEntry => isReferencedByAnyTimeEntry ? null : project);
    }
}
