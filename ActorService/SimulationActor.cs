namespace MyActorService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::MyActorService.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    //[ActorService(Name = nameof(SimulationActor))]
    [StatePersistence(StatePersistence.Persisted)]
    internal class SimulationActor : Actor, ISimulationActor
    {
        private IActorTimer _updateTimer;

        private const string GameStateName = "GameState";
        // todo: make configurable
        private const int GameDurationSec = 10;

        /// <summary>
        /// Initializes a new instance of SimulationActorService
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public SimulationActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization
            await base.OnActivateAsync();
        }

        public async Task SimulateMatch(List<UserRequest> players)
        {
            //_updateTimer = RegisterTimer(Update, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            bool added = await this.StateManager.TryAddStateAsync<GameState>(GameStateName, new GameState(players));

            if (!added)
            {
                // value already exists, which means processing has already started.
                throw new InvalidOperationException("Processing for this actor has already started.");
            }
        }

        public async Task ApplyInput(UserRequest user, UserInput input)
        {
            ConditionalValue<GameState> gameState = await this.StateManager.TryGetStateAsync<GameState>(GameStateName);

            if (!gameState.HasValue)
            {
                // value doesn't exists
                throw new InvalidOperationException("Cannot find game state.");
            }

            ActorEventSource.Current.ActorMessage(this, $"Processing user: {user.ToString()}. Current input: {input.ToString()}");

            var playerState = gameState.Value.GetPlayerState(user);
            if (playerState == null)
            {
                ActorEventSource.Current.ActorMessage(this, $"Cannot find user {user}");
                return;
            }

            ApplyInput(playerState, input); 
            
            await this.StateManager.SetStateAsync<GameState>(GameStateName, gameState.Value);
        }

        private void ApplyInput(PlayerState state, UserInput input)
        {
            switch (input.Input)
            {
                case 0:
                    state.Value += 100;
                    break;
                case 1:
                    state.Value -= 100;
                    break;
                case 2:
                    state.Value *= 100;
                    break;
                case 3:
                    state.Value /= 100;
                    break;
                default:
                    break;
            }
        }

        private async Task Update(object state)
        {
            var gameState = await this.StateManager.GetStateAsync<GameState>(GameStateName);

            ActorEventSource.Current.ActorMessage(this, $"Processing actorID: {this.Id}. Current value: {gameState.ToString()}");

            //foreach (PlayerState player in gameState.State)
            //{
            //    player.Value++;
            //}

            gameState.GameTimeSec++;

            await this.StateManager.SetStateAsync<GameState>(GameStateName, gameState);

            var newState = await this.StateManager.GetStateAsync<GameState>(GameStateName);

            ActorEventSource.Current.ActorMessage(this, $"ActorID: {this.Id}. New value: {newState.ToString()}");

            var ev = GetEvent<ISimulationEvents>();
            ev.StateUpdated(this.Id, newState);

            if(newState.GameTimeSec == GameDurationSec)
            {
                ev.MatchFinished(this.Id, newState);

                await this.StateManager.TryRemoveStateAsync(GameStateName);

                UnregisterTimer(_updateTimer);
            }
        }

        public async Task<UserRequest> GetOpponent(UserRequest user)
        {
            var gameState = await this.StateManager.GetStateAsync<GameState>(GameStateName);

            var opponent = gameState.GetOpponentPlayerState(user);

            return opponent.User;
        }

        public async Task<GameState> FighterDead(UserRequest user)
        {
            var gameState = await this.StateManager.GetStateAsync<GameState>(GameStateName);

            var ev = GetEvent<ISimulationEvents>();
            ev.MatchFinished(this.Id, gameState);

            await this.StateManager.TryRemoveStateAsync(GameStateName);

            return gameState;
        }
    }
}
