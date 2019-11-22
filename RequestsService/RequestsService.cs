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

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class RequestsService : StatefulService, IRequestManager, ISimulationEvents
    {
        private const string UserRequestsQueueName = "userRequestsQueueName";
        //private const string UserToActorMapName = "userToActorMapName";
        private const string InUseActorsMapName = "inUseActorsMapName";

        private static TimeSpan TxTimeout = TimeSpan.FromSeconds(4);
        private static TimeSpan MatchmakeInterval = TimeSpan.FromSeconds(1);

        private readonly StatefulServiceContext context;

        // todo: put in config
        private const int MatchSize = 2;

        public RequestsService(StatefulServiceContext context)
            : base(context)
        {
            this.context = context;
        }

        public async Task AddRequestAsync(UserRequest request)
        {
            try
            {
                //IReliableDictionary<UserRequest, ActorId> inProgressMap =
                //    await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);

                //using (ITransaction tx = this.StateManager.CreateTransaction())
                //{
                //    ConditionalValue<ActorId> assignedActor = await inProgressMap.TryGetValueAsync(tx, request);

                //    if (assignedActor.HasValue)
                //    {
                //        ServiceEventSource.Current.Message($"User request {request } has been discared because user has already started a match!");
                //        // todo: send actorId to user
                //        return;
                //    }
                //}

                // Add user to waiting queue
                var requestsQueue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await requestsQueue.EnqueueAsync(tx, request);
                    await tx.CommitAsync();
                }

                ServiceEventSource.Current.Message($"User request {request} added to the queue!");
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

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        public async Task Matchmake(CancellationToken cancellationToken)
        {
            try
            {
                var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, List<UserRequest>>>(InUseActorsMapName);
                
                await MatchmakeOneGame(cancellationToken, queue, usedActors);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
            }
        }

        //protected override async Task RunAsync(CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
        //        //var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
        //        var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, List<UserRequest>>>(InUseActorsMapName);

        //        while (!cancellationToken.IsCancellationRequested)
        //        {
        //            bool ret;
        //            do
        //            {
        //                ret = await MatchmakeOneGame(cancellationToken, queue, usedActors);
        //            } while (ret);

        //            await Task.Delay(MatchmakeInterval, cancellationToken);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
        //    }
        //}

        private async Task<bool> MatchmakeOneGame(CancellationToken cancellationToken, IReliableConcurrentQueue<UserRequest> queue, IReliableDictionary<ActorId, List<UserRequest>> usedActors)
        {
            try
            {
                //var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<UserRequest>>(UserRequestsQueueName);
                //var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                //var usedActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, List<UserRequest>>>(InUseActorsMapName);

                ConditionalValue<UserRequest> ret;
                List<UserRequest> players = new List<UserRequest>();
                ActorId actorId;

                using (var tx = this.StateManager.CreateTransaction())
                {
                    do
                    {
                        ret = await queue.TryDequeueAsync(tx, cancellationToken);
                        if (ret.HasValue)
                        {
                            players.Add(ret.Value);
                        }
                    }
                    while (!cancellationToken.IsCancellationRequested && ret.HasValue && players.Count < MatchSize);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Cancellation requested!");
                        tx.Abort();
                        return false;
                    }

                    if(players.Count != MatchSize)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Not enough players in the queue to matchmake!");
                        tx.Abort();
                        return false;
                    }

                    // found enough players - assign them actor
                    bool usedActor = false;
                    do
                    {
                        actorId = ActorId.CreateRandom();

                        ConditionalValue<List<UserRequest>> inUse = await usedActors.TryGetValueAsync(tx, actorId);
                        usedActor = inUse.HasValue;
                    }
                    while (!cancellationToken.IsCancellationRequested && usedActor);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Cancellation requested!");
                        tx.Abort();
                        return false;
                    }

                    await usedActors.TryAddAsync(tx, actorId, players);

                    //for (int i = 0; i < players.Count; i++)
                    //{
                    //    bool added = await map.TryAddAsync(tx, players[i], actorId);
                    //    if (!added)
                    //    {
                    //        ServiceEventSource.Current.ServiceMessage(this.Context, $"Tried to add already existing request {players[i]}");
                    //        continue;
                    //    }
                    //}

                    await tx.CommitAsync();
                }

                string actorServiceUri = $"{this.Context.CodePackageActivationContext.ApplicationName}/SimulationActorService";

                ISimulationActor simulationActor = ActorProxy.Create<ISimulationActor>(actorId, new Uri(actorServiceUri));

                await simulationActor.SimulateMatch(players);
                await simulationActor.SubscribeAsync<ISimulationEvents>(this);

                StringBuilder builder = new StringBuilder();
                builder.Append($"Created new match\n ActorID: {actorId}\n");

                for (int i = 0; i < players.Count; i++)
                {
                    builder.Append($"UserID_{i}: {players[i]}\n");
                }

                ServiceEventSource.Current.ServiceMessage(this.Context, builder.ToString());

                players.Clear();

                return true;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(string.Format("Exception {0}", ex));
                return false;
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
                //var map = await this.StateManager.GetOrAddAsync<IReliableDictionary<UserRequest, ActorId>>(UserToActorMapName);
                var userActors = await this.StateManager.GetOrAddAsync<IReliableDictionary<ActorId, List<UserRequest>>>(InUseActorsMapName);

                using (var tx = this.StateManager.CreateTransaction())
                {
                    //for(int i = 0; i < userIds.Count; i++)
                    //{
                    //    ConditionalValue<ActorId> remove = await map.TryRemoveAsync(tx, userIds[i]);
                    //    if (!remove.HasValue)
                    //    {
                    //        ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing request, it's not present in dictionary");
                    //        continue;
                    //    }
                    //}

                    ConditionalValue<List<UserRequest>> players = await userActors.TryRemoveAsync(tx, actorId);
                    if (!players.HasValue)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Somethings went wrong while removing in use actors");
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

        #endregion
    }
}
