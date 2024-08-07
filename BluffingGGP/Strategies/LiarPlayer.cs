using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GamePlayer
{
    class LiarPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly LiarStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;
        internal Boolean isCooperative = false;
        internal List<Func<GameStateT, bool>> claimsSent = new List<Func<GameStateT, bool>>();
        internal List<Func<GameStateT, bool>> claimsReceived = new List<Func<GameStateT, bool>>();

        public LiarPlayer(LiarStrategy<MoveT, GameT, GameStateT> strategy = null)
        {
            this.strategy = strategy ?? new LiarStrategy<MoveT, GameT, GameStateT>(
                new Dictionary<(GameStateT state, int player, MoveT move), double[]>(),
                new Dictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>>()
            );
        }

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;
            this.player = player;

            // Save the possible starting states
            PossibleGameStates = possibleStartingStates
                .Select(stateAndWeight => (stateAndWeight.state, 1000d * stateAndWeight.weight))
                .ToList();
            
            // Initiate empty claimsSent and claimsReceived list
            claimsSent = new List<Func<GameStateT, bool>>();
            claimsReceived = new List<Func<GameStateT, bool>>();

            historyId = new List<int>(possibleStartingStates.Select(e => e.id));
            historyId.Sort();
        }

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves)
        {
            // Choose a move
            var move = random.Choose(strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates, claimsSent, claimsReceived));

            historyId.Add(-1 - move.GetIndex());

            // Advance the game state
            PossibleGameStates = PossibleGameStates
                .Select(possibility => {
                    var possibleNextStates = game.GetPossibleStatesAfterMove(possibility.state, player, move);
                    return possibleNextStates.Select(state => (state, possibility.weight / possibleNextStates.Count()));
                })
                .Flatten()
                .ToList();

            if (game.IsClaim(move, out var receivers, out var claim, out var lieClaim, out var id))
            {
                claimsSent.Add(lieClaim);
            }

            return move;
        }

        public IEnumerable<MoveT> GetAllBestMoves(IEnumerable<MoveT> legalMoves) => strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates, 
                                                                                    claimsSent, claimsReceived);

        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id)
        {
            historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(percepts, PossibleGameStates);
        }

        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, Func<GameStateT, bool> lieClaim, int id)> claims) {
            foreach (var (_, _, _, id) in claims) historyId.Add(id);

            // Add the claims to the claimsReceived list
            if (claimsReceived == null) claimsReceived = new List<Func<GameStateT, bool>>();
            foreach (var claim in claims) claimsReceived.Add(claim.lieClaim);

            // Apply the claim to the possible states, but do not trust every claims
            //PossibleGameStates = strategy.ProvidePercepts(state => claims.All(communication => !communication.claim(state)), PossibleGameStates);
        }
    }

    class LiarStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;

        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;

        internal LiarStrategy(
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
                                        IEnumerable<(GameStateT state, double weight)> possibleGameStates,
                                        List<Func<GameStateT, bool>> claimsSent, List<Func<GameStateT, bool>> claimsReceived)
        {
            if (legalMoves.Count() == 1) return legalMoves.ToList();

            if (historyId != null)
            {
                var matchInCache = bestMoveCache.FirstOrDefault(e => historyId.SequenceEqual(e.historyId)).bestMoves;

                if (matchInCache != null) return matchInCache.ToList();
            }

            double maximumUtilitySum = 0;
            var bestMoves = new List<MoveT>();
            var totalWeight = possibleGameStates.Sum(possibility => possibility.weight);

            foreach (var move in legalMoves)
            {
                // Get expected utilities for each moves available
                double utilitySum = 0;
                
                // For each current possible game state, calculate the expected utility of the move
                foreach ((var state, var weight) in possibleGameStates)
                {
                    utilitySum += weight * GetExpectedUtilities(game, state, player, move, claimsSent, claimsReceived)[player];
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

            if (historyId != null)
            {
                bestMoveCache.Add((historyId.ToList(), bestMoves));
            }

            return bestMoves;
        }

        private IEnumerable<(GameStateT state, double weight)> GetBeliefsInRealState(GameT game, GameStateT state, int playerId)
        {
            // Get the possible game states of a player in a specific state
            if (beliefsInStateCache.TryGetValue((state, playerId), out var cachedBeliefs)) return cachedBeliefs;

            var simulatedPlayer = new LiarPlayer<MoveT, GameT, GameStateT>(
                new LiarStrategy<MoveT, GameT, GameStateT>(moveValueCache, beliefsInStateCache, bestMoveCache));

            GameManager.ReplayToState(game, state, playerId, simulatedPlayer);

            beliefsInStateCache.Add((state, playerId), simulatedPlayer.PossibleGameStates);

            return simulatedPlayer.PossibleGameStates;
        }

        private double[] GetExpectedUtilities(GameT game, GameStateT state, int player, MoveT move,
                                            List<Func<GameStateT, bool>> claimsSent, List<Func<GameStateT, bool>> claimsReceived)
        {
            if (moveValueCache.TryGetValue((state, player, move), out var cachedUtilities)) return cachedUtilities;

            var _claimsSent = new List<Func<GameStateT, bool>>(claimsSent);

            // Check if the move is a claim
            if (game.IsClaim(move, out var receivers, out var claim, out var lieClaim, out var id))
            {
                _claimsSent.Add(lieClaim);
            }

            // If the claim sent is true to the current state, return 0 utilities
            if (_claimsSent.Any() && !_claimsSent.Last()(state))
            {
                moveValueCache.Add((state, player, move), new double[game.NumberOfPlayers]);
                return new double[game.NumberOfPlayers];
            }

            var possibleMovesByPlayer = state.LegalMovesByPlayer.ToList();
            var utilitySums = new double[game.NumberOfPlayers];

            // Get the opponent moves
            for (int playerIndex = 0; playerIndex < game.NumberOfPlayers; playerIndex++)
            {
                if (playerIndex == player) continue;

                // Loop through the opponent different moves
                var tempPossibleMovesByPlayer = state.LegalMovesByPlayer.ToList(); ;
                tempPossibleMovesByPlayer[player] = new List<MoveT> { move };
                for (int playerMove = 0; playerMove < possibleMovesByPlayer[playerIndex].Count(); playerMove++)
                {
                    var _claimsReceived = new List<Func<GameStateT, bool>>(claimsReceived);
                    // Get the combined move and the next state of the combined move
                    tempPossibleMovesByPlayer[playerIndex] = new List<MoveT> { possibleMovesByPlayer[playerIndex].ElementAt(playerMove) };

                    var combinedMove = tempPossibleMovesByPlayer
                        .Select(movesForPlayer => movesForPlayer.First());

                    var nextState = game.GetStateAfterCombinedMove(state, combinedMove);
                    var nextUtilities = new double[game.NumberOfPlayers];

                    // If opponent move is a claim, save the claim
                    if (game.IsClaim(possibleMovesByPlayer[playerIndex].ElementAt(playerMove), out receivers, out claim, out lieClaim, out id))
                    {
                        _claimsReceived.Add(lieClaim);
                    }

                    // Iterate through all claims made, do not trust all of them
                    if (_claimsReceived.Any() && !_claimsReceived.Last()(nextState))
                    {
                        nextUtilities = new double[] {0, 0};
                        for (int i = 0; i < game.NumberOfPlayers; i++) utilitySums[i] += nextUtilities[i];
                        continue;
                    }

                    if (_claimsSent.Any() && !_claimsSent.Last()(nextState))
                    {
                        nextUtilities = new double[] { 0, 0 };
                        for (int i = 0; i < game.NumberOfPlayers; i++) utilitySums[i] += nextUtilities[i];
                        continue;
                    }

                    // If the next state is terminal, get the utilities, 
                    // otherwise get the expected utilities of the next state
                    if (nextState.IsTerminal)
                    {
                        nextUtilities = nextState.GetUtilities()
                                        .Select(x => (double)x)
                                        .ToArray();
                    }
                    else
                    {
                        // Get the best moves in the next state
                        var nextMoves = RequestMoves(nextState.LegalMovesByPlayer.ElementAt(player), null, game, 
                                                    player, GetBeliefsInRealState(game, nextState, player),
                                                    _claimsSent, _claimsReceived);
                        nextUtilities = new double[game.NumberOfPlayers];
                        var nextPossibleState = new List<(GameStateT state, double weight)>
                        {
                            (nextState, 1)
                        };

                        // Calculate the expected utilities of each best moves and sum them
                        nextMoves.ForEach(nextMove =>
                        {
                            var utilities = GetExpectedUtilities(game, nextState, player, nextMove, _claimsSent, _claimsReceived);

                            for (int i = 0; i < game.NumberOfPlayers; i++) nextUtilities[i] += utilities[i];
                        });

                        // Average the utilities
                        nextUtilities = nextUtilities
                            .Select(sum => sum / nextMoves.Count())
                            .ToArray();
                    }
                    for (int i = 0; i < game.NumberOfPlayers; i++) utilitySums[i] += nextUtilities[i];
                }
                utilitySums = utilitySums
                    .Select(sum => sum / possibleMovesByPlayer[playerIndex].Count())
                    .ToArray();
            }

            moveValueCache.Add((state, player, move), utilitySums);

            return utilitySums;
        }
    }
}
