using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class BeliefRevisionPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly BeliefRevisionStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;

        public BeliefRevisionPlayer(BeliefRevisionStrategy<MoveT, GameT, GameStateT> strategy = null)
        {
            this.strategy = strategy ?? new BeliefRevisionStrategy<MoveT, GameT, GameStateT>(
                new Dictionary<(GameStateT state, int player, MoveT move), double[]>(),
                new Dictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>>()
            );
        }

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;
            this.player = player;

            if (game.IsSpecialGame) possibleStartingStates = game.GetSpecialInitialStates();

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
            PossibleGameStates = strategy.ProvideClaims(claims, historyId, game, player, PossibleGameStates);
        }
    }

    class BeliefRevisionStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;

        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;
        readonly List<(IEnumerable<int> historyId, IEnumerable<(GameStateT state, double weight)> beliefs)> beliefsCache;

        readonly Stack<(int turn, int sender, int receiver)> trustedClaimOverrides;

        internal BeliefRevisionStrategy(
            IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache,
            IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache,
            List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache = null,
            List<(IEnumerable<int> historyId, IEnumerable<(GameStateT state, double weight)> beliefs)> beliefsCache = null,
            Stack<(int turn, int sender, int receiver)> trustedClaimOverrides = null)
        {
            this.moveValueCache = moveValueCache;
            this.beliefsInStateCache = beliefsInStateCache;
            this.bestMoveCache = bestMoveCache ?? new List<(IEnumerable<int>, IEnumerable<MoveT>)>();
            this.beliefsCache = beliefsCache ?? new List<(IEnumerable<int>, IEnumerable<(GameStateT, double)>)>();

            this.trustedClaimOverrides = trustedClaimOverrides ?? new Stack<(int turn, int sender, int receiver)>();
        }

        internal List<(GameStateT, double)> ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, Func<GameStateT, bool> lieClaim, int id)> claims, IEnumerable<int> historyId, GameT game, int player, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            if (historyId != null && !trustedClaimOverrides.Any())
            {
                var matchInCache = beliefsCache.FirstOrDefault(e => historyId.SequenceEqual(e.historyId)).beliefs;

                if (matchInCache != null) return matchInCache.ToList();
            }

            var turn = possibleGameStates.First().state.Turn;

            var overriddenClaims = claims
                .Where(communication => trustedClaimOverrides.Contains((turn, communication.sender, player)));

            var possibleGameStatesAfterOverride = overriddenClaims.Any() ? ProvidePercepts(state => overriddenClaims.All(communication => communication.claim(state)), possibleGameStates)
                : possibleGameStates.ToList();

            if (overriddenClaims.Count() == claims.Count()) return possibleGameStatesAfterOverride.ToList();

            ///**
            // * For each possible state:
            // * - Determine beliefs of claimer
            // * - Determine whether claim does not maximise expected utility of claimer **if claim is believed**
            // * - If it does not, reduce belief in possible state
            // */

            var remainingClaims = claims.Where(claim => !overriddenClaims.Contains(claim));

            foreach ((var sender, var claim, var lieClaim, var actionId) in claims)
            {
                var claimIsOptimalInSomeState = false;

                for (int i = 0; i < possibleGameStatesAfterOverride.Count(); i++)
                {
                    var (state, weight) = possibleGameStatesAfterOverride.ElementAt(i);

                    var history = game.GetHistory(state);
                    var prevState = turn == 1 ? game.GetInitialStateFromCurrent(state) : history.ElementAt(turn - 2).nextState;
                    var combinedMove = history.ElementAt(turn - 1).combinedMove;
                    var legalMovesByPlayer = prevState.LegalMovesByPlayer;

                    var beliefsOfClaimer = GetBeliefsInRealState(game, prevState, sender);
                    var legalMovesOfClaimer = legalMovesByPlayer.ElementAt(sender);

                    trustedClaimOverrides.Push((turn, sender, player));

                    var bestMovesWithTrust = legalMovesOfClaimer.Maximise(move => GetExpectedUtilitiesWithChoice(game, prevState, sender, move)[sender]);

                    if (!bestMovesWithTrust.Contains(combinedMove.ElementAt(sender))) possibleGameStatesAfterOverride[i] = (state, weight / 1000);
                    else claimIsOptimalInSomeState = true;

                    trustedClaimOverrides.Pop();
                }

                if (!claimIsOptimalInSomeState)
                {
                    possibleGameStatesAfterOverride = possibleGameStatesAfterOverride
                        .Select(belief => claim(belief.state) ? (belief.state, belief.weight * 1000) : belief)
                        .ToList();
                }
            }

            if (historyId != null && !trustedClaimOverrides.Any())
            {
                beliefsCache.Add((historyId.ToList(), possibleGameStatesAfterOverride));
            }

            return possibleGameStatesAfterOverride;
        }

        internal List<(GameStateT state, double weight)> ProvidePercepts(Func<GameStateT, bool> percepts, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            return possibleGameStates
                .Where(possibility => percepts(possibility.state))
                .ToList();
        }

        internal List<MoveT> RequestMoves(IEnumerable<MoveT> legalMoves, IEnumerable<int> historyId, GameT game, int player, IEnumerable<(GameStateT state, double weight)> possibleGameStates)
        {
            if (legalMoves.Count() == 1) return legalMoves.ToList();

            if (historyId != null && !trustedClaimOverrides.Any())
            {
                var matchInCache = bestMoveCache.FirstOrDefault(e => historyId.SequenceEqual(e.historyId)).bestMoves;

                if (matchInCache != null) return matchInCache.ToList();
            }

            double maximumUtilitySum = 0;
            var bestMoves = new List<MoveT>();
            var totalWeight = possibleGameStates.Sum(possibility => possibility.weight);

            foreach (var move in legalMoves)
            {
                double utilitySum = 0;

                foreach ((var state, var weight) in possibleGameStates)
                {
                    utilitySum += weight * GetExpectedUtilitiesWithChoice(game, state, player, move)[player];
                }

                if (utilitySum > maximumUtilitySum)
                {
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    maximumUtilitySum = utilitySum;
                }
                else if (utilitySum == maximumUtilitySum)
                {
                    bestMoves.Add(move);
                }
            }

            if (historyId != null && !trustedClaimOverrides.Any())
            {
                bestMoveCache.Add((historyId.ToList(), bestMoves));
            }

            return bestMoves;
        }

        private double[] GetExpectedUtilitiesByPlayer(GameT game, GameStateT state)
        {
            if (state.IsTerminal) return state.GetUtilities()
                    .Select(x => (double)x)
                    .ToArray();

            var legalMovesByPlayer = state.LegalMovesByPlayer;
            var playersWithChoice = new List<int>();

            for (int player = 0; player < game.NumberOfPlayers; player++)
            {
                if (legalMovesByPlayer.ElementAt(player).Count() > 1) playersWithChoice.Add(player);
            }

            if (playersWithChoice.Count() > 1) throw new NotImplementedException("This system does not yet support simultaneous games");

            var playerWithChoice = playersWithChoice.First();
            var movesChosen = RequestMoves(legalMovesByPlayer.ElementAt(playerWithChoice), null, game, playerWithChoice, GetBeliefsInRealState(game, state, playerWithChoice));

            var utilitySums = new double[game.NumberOfPlayers];

            movesChosen.ForEach(move => {
                var utilities = GetExpectedUtilitiesWithChoice(game, state, playerWithChoice, move);

                for (int player = 0; player < game.NumberOfPlayers; player++) utilitySums[player] += utilities[player];
            });

            return utilitySums
                .Select(sum => sum / movesChosen.Count())
                .ToArray();
        }

        private IEnumerable<(GameStateT state, double weight)> GetBeliefsInRealState(GameT game, GameStateT state, int playerId)
        {
            if (trustedClaimOverrides.Count() == 0 && beliefsInStateCache.TryGetValue((state, playerId), out var cachedBeliefs)) return cachedBeliefs;

            var simulatedPlayer = new BeliefRevisionPlayer<MoveT, GameT, GameStateT>(
                new BeliefRevisionStrategy<MoveT, GameT, GameStateT>(moveValueCache, beliefsInStateCache, bestMoveCache, beliefsCache, trustedClaimOverrides));

            GameManager.ReplayToState(game, state, playerId, simulatedPlayer);

            if (trustedClaimOverrides.Count() == 0) beliefsInStateCache.Add((state, playerId), simulatedPlayer.PossibleGameStates);

            return simulatedPlayer.PossibleGameStates;
        }

        private double[] GetExpectedUtilitiesWithChoice(GameT game, GameStateT state, int player, MoveT move)
        {
            if (trustedClaimOverrides.Count() == 0 && moveValueCache.TryGetValue((state, player, move), out var cachedUtilities)) return cachedUtilities;

            var possibleMovesByPlayer = state.LegalMovesByPlayer.ToList();
            possibleMovesByPlayer[player] = new List<MoveT> { move };

            var combinedMove = possibleMovesByPlayer
                .Select(movesForPlayer => movesForPlayer.First());

            var utilities = GetExpectedUtilitiesByPlayer(game, game.GetStateAfterCombinedMove(state, combinedMove));

            if (trustedClaimOverrides.Count() == 0) moveValueCache.Add((state, player, move), utilities);

            return utilities;
        }
    }
}