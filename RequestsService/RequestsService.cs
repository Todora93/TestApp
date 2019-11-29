namespace RequestsService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Newtonsoft.Json;
    using MyActorService.Interfaces;
    using System.Threading;
    using System.Text;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using System.Fabric.Query;
    using Microsoft.ServiceFabric.Services.Client;
    using System.Runtime.CompilerServices;
    using Microsoft.ServiceFabric.Actors.Query;
    using System.Linq;
    using System.Fabric.Description;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class RequestsService : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IRequestManager, ISimulationEvents
    {
        private const string UserRequestsQueueName = "userRequestsQueueName";
        private const string UserToActorMapName = "userToActorMapName";
        private const string InUseActorsMapName = "inUseActorsMapName";
        private const string ActorsCountName = "actorsCountName";
        private const string ActorsCount = "actorsCount";

        private static TimeSpan TxTimeout = TimeSpan.FromSeconds(4);
        private static TimeSpan MatchmakeInterval = TimeSpan.FromSeconds(1);

        private readonly StatefulServiceContext context;
        private readonly FabricClient fabricClient;

        // todo: put in config
        private const int MatchSize = 2;
        private const float ActorCountThreshold = 0.1f;
        private const bool ScaleActors = false;

        public RequestsService(StatefulServiceContext context)
            : base(context)
        {
            this.context = context;
            this.fabricClient = new FabricClient();
        }

        public async Task<ExistingMatch> AddRequestAsync(UserRequest request)
        {
            try
            {
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                var requestsQueue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);

                bool hasActor = false;
                ActorId actorId = null;

                ExistingMatch existingMatch = null;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    //var enumerable = await matchmakedUsers.CreateEnumerableAsync(tx, EnumerationMode.Ordered);
                    //var asyncEnumerator = enumerable.GetAsyncEnumerator();
                    //var cancellationToken = new CancellationToken();

                    //while (await asyncEnumerator.MoveNextAsync(cancellationToken))
                    //{
                    //    if (asyncEnumerator.Current.Key.Equals(request))
                    //    {
                    //        hasActor = true;
                    //        actorId = asyncEnumerator.Current.Value;
                    //        break;
                    //    }
                    //}

                    //if (hasActor)
                    //{
                    //    ServiceEventSource.Current.Message($"User {request } has active match on actor {actorId}!");
                    //}
                    //else
                    //{
                    //    await requestsQueue.EnqueueAsync(tx, request);
                    //    ServiceEventSource.Current.Message($"User request {request } added to the queue!");
                    //}

                    ConditionalValue<ActorId> assignedActor;
                    bool hasMatchInProgress = await matchmakedUsers.ContainsKeyAsync(tx, request);
                    if (hasMatchInProgress)
                    {
                        assignedActor = await matchmakedUsers.TryGetValueAsync(tx, request);
                        hasActor = assignedActor.HasValue;

                        if (hasActor)
                        {
                            actorId = assignedActor.Value;
                            ServiceEventSource.Current.Message($"Something went wrong - user {request} has match in progress but cannot get actorId!");
                        }
                        else
                        {
                            ServiceEventSource.Current.Message($"User {request } has active match on actor {actorId}!");
                        }
                    }
                    else
                    {
                        await requestsQueue.EnqueueAsync(tx, request);
                        ServiceEventSource.Current.Message($"User request {request } added to the queue!");
                    }

                    existingMatch = new ExistingMatch(hasActor, actorId);

                    await tx.CommitAsync();
                }

                return existingMatch;
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("AddRequestAsync: Adding request {0} rejected: {1}", request, ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("AddRequestAsync: Exception {0}: {1}", request, ex));
                throw;
            }
        }

        private async Task<bool> MatchmakeOneGame(CancellationToken cancellationToken, IReliableConcurrentQueue<UserRequest> queue, IReliableDictionary<ActorId, PlayersInMatch> usedActors, IReliableDictionary<UserRequest, ActorId> matchmakedUsers)
        {
            try
            {
                ConditionalValue<UserRequest> ret;
                PlayersInMatch players = new PlayersInMatch();
                ActorId actorId;

                using (var tx = this.StateManager.CreateTransaction())
                {
                    do
                    {
                        ret = await queue.TryDequeueAsync(tx, cancellationToken);
                        if (ret.HasValue)
                        {
                            players = players.AddPlayer(new UserRequest(ret.Value.UserName));
                        }
                    }
                    while (!cancellationToken.IsCancellationRequested && ret.HasValue && players.Count < MatchSize);

                    if (cancellationToken.IsCancellationRequested || players.Count != MatchSize)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, cancellationToken.IsCancellationRequested ? $"Cancellation requested!" : $"Not enough players in the queue to matchmake!");
                        tx.Abort();
                        return false;
                    }

                    // found enough players - assign them actor
                    bool usedActor = false;
                    do
                    {
                        actorId = ActorId.CreateRandom();
                        usedActor = await usedActors.ContainsKeyAsync(tx, actorId);
                    }
                    while (!cancellationToken.IsCancellationRequested && usedActor);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Cancellation requested!");
                        tx.Abort();
                        return false;
                    }

                    bool added = await usedActors.TryAddAsync(tx, actorId, players);
                    if (!added)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Tried to add already used actor {actorId}");
                        tx.Abort();
                        return false;
                    }

                    foreach(var player in players.Players)
                    {
                        added = await matchmakedUsers.TryAddAsync(tx, player, actorId);
                        if (!added)
                        {
                            ServiceEventSource.Current.ServiceMessage(this.Context, $"Tried to add already existing request {player}");
                            tx.Abort();
                            return false;
                        }
                    }

                    await tx.CommitAsync();
                }

                string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService";

                List<UserRequest> playersList = players.GetList();

                // Create actor simulation
                ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorId, new Uri(actorServiceUri));
                await simulationActor.SimulateMatch(playersList);
                await simulationActor.SubscribeAsync<ISimulationEvents>(this);

                // Notify clients
                IWebService webService = ServiceProxy.Create<IWebService>(new Uri($"{this.context.CodePackageActivationContext.ApplicationName}/WebService"));
                await webService.StartGame(actorId, playersList);

                StringBuilder builder = new StringBuilder();
                builder.Append($"Created new match\n ActorID: {actorId}\n");

                for (int i = 0; i < players.Count; i++)
                {
                    builder.Append($"UserID_{i}: {playersList[i]}\n");
                }

                ServiceEventSource.Current.ServiceMessage(this.Context, builder.ToString());

                return true;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
                return false;
            }
        }


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, PlayersInMatch>>(InUseActorsMapName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    //StringBuilder s1 = new StringBuilder();
                    
                    //using (var tx = this.StateManager.CreateTransaction())
                    //{
                    //    var enumerable = await matchmakedUsers.CreateEnumerableAsync(tx, EnumerationMode.Ordered);
                    //    var asyncEnumerator = enumerable.GetAsyncEnumerator();

                    //    s1.Append("matchmaked ");
                    //    while (await asyncEnumerator.MoveNextAsync(cancellationToken))
                    //    {
                    //        // Process asyncEnumerator.Current.Key and asyncEnumerator.Current.Value as you wish
                    //        s1.Append($"key: {asyncEnumerator.Current.Key}, value: {asyncEnumerator.Current.Value}");
                    //    }

                    //    var enumerable2 = await usedActors.CreateEnumerableAsync(tx, EnumerationMode.Ordered);
                    //    var asyncEnumerator2 = enumerable2.GetAsyncEnumerator();

                    //    s1.Append("usedActors");
                    //    while (await asyncEnumerator2.MoveNextAsync(cancellationToken))
                    //    {
                    //        // Process asyncEnumerator.Current.Key and asyncEnumerator.Current.Value as you wish
                    //        s1.Append($"key: {asyncEnumerator2.Current.Key}, value: {asyncEnumerator2.Current.Value}");
                    //    }

                    //}
                    //string s = s1.ToString();


                    bool ret;
                    do
                    {
                        ret = await MatchmakeOneGame(cancellationToken, queue, usedActors, matchmakedUsers);
                    } while (ret);


                    if (ScaleActors && lastActorCheck.Add(actorCheckSpan) <= DateTime.Now)
                    {
                        await ManageActors(cancellationToken);
                        lastActorCheck = DateTime.Now;
                    }

                    await Task.Delay(MatchmakeInterval, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
            }
        }

        #region Callbacks

        public async void StateUpdated(ActorId actorId, GameState gameState)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Match state updated, ActorId: {actorId}, GameState: {gameState.ToString()}");

            string serviceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/WebService";

            IWebService service = ServiceProxy.Create<IWebService>(new Uri(serviceUri));

            await service.GameStateChanged(gameState);
        }

        public async void MatchFinished(ActorId actorId, GameState finalGameState)
        {
            try
            {
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                var userActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, PlayersInMatch>>(InUseActorsMapName);

                using (var tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<PlayersInMatch> playersInMatch = await userActors.TryRemoveAsync(tx, actorId);
                    if (!playersInMatch.HasValue)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing in use actors");
                    }
                    else
                    {
                        foreach(var player in playersInMatch.Value.Players)
                        {
                            ConditionalValue<ActorId> remove = await matchmakedUsers.TryRemoveAsync(tx, player);
                            if (!remove.HasValue)
                            {
                                ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing request, it's not present in dictionary");
                                continue;
                            }
                        }
                    }

                    await tx.CommitAsync();
                }

                ServiceEventSource.Current.ServiceMessage(this.Context, $"Match finished, ActorId: {actorId}");

                string serviceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/WebService";

                IWebService service = ServiceProxy.Create<IWebService>(new Uri(serviceUri));

                await service.MatchFinished(finalGameState);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("MatchFinished: Exception {0}: {1}", actorId, ex));
                throw;
            }
        }

        //public async Task<bool> GetActiveActorId(ActorId actorId)
        //{
        //    var userActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, PlayersInMatch>>(InUseActorsMapName);

        //    using (var tx = this.StateManager.CreateTransaction())
        //    {
        //        return await userActors.ContainsKeyAsync(tx, actorId);
        //    }
        //}

        #endregion

        #region

        private DateTime lastActorCheck = DateTime.Now;
        private TimeSpan actorCheckSpan = TimeSpan.FromMilliseconds(100);

        private ActorServicePoolInfo poolInfo;

        public class ActorServicePoolInfo
        {
            public Dictionary<int, List<ActorsCountInfo>> pool;

            public ActorLocationInfo currentPlacementActor;

            private List<ActorsCountInfo> GetCurrentLocations()
            {
                var list = new List<ActorsCountInfo>();
                foreach (var pair in pool)
                {
                    foreach (var info in pair.Value)
                    {
                        list.Add(info);
                    }
                }
                return list;
            }

            public ActorLocationInfo GetActorLocation(float upperThreshold)
            {
                var current = currentPlacementActor;
                var start = current;

                var infos = GetCurrentLocations();
                int startIndex = infos.FindIndex(x => x.Location == currentPlacementActor);
                int currentIndex = startIndex;

                do
                {
                    var info = infos[currentIndex];
                    if (info.Ratio <= upperThreshold)
                    {
                        currentPlacementActor = info.Location;
                        break;
                    }

                    currentIndex = (currentIndex + 1) % infos.Count;
                } while (startIndex != currentIndex);

                if (startIndex == currentIndex)
                {
                    currentPlacementActor = null;
                }

                return currentPlacementActor;
            }

            public List<int> GetActorsToDelete()
            {
                var toDelete = new List<int>();
                foreach (var pair in pool)
                {
                    bool delete = true;
                    foreach (var info in pair.Value)
                    {
                        if (info.Active > 0)
                        {
                            delete = false;
                            break;
                        }
                    }
                    if (delete)
                    {
                        toDelete.Add(pair.Key);
                    }
                }
                return toDelete;
            }
        }

        public class ActorLocationInfo : IComparable, IComparable<ActorLocationInfo>, IEquatable<ActorLocationInfo>
        {
            public int Index;
            public long PartitionKey;
            public int PartitionIndex;
            public int NumOfPartitions;

            public long KeyDiff => (long)(Int64.MaxValue / NumOfPartitions - Int64.MinValue / NumOfPartitions);

            public int CompareTo(object obj)
            {
                return CompareTo((ActorLocationInfo)obj);
            }

            public int CompareTo([AllowNull] ActorLocationInfo other)
            {
                var compare = Index.CompareTo(other.Index);
                if (compare != 0) return compare;
                compare = PartitionIndex.CompareTo(other.Index);
                return compare;
            }

            public bool Equals([AllowNull] ActorLocationInfo other)
            {
                return Index == other.Index && PartitionIndex == other.PartitionIndex;
            }

            public ActorLocationInfo GetNextPartition(int numberOfServices)
            {
                if (PartitionIndex < NumOfPartitions - 1)
                {
                    return new ActorLocationInfo() { Index = Index, PartitionKey = PartitionKey + KeyDiff, PartitionIndex = PartitionIndex + 1, NumOfPartitions = NumOfPartitions };
                }
                else
                {
                    return new ActorLocationInfo() { Index = (Index + 1) % numberOfServices, PartitionKey = Int64.MinValue, PartitionIndex = 0, NumOfPartitions = NumOfPartitions };
                }
            }
        }

        public class ActorsCountInfo
        {
            public ActorLocationInfo Location;

            public int Active;
            public int Inactive;

            public float Ratio => Active / (Active + Inactive);
        }

        public async Task CheckAllActorsAvailability(CancellationToken cancellationToken, List<int> indexes, Dictionary<int, List<ActorsCountInfo>> map)
        {
            foreach (var index in indexes)
            {
                await CheckActorAvailability(cancellationToken, index, map);
            }
        }

        private async Task CheckActorAvailability(CancellationToken cancellationToken, int index, Dictionary<int, List<ActorsCountInfo>> map)
        {
            string actorServiceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{index}";

            ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(actorServiceUri));

            ContinuationToken continuationToken = null;

            if (!map.ContainsKey(index))
            {
                map[index] = new List<ActorsCountInfo>(partitions.Count);
            }

            int partitionIndex = 0;
            foreach (Partition partition in partitions)
            {
                var partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;
                IActorService actorServiceProxy = ActorServiceProxy.Create(new Uri(actorServiceUri), partitionKey);

                int active = 0;
                int inactive = 0;

                do
                {
                    PagedResult<ActorInformation> page = await actorServiceProxy.GetActorsAsync(continuationToken, cancellationToken);
                    active += page.Items.Count(x => x.IsActive);
                    inactive += page.Items.Count(x => !x.IsActive);
                    continuationToken = page.ContinuationToken;
                } while (continuationToken != null);

                var locationInfo = new ActorLocationInfo()
                {
                    Index = index,
                    PartitionIndex = partitionIndex,
                    PartitionKey = partitionKey,
                    NumOfPartitions = partitions.Count
                };

                map[index].Add(new ActorsCountInfo()
                {
                    Location = locationInfo,
                    Active = active,
                    Inactive = inactive,
                });

                partitionIndex++;
            }
        }

        private int FindIndex(List<int> indexes)
        {
            int min = 100, max = -1;
            foreach (var index in indexes)
            {
                min = Math.Min(min, index);
                max = Math.Max(max, index);
            }

            if (max - min + 1 == indexes.Count)
            {
                return min > 0 ? min - 1 : max + 1;
            }
            else
            {
                if (min > 0) return min - 1;

                for (int i = min; i <= max; i++)
                {
                    if (!indexes.Contains(i))
                        return i;
                }

                return max + 1;
            }
        }

        private async Task ManageActors(CancellationToken cancellationToken)
        {
            var newIndexes = new List<int>();
            var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<int>>>(ActorsCountName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var indexes = await map.TryGetValueAsync(tx, ActorsCount);
                newIndexes.AddRange(indexes.Value); ;

                var getActorsToDelete = poolInfo.GetActorsToDelete();
                if (getActorsToDelete.Count > 0)
                {
                    foreach (var index in getActorsToDelete)
                    {
                        await DeleteActor(index);
                        poolInfo.pool.Remove(index);
                        newIndexes.Remove(index);
                    }
                }

                await map.TryUpdateAsync(tx, ActorsCount, newIndexes, indexes.Value);
                await tx.CommitAsync();
            }

            await CheckAllActorsAvailability(cancellationToken, newIndexes, poolInfo.pool);

            if (poolInfo.GetActorLocation(ActorCountThreshold) == null)
            {
                poolInfo.currentPlacementActor = await CreateActor(FindIndex(newIndexes));
            }
        }

        private async Task<ActorLocationInfo> CreateActor(int index)
        {
            var description = new StatefulServiceDescription()
            {
                HasPersistedState = true,
                //ApplicationName = new Uri(this.context.CodePackageActivationContext.ApplicationName),
                ServiceTypeName = "SimulationActorServiceType",
                ServiceName = new Uri($"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{index}"),
                MinReplicaSetSize = 1,
                TargetReplicaSetSize = 1,
            };
            await this.fabricClient.ServiceManager.CreateServiceAsync(description);

            return new ActorLocationInfo()
            {
                Index = index,
                PartitionIndex = 0,
                PartitionKey = Int64.MinValue,
                NumOfPartitions = 1
            };
        }

        private async Task DeleteActor(int index)
        {
            var description = new DeleteServiceDescription(new Uri($"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{index}"));
            await this.fabricClient.ServiceManager.DeleteServiceAsync(description);
        }

        #endregion

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        #region Helpers

        public async Task<string> GetAllRequests()
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);

                List<UserRequest> items = new List<UserRequest>();

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<UserRequest> ret;

                    do
                    {
                        ret = await queue.TryDequeueAsync(tx);

                        if (ret.HasValue)
                        {
                            items.Add(ret.Value);
                        }

                    } while (ret.HasValue);

                    tx.Abort();
                }

                return JsonConvert.SerializeObject(items);
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("GetAllRequests: Getting requests rejected: {0}", ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("GetAllRequests: Exception {0}", ex));
                throw;
            }
        }

        public async Task DeleteAllRequests()
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);

                List<UserRequest> items = new List<UserRequest>();

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<UserRequest> ret;

                    do
                    {
                        ret = await queue.TryDequeueAsync(tx);

                        if (ret.HasValue)
                        {
                            items.Add(ret.Value);
                        }

                    } while (ret.HasValue);

                    await tx.CommitAsync();
                }
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("DeleteAllRequests: Getting requests rejected: {0}", ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("DeleteAllRequests: Exception {0}", ex));
                throw;
            }
        }

        public async Task DeleteAll()
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, PlayersInMatch>>(InUseActorsMapName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<UserRequest> ret;

                    do
                    {
                        ret = await queue.TryDequeueAsync(tx);
                    } while (ret.HasValue);

                    await matchmakedUsers.ClearAsync();
                    await usedActors.ClearAsync();

                    await tx.CommitAsync();
                }
            }
            catch (InvalidOperationException ex)
            {
                ServiceEventSource.Current.Message(string.Format("DeleteAllRequests: Getting requests rejected: {0}", ex));
                throw;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("DeleteAllRequests: Exception {0}", ex));
                throw;
            }
        }

        public async Task Matchmake(CancellationToken cancellationToken)
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, PlayersInMatch>>(InUseActorsMapName);
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);

                await MatchmakeOneGame(cancellationToken, queue, usedActors, matchmakedUsers);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
            }
        }

        #endregion
    }
}
