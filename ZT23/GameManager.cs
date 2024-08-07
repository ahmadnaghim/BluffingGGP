using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamePlayer
{
    abstract class GameManager
    {
        static readonly Random random = new Random();

        public static GameStateT Play<MoveT, GameT, GameStateT>(GameT game, params IPlayer<MoveT, GameT, GameStateT>[] players)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            return PlayWithInitialState(game, random.ChooseWithWeights(game.GetInitialStates()), players);
        }

        public static GameStateT PlayWithFilteredInitialStates<MoveT, GameT, GameStateT>(GameT game, Func<GameStateT, bool> filter, params IPlayer<MoveT, GameT, GameStateT>[] players)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            return PlayWithInitialState(game, random.ChooseWithWeights(game.GetInitialStates().Where(e => filter(e.state))), players);
        }

        public static GameStateT PlayWithInitialState<MoveT, GameT, GameStateT>(GameT game, GameStateT initialState, params IPlayer<MoveT, GameT, GameStateT>[] players)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            for (int player = 0; player < game.NumberOfPlayers; player++)
            {
                players[player].ProvideRulesAndInitialKnowledge(game, player, game.GetPerceivedInitialStatesFromActual(initialState, player));
            }

            var state = initialState;

            while (!state.IsTerminal)
            {
                
                var legalMovesByPlayer = state.LegalMovesByPlayer;

                var zip = Enumerable.Zip(players, legalMovesByPlayer);

                var combinedMove = Enumerable
                    .Zip(players, legalMovesByPlayer)
                    .Select(playerAndMoves => playerAndMoves.First.RequestMove(playerAndMoves.Second))
                    .ToList();

                state = game.GetStateAfterCombinedMove(state, combinedMove);

                var perceptsByPlayer = game.GetPerceptsFromMove(state, combinedMove);
                var claimsByPlayer = game.GetClaimsFromMove(state, combinedMove);
                
                for (int player = 0; player < game.NumberOfPlayers; player++)
                {
                    players[player].ProvidePercepts(perceptsByPlayer[player].percepts, perceptsByPlayer[player].id);
                    players[player].ProvideClaims(claimsByPlayer[player]);
                }
                
            }

            return state;
        }

        public static void ReplayToState<MoveT, GameT, GameStateT>(GameT game, GameStateT state, int playerId, IPlayer<MoveT, GameT, GameStateT> player)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            player.ProvideRulesAndInitialKnowledge(game, playerId, game.GetPerceivedInitialStatesFromActual(state, playerId));

            foreach ((var combinedMove, var nextState) in game.GetHistory(state))
            {
                player.RequestMove(new[] { combinedMove.ElementAt(playerId) });

                var percepts = game.GetPerceptsFromMove(nextState, combinedMove)[playerId];
                player.ProvidePercepts(percepts.percepts, percepts.id);
                player.ProvideClaims(game.GetClaimsFromMove(nextState, combinedMove)[playerId]);
            }
        }
    }
}
