using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GamePlayer
{
    class BluffingPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly BluffingStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;

        public BluffingPlayer(BluffingStrategy<MoveT, GameT, GameStateT> strategy = null)
        {
            this.strategy = strategy ?? new BluffingStrategy<MoveT, GameT, GameStateT>(
                new Dictionary<(GameStateT state, int player, MoveT move), double[]>(),
                new Dictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>>()
            );
        }

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;
            this.player = player;

            PossibleGameStates = possibleStartingStates
                .Select(stateAndWeight => (stateAndWeight.state, 1000d * stateAndWeight.weight))
                .ToList();

            historyId = new List<int>(possibleStartingStates.Select(e => e.id));
            historyId.Sort();
        }

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves)
        {
            // Choose a move
            var move = random.Choose(strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates));

            historyId.Add(-1 - move.GetIndex());

            // Advance the game state
            PossibleGameStates = PossibleGameStates
                .Select(possibility => {
                    var possibleNextStates = game.GetPossibleStatesAfterMove(possibility.state, player, move);
                    return possibleNextStates.Select(state => (state, possibility.weight / possibleNextStates.Count()));
                })
                .Flatten()
                .ToList();

            return move;
        }

        public IEnumerable<MoveT> GetAllBestMoves(IEnumerable<MoveT> legalMoves) => strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates);

        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id)
        {
            historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(percepts, PossibleGameStates);
        }

        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, Func<GameStateT, bool> lieClaim, int id)> claims)
        {
            foreach (var (_, _, _, id) in claims) historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(state => claims.All(communication => communication.claim(state)), PossibleGameStates);
        }
    }

    class BluffingStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;

        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;

        internal BluffingStrategy(
            IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache,
            IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache,
            List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache = null)
        {
            this.moveValueCache = moveValueCache;
            this.beliefsInStateCache = beliefsInStateCache;
            this.bestMoveCache = bestMoveCache ?? new List<(IEnumerable<int>, IEnumerable<MoveT>)>();
        }

        internal List<(GameStateT state, double weight)> ProvidePercepts(Func<GameStateT, bool> percepts, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            return possibleGameStates
                .Where(possibility => percepts(possibility.state))
                .ToList();
        }

        internal List<MoveT> RequestMoves(IEnumerable<MoveT> legalMoves, IEnumerable<int> historyId, GameT game, int player, 
                                        IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            if (legalMoves.Count() == 1) return legalMoves.ToList();

            if (historyId != null)
            {
                var matchInCache = bestMoveCache.FirstOrDefault(e => historyId.SequenceEqual(e.historyId)).bestMoves;

                if (matchInCache != null) return matchInCache.ToList();
            }

            var bestMoves = possibleGameStates.First().state.BluffingMoves(player);

            if (historyId != null)
            {
                bestMoveCache.Add((historyId.ToList(), bestMoves));
            }

            return bestMoves;
        }
    }
}
