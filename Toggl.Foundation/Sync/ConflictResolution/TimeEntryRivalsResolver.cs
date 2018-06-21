﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.ConflictResolution
{
    internal sealed class TimeEntryRivalsResolver : IRivalsResolver<IDatabaseTimeEntry, TimeEntryDto>
    {
        private ITimeService timeService;

        public TimeEntryRivalsResolver(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public bool CanHaveRival(IDatabaseTimeEntry entity) => entity.IsRunning();

        public Expression<Func<IDatabaseTimeEntry, bool>> AreRivals(IDatabaseTimeEntry entity)
        {
            if (!CanHaveRival(entity))
                throw new InvalidOperationException("The entity cannot have any rivals.");

            return potentialRival => potentialRival.Duration == null && potentialRival.Id != entity.Id;
        }

        public (TimeEntryDto FixedEntity, TimeEntryDto FixedRival) FixRivals<TDatabaseObject>(
            IDatabaseTimeEntry entity, IDatabaseTimeEntry rival, IQueryable<TDatabaseObject> allTimeEntries)
            where TDatabaseObject : IDatabaseTimeEntry
            => rival.At < entity.At ? (TimeEntryDto.From(entity), stop(rival, allTimeEntries)) : (stop(entity, allTimeEntries), TimeEntryDto.From(rival));

        private TimeEntryDto stop<TDatabaseObject>(IDatabaseTimeEntry toBeStopped, IQueryable<TDatabaseObject> allTimeEntries)
            where TDatabaseObject : IDatabaseTimeEntry
        {
            var timeEntriesStartingAfter = (IEnumerable<IDatabaseTimeEntry>)allTimeEntries
                .Where(startsAfter<TDatabaseObject>(toBeStopped.Start));
            var stopTime = timeEntriesStartingAfter
                .Select(te => te.Start)
                .Where(start => start != default(DateTimeOffset))
                .DefaultIfEmpty(timeService.CurrentDateTime)
                .Min();
            var duration = (long)(stopTime - toBeStopped.Start).TotalSeconds; // truncates towards zero (floor)

            return TimeEntryDto.From<IDatabaseTimeEntry>(
                toBeStopped,
                duration: duration,
                syncStatus: SyncStatus.SyncNeeded,
                at: timeService.CurrentDateTime);
        }

        private Expression<Func<TDatabaseObject, bool>> startsAfter<TDatabaseObject>(DateTimeOffset start)
            where TDatabaseObject : IDatabaseTimeEntry
        {
            var other = Expression.Parameter(typeof(TDatabaseObject), "other");
            var otherStart = Expression.Property(other, nameof(ITimeEntry.Start));
            var startDate = Expression.Constant(start, typeof(DateTimeOffset));
            var expression = Expression.GreaterThan(otherStart, startDate);
            return Expression.Lambda<Func<TDatabaseObject, bool>>(expression, new[] { other });
        }
    }
}
