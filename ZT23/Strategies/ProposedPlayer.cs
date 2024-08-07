using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;

namespace GamePlayer
{
    class ProposedPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly ProposedStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;
        internal Boolean isCooperative = false;
        internal List<Func<GameStateT, bool>> claimsSent = new List<Func<GameStateT, bool>>();
        internal List<Func<GameStateT, bool>> claimsReceived = new List<Func<GameStateT, bool>>();

        public ProposedPlayer(ProposedStrategy<MoveT, GameT, GameStateT> strategy = null)
        {
            this.strategy = strategy ?? new ProposedStrategy<MoveT, GameT, GameStateT>(
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

            claimsSent = new List<Func<GameStateT, bool>>();
            claimsReceived = new List<Func<GameStateT, bool>>();

            historyId = new List<int>(possibleStartingStates.Select(e => e.id));
            historyId.Sort();
        }

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves)
        {
            // Check whether in the current state the game is cooperative
            var isCooperative = strategy.IsCooperative(game, PossibleGameStates, player);

            // If cooperative, then apply all the claim made by the opponent
            if (isCooperative)
            {
                PossibleGameStates = PossibleGameStates
                    .Where(possibility => claimsReceived.All(claim => claim(possibility.state)))
                    .ToList();
            }

            // Choose a move
            var move = random.Choose(strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates, claimsSent, claimsReceived));

            historyId.Add(-1 - move.GetIndex());

            if (game.IsClaim(move, out var receivers, out var claim, out var lieClaim, out var id))
            {
                claimsSent.Add(claim);
            }

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

        public IEnumerable<MoveT> GetAllBestMoves(IEnumerable<MoveT> legalMoves) => strategy.RequestMoves(legalMoves, historyId, game, player, PossibleGameStates, 
                                                                                    claimsSent, claimsReceived);

        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id)
        {
            historyId.Add(id);
            PossibleGameStates = strategy.ProvidePercepts(percepts, PossibleGameStates);
        }

        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, Func<GameStateT, bool> lieClaim, int id)> claims)
        {
            foreach (var (_, _, _, id) in claims) historyId.Add(id);
            // Add the claims to the claimsReceived list
            if (claimsReceived == null) claimsReceived = new List<Func<GameStateT, bool>>();
            foreach (var claim in claims) claimsReceived.Add(claim.claim);
            //PossibleGameStates = strategy.ProvidePercepts(state => claims.All(communication => communication.claim(state)), PossibleGameStates);
        }
    }

    class ProposedStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;

        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;

        internal ProposedStrategy(
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

            var isCooperative = IsCooperative(game, possibleGameStates, player);

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
                    utilitySum += weight * GetExpectedUtilities(game, state, player, move, isCooperative, claimsSent, claimsReceived)[player];
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

        internal Boolean IsCooperative(GameT game, IEnumerable<(GameStateT state, double weight)> possibleGameStates, int player)
        {
            var enemyPlayer = player == 1 ? 0 : 1;
            var cooperative = true;

            // Iterate through all possible game states
            // If all possible game states are cooperative, then the current information set is cooperative
            foreach (var state in possibleGameStates)
            {
                var legalMoves = state.state.LegalMovesByPlayer;
                var stateCooperative = true;
                // If in the current state, the player only have 1 choices of actions, change the player
                if (legalMoves.ElementAt(player).Count() == 1)
                {
                    var temp = enemyPlayer;
                    enemyPlayer = player;
                    player = temp;
                }   

                foreach (var enemyMove in legalMoves.ElementAt(enemyPlayer))
                {
                    Boolean currentMoveCooperative = true;
                    List<double[]> possibleUtilities = new List<double[]>();
                    foreach (var ourMove in legalMoves.ElementAt(player))
                    {
                        var combinedMove = new List<MoveT> { ourMove, enemyMove };
                        combinedMove[player] = ourMove;
                        combinedMove[enemyPlayer] = enemyMove;

                        var nextState = game.GetStateAfterCombinedMove(state.state, combinedMove);

                        if (nextState.IsTerminal)
                        {
                            var currentUtility = nextState.GetUtilities()
                                .Select(x => (double)x)
                                .ToArray();
                            possibleUtilities.Add(currentUtility);
                        }
                        else
                        {
                            var nextPossibleStates = new List<(GameStateT state, double weight)>
                            {
                                (nextState, 1)
                            };
                            currentMoveCooperative = IsCooperative(game, nextPossibleStates, player);
                            if (currentMoveCooperative == false) break;
                        }
                    }

                    // Check if there exist a combined move with lower utility for both player
                    // If it exist then, then if the enemy make this move, the game is cooperative
                    for (var i = 0; i < possibleUtilities.Count(); i++)
                    {
                        for (var j = i + 1; j < possibleUtilities.Count(); j++)
                        {
                            if (i == j) continue;
                            if (!((possibleUtilities[i][player] < possibleUtilities[j][player] 
                                && possibleUtilities[i][enemyPlayer] < possibleUtilities[j][enemyPlayer])
                                || (possibleUtilities[i][player] > possibleUtilities[j][player] 
                                && possibleUtilities[i][enemyPlayer] > possibleUtilities[j][enemyPlayer])))
                            {
                                currentMoveCooperative = false;
                                break;
                            }
                        }
                    }
                    if (!currentMoveCooperative)
                    {
                        stateCooperative = false;
                        break;
                    }
                }

                if (!stateCooperative)
                {
                    cooperative = false;
                    break;
                }
            }

            return cooperative;
        }

        private IEnumerable<(GameStateT state, double weight)> GetBeliefsInRealState(GameT game, GameStateT state, int playerId)
        {
            // Get the possible game states of a player in a specific state
            if (beliefsInStateCache.TryGetValue((state, playerId), out var cachedBeliefs)) return cachedBeliefs;

            var simulatedPlayer = new ProposedPlayer<MoveT, GameT, GameStateT>(
                new ProposedStrategy<MoveT, GameT, GameStateT>(moveValueCache, beliefsInStateCache, bestMoveCache));

            GameManager.ReplayToState(game, state, playerId, simulatedPlayer);

            beliefsInStateCache.Add((state, playerId), simulatedPlayer.PossibleGameStates);

            return simulatedPlayer.PossibleGameStates;
        }

        private double[] GetExpectedUtilities(GameT game, GameStateT state, int player, MoveT move, bool isCooperative,
                                            List<Func<GameStateT, bool>> claimsSent, List<Func<GameStateT, bool>> claimsReceived)
        {
            if (moveValueCache.TryGetValue((state, player, move), out var cachedUtilities)) return cachedUtilities;

            var _claimsSent = new List<Func<GameStateT, bool>>(claimsSent);

            // Check if the move is a claim
            if (game.IsClaim(move, out var receivers, out var claim, out var lieClaim, out var id))
            {
                _claimsSent.Add(claim);
            }

            // If the claim sent is not true to the current state, return 0 utilities and the game is not cooperative
            if (isCooperative & _claimsSent.Any() && !_claimsSent.Last()(state))
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
                        _claimsReceived.Add(claim);
                    }

                    // If the game is cooperative, apply the claims sent and received to the next state
                    // If the next state does not fulfill, then do not calculate the expected utilities of the next state
                    var nextStateFulfill = true;
                    if (isCooperative)
                    {
                        if (_claimsReceived.Any())
                        {
                            foreach (var claimReceived in _claimsReceived)
                            {
                                if (!claimReceived(nextState))
                                {
                                    nextStateFulfill = false;
                                    break;
                                }
                            }
                        }
                        if (_claimsSent.Any() && nextStateFulfill)
                        {
                            foreach (var claimSent in _claimsSent)
                            {
                                if (!claimSent(nextState))
                                {
                                    nextStateFulfill = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (!nextStateFulfill) continue;

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
                        var nextStateIsCooperative = IsCooperative(game, nextPossibleState, player);

                        // Calculate the expected utilities of each best moves and sum them
                        nextMoves.ForEach(nextMove =>
                        {
                            var utilities = GetExpectedUtilities(game, nextState, player, nextMove, nextStateIsCooperative, _claimsSent, _claimsReceived);

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
