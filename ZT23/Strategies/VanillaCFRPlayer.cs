using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GamePlayer
{
    class VanillaCFRPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();
        readonly VanillaCFRStrategy<MoveT, GameT, GameStateT> strategy;

        GameT game;
        int player;
        internal IEnumerable<(GameStateT state, double weight)> PossibleGameStates { get; private set; }
        internal List<int> historyId;
        internal Boolean isCooperative = false;
        internal List<Func<GameStateT, bool>> claimsSent = new List<Func<GameStateT, bool>>();
        internal List<Func<GameStateT, bool>> claimsReceived = new List<Func<GameStateT, bool>>();
        internal bool Initiate = true;
        internal int iter = 100;

        public VanillaCFRPlayer(VanillaCFRStrategy<MoveT, GameT, GameStateT> strategy = null, int iter = 100)
        {
            this.strategy = strategy ?? new VanillaCFRStrategy<MoveT, GameT, GameStateT>(
                new Dictionary<(GameStateT state, int player, MoveT move), double[]>(),
                new Dictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>>()
            );
            this.iter = iter;
        }

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates)
        {
            this.game = game;
            this.player = player;

            // Iterate through the VanillaCFR to get the strategy
            if (Initiate)
            {
                var ev = new List<double[]>();
                for (int i = 0; i < iter; i++)
                {
                    var temp = this.strategy.InitiateVanillaCFR(game);
                    foreach (var infoSet in strategy.informationSets.Values)
                    {
                        infoSet.NextStrategy();
                    }
                    ev.Add(temp);
                }
                Initiate = false;
            }

            PossibleGameStates = possibleStartingStates
                .Select(stateAndWeight => (stateAndWeight.state, 1000d * stateAndWeight.weight))
                .ToList();

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

    class VanillaCFRStrategy<MoveT, GameT, GameStateT>
        where MoveT : Enum
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache;
        readonly IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache;
        readonly List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache;
        internal Dictionary<String, InformationSet> informationSets;
        readonly Random random = new Random();

        internal VanillaCFRStrategy(
            IDictionary<(GameStateT state, int player, MoveT move), double[]> moveValueCache,
            IDictionary<(GameStateT realState, int player), IEnumerable<(GameStateT state, double weight)>> beliefsInStateCache,
            List<(IEnumerable<int> historyId, IEnumerable<MoveT> bestMoves)> bestMoveCache = null)
        {
            this.moveValueCache = moveValueCache;
            this.beliefsInStateCache = beliefsInStateCache;
            this.bestMoveCache = bestMoveCache ?? new List<(IEnumerable<int>, IEnumerable<MoveT>)>();
            informationSets = new Dictionary<string, InformationSet>();
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

            // Get the information set of the state
            var state = possibleGameStates.First().state;
            var key = state.GetKey(player);
            if (!informationSets.ContainsKey(key))
            {
                Console.WriteLine("ERROR");
                return legalMoves.ToList();
            }
            var informationSet = informationSets[key];
            var strategy = informationSet.strategy;
            var maxStrategy = strategy.Max();
            var bestMoves = new List<MoveT>();
            for (int i = 0; i < legalMoves.Count(); i++)
            {
                if (strategy[i] == maxStrategy) bestMoves.Add(legalMoves.ElementAt(i));
            }

            if (historyId != null)
            {
                bestMoveCache.Add((historyId.ToList(), bestMoves));
            }

            return bestMoves;
        }

        private double[] IterateVanillaCFR(GameT game, GameStateT state, double prob_1, double prob_2, double prob_3)
        {
            // If terminal state, return the utility
            if (state.IsTerminal) return state.GetUtilities()
                                    .Select(x => (double)x)
                                    .ToArray();

            // Get the player with action in the current turn
            var legalMoves = state.LegalMovesByPlayer;
            var player = state.PlayingPlayer;

            // Get the information set
            var infoSet = GetInformationSet(game, state, player);
            var strategy = infoSet.strategy;

            // Update the probability to reach current state
            if (player == 0)
            {
                infoSet.reachPr += prob_1;
            } 
            else
            {
                infoSet.reachPr += prob_2;
            }

            // Get the counterfactual utility of each action
            var finalUtils = new double[game.NumberOfPlayers];
            var actionUtils = new double[infoSet.totalMove];
            for (int i = 0; i < infoSet.totalMove; i++)
            {
                var combinedMove = new List<MoveT>(new MoveT[game.NumberOfPlayers]);
                combinedMove[player] = legalMoves.ElementAt(player).ElementAt(i);
                combinedMove[1 - player] = legalMoves.ElementAt(1 - player).First();
                // Update the probability to reach the next state
                var nextProb_1 = prob_1;
                var nextProb_2 = prob_2;
                if (player == 0)
                {
                    nextProb_1 *= strategy[i];
                }
                else
                {
                    nextProb_2 *= strategy[i];
                }
                var nextStateUtilities = IterateVanillaCFR(game, game.GetStateAfterCombinedMove(state, combinedMove), nextProb_1, nextProb_2, prob_3);
                actionUtils[i] = nextStateUtilities[player];
                // Update the final utilities
                for (int j = 0; j < game.NumberOfPlayers; j++)
                {
                    finalUtils[j] += strategy[i] * nextStateUtilities[j];
                }
            }
            var totalUtils = finalUtils[player];
            for (int i = 0; i < infoSet.totalMove; i++)
            {
                var regret = actionUtils[i] - totalUtils;
                if (player == 0)
                {
                    infoSet.regretSum[i] += prob_2 * prob_3 * regret;
                }
                else
                {
                    infoSet.regretSum[i] += prob_1 * prob_3 * regret;
                }
            }

            return finalUtils;
        }

        internal double[] InitiateVanillaCFR(GameT game)
        {
            // Get all possible game states of the game and iterate it through the VanillaCFR
            var possibleInitialStates = game.GetInitialStates();
            var test = new double[game.NumberOfPlayers];
            foreach (var initialState in possibleInitialStates)
            {
                var temp = IterateVanillaCFR(game, initialState.state, 1.0, 1.0, 1.0 / possibleInitialStates.Count());
                test[0] += temp[0];
                test[1] += temp[1];
            }
            test[0] /= possibleInitialStates.Count();
            test[1] /= possibleInitialStates.Count();
            return test;
        }

        private InformationSet GetInformationSet(GameT game, GameStateT state, int player)
        {
            InformationSet informationSet;
            String key = state.GetKey(player);
            if (informationSets.ContainsKey(key))
            {
                informationSet = informationSets[key];
            }
            else
            {
                informationSet = state.GetInformationSet(player);
                informationSets.Add(key, informationSet);
            }
            return informationSet;
        }
    }

    
}
