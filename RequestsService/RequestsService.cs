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
    using System.Runtime.CompilerServices;
    using Microsoft.ServiceFabric.Actors.Query;
    using System.Linq;
    using System.Fabric.Description;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        private static TimeSpan ResubsriptionInterval = TimeSpan.FromMilliseconds(100);

        private readonly StatefulServiceContext context;
        private readonly FabricClient fabricClient;

        // todo: put in config
        private const int MatchSize = 2;
        private const int ActiveActorsThreshold = 100;

        public RequestsService(StatefulServiceContext context)
            : base(context)
        {
            this.context = context;
            this.fabricClient = new FabricClient();
        }

        public Task StartGeneratingRequests(int loopCount)
        {
            actorLoopSpan = TimeSpan.FromMilliseconds(100);
            actorLoopingCount = loopCount;
            return Task.CompletedTask;
        }

        public async Task<ExistingMatch> AddRequestAsync(UserRequest request)
        {
            try
            {
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);
                var requestsQueue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);

                bool hasActor = false;
                ActorInfo actorId = null;

                ExistingMatch existingMatch = null;

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<ActorInfo> assignedActor;
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

        public async Task RemoveActiveMatchAsync(ActorInfo actorInfo)
        {
            try
            {
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorInfo, PlayersInMatch>>(InUseActorsMapName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    bool hasInfo = await usedActors.ContainsKeyAsync(tx, actorInfo);
                    if (hasInfo)
                    {
                        await MatchFinished(tx, actorInfo, null, matchmakedUsers, usedActors);
                    }

                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("RemoveActiveMatchAsync: Exception {0}: {1}", actorInfo, ex));
                throw;
            }
        }

        private async Task<bool> MatchmakeOneGame(CancellationToken cancellationToken, IReliableConcurrentQueue<UserRequest> queue, IReliableDictionary<ActorInfo, PlayersInMatch> usedActors, IReliableDictionary<UserRequest, ActorInfo> matchmakedUsers)
        {
            try
            {
                ConditionalValue<UserRequest> ret;
                PlayersInMatch players = new PlayersInMatch();
                ActorInfo actorId;

                using (var tx = this.StateManager.CreateTransaction())
                {
                    do
                    {
                        ret = await queue.TryDequeueAsync(tx, cancellationToken);
                        if (ret.HasValue)
                        {
                            players = players.AddPlayer(new UserRequest(ret.Value));
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
                    //bool usedActor = false;
                    //do
                    //{
                    //    actorId = ActorId.CreateRandom();
                    //    usedActor = await usedActors.ContainsKeyAsync(tx, actorId);
                    //}
                    //while (!cancellationToken.IsCancellationRequested && usedActor);

                    actorId = await GetSimulationActorId(tx, cancellationToken);

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

                    var playersToAdd = players.GetList();
                    List<UserRequest> addedPlayers = new List<UserRequest>();

                    foreach(var player in playersToAdd)
                    {
                        added = await matchmakedUsers.TryAddAsync(tx, player, actorId);
                        if (added)
                        {
                            addedPlayers.Add(player);
                        }
                    }

                    if (addedPlayers.Count != playersToAdd.Count)
                    {
                        foreach(var player in addedPlayers)
                        {
                            await matchmakedUsers.TryRemoveAsync(tx, player);
                            await queue.EnqueueAsync(tx, player);
                        }
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Some duplicated requests encountered");
                    }

                    await tx.CommitAsync();
                }

                List<UserRequest> playersList = players.GetList();

                // Create actor simulation
                int index = actorId.ActorIndex;

                string suffix = GetSimulationActorNameSuffix(index);
                string actorServiceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{suffix}";
                ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorId.ActorId, new Uri(actorServiceUri));

                bool simulated = await simulationActor.SimulateMatch(playersList, actorId);
                if (!simulated)
                {
                    ServiceEventSource.Current.Message($"Something went wrong with simulation");
                    return false;
                }

                await simulationActor.SubscribeAsync<ISimulationEvents>(this, ResubsriptionInterval);

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

        private async Task InitializeMap()
        {
            var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ActiveActors>>(ActorsCountName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                await map.SetAsync(tx, ActorsCount, new ActiveActors(new List<int>() { 0 }));
                await tx.CommitAsync();
            }

            poolInfo.currentPlacementActor = PopulateActorInfo(0, poolInfo.pool);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await InitializeMap();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                    var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);
                    var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorInfo, PlayersInMatch>>(InUseActorsMapName);

                    bool ret;
                    do
                    {
                        ret = await MatchmakeOneGame(cancellationToken, queue, usedActors, matchmakedUsers);
                    } while (ret);


                    if (lastActorCheck.Add(actorLoopSpan) <= DateTime.Now)
                    {
                        if(actorLoopingCount > 0)
                        {
                            for (int i = 0; i < 200; i++)
                            {
                                await AddRequestAsync(new UserRequest(Guid.NewGuid().ToString(), true));
                            }

                            actorLoopingCount--;
                        }

                        await DeleteUnusuedActors(cancellationToken);

                        lastActorCheck = DateTime.Now;
                    }

                    await Task.Delay(MatchmakeInterval, cancellationToken);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
                }
            }
        }

        #region Callbacks

        public async void StateUpdated(ActorInfo actorInfo, GameState gameState)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, $"Match state updated, ActorId: {actorInfo}, GameState: {gameState.ToString()}");

            string serviceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/WebService";

            IWebService service = ServiceProxy.Create<IWebService>(new Uri(serviceUri));

            await service.GameStateChanged(gameState);
        }

        public async void MatchFinished(ActorInfo actorId, GameState finalGameState)
        {
            try
            {
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorInfo, PlayersInMatch>>(InUseActorsMapName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await MatchFinished(tx, actorId, finalGameState, matchmakedUsers, usedActors);
                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("MatchFinished: Exception {0}: {1}", actorId, ex));
                throw;
            }
        }

        public async Task MatchFinished(ITransaction tx, ActorInfo actorId, GameState finalGameState, IReliableDictionary<UserRequest, ActorInfo> matchmakedUsers, IReliableDictionary<ActorInfo, PlayersInMatch> usedActors)
        {
            ConditionalValue<PlayersInMatch> playersInMatch = await usedActors.TryRemoveAsync(tx, actorId);
            if (!playersInMatch.HasValue)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing in use actors");
            }
            else
            {
                foreach(var player in playersInMatch.Value.Players)
                {
                    ConditionalValue<ActorInfo> remove = await matchmakedUsers.TryRemoveAsync(tx, player);
                    if (!remove.HasValue)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing request, it's not present in dictionary");
                        continue;
                    }
                }
            }

            bool r = poolInfo.pool[actorId.ActorIndex].ActiveActors.Remove(actorId.ActorId);
            if (!r)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Something went wrong, cannot remove actor with id {actorId}");
            }

            ServiceEventSource.Current.ServiceMessage(this.Context, $"Match finished, ActorId: {actorId}");
                
            if(finalGameState != null)
            {
                string serviceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/WebService";

                IWebService service = ServiceProxy.Create<IWebService>(new Uri(serviceUri));

                await service.MatchFinished(finalGameState);
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
        private TimeSpan actorLoopSpan = TimeSpan.FromMilliseconds(100);
        private int actorLoopingCount = 0;

        public class ActorsCountInfo
        {
            public ActorLocationInfo Location;

            public List<ActorId> ActiveActors;

            public int Active => ActiveActors.Count;

            public bool IsAlive()
            {
                return Active > 0;
            }

            public bool HasMoreSpace()
            {
                return Active < ActiveActorsThreshold;
            }
        }

        public class ActorLocationInfo : IComparable, IComparable<ActorLocationInfo>, IEquatable<ActorLocationInfo>
        {
            public int Index;

            public int CompareTo(object obj)
            {
                return CompareTo((ActorLocationInfo)obj);
            }

            public int CompareTo([AllowNull] ActorLocationInfo other)
            {
                var compare = Index.CompareTo(other.Index);
                return compare;
            }

            public bool Equals([AllowNull] ActorLocationInfo other)
            {
                return Index == other.Index;
            }
        }

        private ActorServicePoolInfo poolInfo = new ActorServicePoolInfo();

        public class ActorServicePoolInfo
        {
            public Dictionary<int, ActorsCountInfo> pool;
            public ActorsCountInfo currentPlacementActor;

            public ActorServicePoolInfo()
            {
                pool = new Dictionary<int, ActorsCountInfo>();
                //pool.Add(0, new ActorsCountInfo()
                //{
                //    Location = new ActorLocationInfo() { Index = 0 },
                //    ActiveActors = new List<ActorId>()
                //});
            }

            private List<ActorsCountInfo> GetCurrentLocations()
            {
                return pool.Values.ToList<ActorsCountInfo>();
            }

            public ActorsCountInfo GetActorLocation()
            {
                var current = currentPlacementActor;
                var start = current;

                var infos = GetCurrentLocations();
                int startIndex = infos.FindIndex(x => x.Location == currentPlacementActor.Location);
                int currentIndex = startIndex;

                do
                {
                    var info = infos[currentIndex];
                    if (info.HasMoreSpace())
                    {
                        currentPlacementActor = info;
                        return currentPlacementActor;
                    }

                    currentIndex = (currentIndex + 1) % infos.Count;
                } while (startIndex != currentIndex);

                currentPlacementActor = null;
                return currentPlacementActor;
            }

            public List<int> GetActorsToDelete()
            {
                var toDelete = new List<int>();
                foreach (var pair in pool)
                {
                    bool delete = true;
                    if (currentPlacementActor.Location.Index == pair.Key) continue;

                    if (pair.Value.IsAlive())
                    {
                        delete = false;
                        break;
                    }

                    if (delete)
                    {
                        toDelete.Add(pair.Key);
                    }
                }
                return toDelete;
            }
        }

        private async Task<ActorInfo> GetSimulationActorId(ITransaction tx, CancellationToken cancellationToken)
        {
            await SpawnNewActorsIfNeeded();

            ActorId actorId;
            bool usedActor = false;
            var activeActors = poolInfo.currentPlacementActor.ActiveActors;
            do
            {
                actorId = ActorId.CreateRandom();
                usedActor = activeActors.Contains(actorId);
            }
            while (!cancellationToken.IsCancellationRequested && usedActor);

            poolInfo.currentPlacementActor.ActiveActors.Add(actorId);
            return new ActorInfo(actorId, poolInfo.currentPlacementActor.Location.Index);
        }

        private string GetSimulationActorNameSuffix(int index)
        {
            return index == 0 ? "" : $"{index}";
        }

        private ActorsCountInfo PopulateActorInfo(int index, Dictionary<int, ActorsCountInfo> map)
        {
            if (map.ContainsKey(index))
            {
                return map[index];
            }

            var info = new ActorsCountInfo()
            {
                Location = new ActorLocationInfo()
                {
                    Index = index,
                },
                ActiveActors = new List<ActorId>(),
            };

            map[index] = info;
            return info;
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

        private async Task SpawnNewActorsIfNeeded()
        {
            var newIndexes = new List<int>();
            var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ActiveActors>>(ActorsCountName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var indexes = await map.TryGetValueAsync(tx, ActorsCount);
                if (indexes.HasValue)
                {
                    newIndexes.AddRange(indexes.Value.GetList()); ;
                }

                var actorLocation = poolInfo.GetActorLocation();
                if (actorLocation == null)
                {
                    var newIndex = FindIndex(newIndexes);
                    poolInfo.currentPlacementActor = await CreateActor(newIndex);
                    newIndexes.Add(newIndex);
                }

                await map.TryUpdateAsync(tx, ActorsCount, new ActiveActors(newIndexes), indexes.Value);
                await tx.CommitAsync();
            }
        }

        private async Task DeleteUnusuedActors(CancellationToken cancellationToken)
        {
            var newIndexes = new List<int>();
            var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ActiveActors>>(ActorsCountName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var indexes = await map.TryGetValueAsync(tx, ActorsCount);
                if (indexes.HasValue)
                {
                    newIndexes.AddRange(indexes.Value.GetList()); ;
                }

                var getActorsToDelete = poolInfo.GetActorsToDelete();
                if (getActorsToDelete.Count > 0)
                {
                    foreach (var index in getActorsToDelete)
                    {
                        await DeleteActor(index);
                        newIndexes.Remove(index);
                    }
                }

                await map.TryUpdateAsync(tx, ActorsCount, new ActiveActors(newIndexes), indexes.Value);
                await tx.CommitAsync();
            }
        }

        private async Task<ActorsCountInfo> CreateActor(int index)
        {
            var description = new StatefulServiceDescription()
            {
                HasPersistedState = true,
                ApplicationName = new Uri(this.context.CodePackageActivationContext.ApplicationName),
                ServiceName = new Uri($"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSimulationActorNameSuffix(index)}"),
                ServiceTypeName = "SimulationActorServiceType",
                PartitionSchemeDescription = new UniformInt64RangePartitionSchemeDescription()
                {
                    LowKey = -9223372036854775808,
                    HighKey = 9223372036854775807,
                    PartitionCount = 1,
                },
                MinReplicaSetSize = 1,
                TargetReplicaSetSize = 1,
            };
            await this.fabricClient.ServiceManager.CreateServiceAsync(description);

            return PopulateActorInfo(index, poolInfo.pool);
        }

        private async Task DeleteActor(int index)
        {
            var description = new DeleteServiceDescription(new Uri($"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{GetSimulationActorNameSuffix(index)}"));
            await this.fabricClient.ServiceManager.DeleteServiceAsync(description);

            poolInfo.pool.Remove(index);
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
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorInfo, PlayersInMatch>>(InUseActorsMapName);

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
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorInfo, PlayersInMatch>>(InUseActorsMapName);
                var matchmakedUsers = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorInfo>>(UserToActorMapName);

                await MatchmakeOneGame(cancellationToken, queue, usedActors, matchmakedUsers);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
            }
        }

        #endregion

        #region COmmented out

        //public async Task CheckAllActorsAvailability(CancellationToken cancellationToken, List<int> indexes, Dictionary<int, List<ActorsCountInfo>> map)
        //{
        //    foreach (var index in indexes)
        //    {
        //        await CheckActorAvailability(cancellationToken, index, map);
        //    }
        //}


        //private async Task CheckActorAvailability(CancellationToken cancellationToken, int index, Dictionary<int, List<ActorsCountInfo>> map)
        //{
        //    string suffix = GetSimulationActorNameSuffix(index);
        //    string actorServiceUri = $"{this.context.CodePackageActivationContext.ApplicationName}/SimulationActorService{suffix}";

        //    ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(actorServiceUri));

        //    ContinuationToken continuationToken = null;

        //    if (!map.ContainsKey(index))
        //    {
        //        map[index] = new List<ActorsCountInfo>(partitions.Count);
        //    }

        //    List<ActorInformation> activeActors = new List<ActorInformation>();
        //    List<ActorInformation> inactiveActors = new List<ActorInformation>();

        //    int partitionIndex = 0;
        //    foreach (Partition partition in partitions)
        //    {
        //        var partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;
        //        IActorService actorServiceProxy = ActorServiceProxy.Create(new Uri(actorServiceUri), partitionKey);

        //        activeActors.Clear();
        //        inactiveActors.Clear();

        //        do
        //        {
        //            PagedResult<ActorInformation> page = await actorServiceProxy.GetActorsAsync(continuationToken, cancellationToken);
        //            activeActors.AddRange(page.Items.Where(x => x.IsActive));
        //            inactiveActors.AddRange(page.Items.Where(x => !x.IsActive));
        //            continuationToken = page.ContinuationToken;
        //        } while (continuationToken != null);

        //        var locationInfo = new ActorLocationInfo()
        //        {
        //            Index = index,
        //        };

        //        map[index].Add(new ActorsCountInfo()
        //        {
        //            Location = locationInfo,
        //            ActiveActors = new List<ActorId>(),
        //        });

        //        partitionIndex++;
        //    }
        //}

        //private async Task ManageActors(CancellationToken cancellationToken)
        //{
        //    var newIndexes = new List<int>();
        //    var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ActiveActors>>(ActorsCountName);
        //    using (var tx = this.StateManager.CreateTransaction())
        //    {
        //        var indexes = await map.TryGetValueAsync(tx, ActorsCount);
        //        if (indexes.HasValue)
        //        {
        //            newIndexes.AddRange(indexes.Value.GetList()); ;
        //        }

        //        var actorLocation = poolInfo.GetActorLocation();
        //        if (actorLocation == null)
        //        {
        //            var newIndex = FindIndex(newIndexes);
        //            poolInfo.currentPlacementActor = await CreateActor(newIndex);
        //            newIndexes.Add(newIndex);
        //        }

        //        var getActorsToDelete = poolInfo.GetActorsToDelete();
        //        if (getActorsToDelete.Count > 0)
        //        {
        //            foreach (var index in getActorsToDelete)
        //            {
        //                await DeleteActor(index);
        //                newIndexes.Remove(index);
        //            }
        //        }

        //        await map.TryUpdateAsync(tx, ActorsCount, new ActiveActors(newIndexes), indexes.Value);
        //        await tx.CommitAsync();
        //    }
        //}

        #endregion
    }
}
