using System;
using System.Collections.Generic;

namespace GamePlayer
{
    class RandomPlayer<MoveT, GameT, GameStateT> : IPlayer<MoveT, GameT, GameStateT>
        where GameT : IGame<MoveT, GameStateT>
        where GameStateT : IGameState<MoveT>
    {
        readonly Random random = new Random();

        public void ProvideRulesAndInitialKnowledge(GameT game, int player, IEnumerable<(GameStateT state, int weight, int id)> possibleStartingStates) {}
        public void ProvidePercepts(Func<GameStateT, bool> percepts, int id) {}
        public void ProvideClaims(IEnumerable<(int sender, Func<GameStateT, bool> claim, Func<GameStateT, bool> lieClaim, int id)> claims) {}

        public MoveT RequestMove(IEnumerable<MoveT> legalMoves) => random.Choose(legalMoves);
    }
}
