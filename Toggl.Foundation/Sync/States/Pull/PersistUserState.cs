﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class PersistUserState : BasePersistState<IUser, IDatabaseUser>
    {
        public PersistUserState(IRepository<IDatabaseUser> repository, ISinceParameterRepository sinceParameterRepository)
            : base(repository, User.Clean, sinceParameterRepository, Resolver.ForUser())
        {
        }

        protected override IObservable<IEnumerable<IUser>> FetchObservable(FetchObservables fetch)
            => fetch.User.Select(user
                => user == null
                    ? new User[0]
                    : new[] { user });

        protected override ISinceParameters UpdateSinceParameters(ISinceParameters old, DateTimeOffset? lastUpdated)
            => old;
    }
}
