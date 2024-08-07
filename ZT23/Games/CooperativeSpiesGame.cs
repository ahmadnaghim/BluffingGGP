using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class CooperativeSpiesGame : IGame<CooperativeSpiesGameMove, CooperativeSpiesGameState>
    {
        public int NumberOfPlayers => 2;

        public bool IsSpecialGame => false;

        public IEnumerable<(IEnumerable<CooperativeSpiesGameMove> combinedMove, CooperativeSpiesGameState nextState)> GetHistory(CooperativeSpiesGameState state) => state.history;

        public CooperativeSpiesGameState GetInitialStateFromCurrent(CooperativeSpiesGameState state) => state.initialState;

        public IEnumerable<(CooperativeSpiesGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new CooperativeSpiesGameState(true), 1, 300),
            (new CooperativeSpiesGameState(false), 1, 301),
        };

        public IEnumerable<(CooperativeSpiesGameState state, int weight, int id)> GetSpecialInitialStates() => GetInitialStates();

        public IEnumerable<(CooperativeSpiesGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(CooperativeSpiesGameState state, int player)
            => player == 1 ? GetInitialStates() : GetInitialStates().Where(initial => initial.state.isWireToCutRed == state.isWireToCutRed);

        public (Func<CooperativeSpiesGameState, bool>, int)[] GetPerceptsFromMove(CooperativeSpiesGameState state, IEnumerable<CooperativeSpiesGameMove> combinedMove)
        {
            var moveSeen = state.history.First().move.First();

            return new (Func<CooperativeSpiesGameState, bool>, int)[] {
                (state => true, 200),
                (state => state.history.First().move.First() == moveSeen, 201 + moveSeen.GetIndex()),
            };
        }

        public CooperativeSpiesGameState GetStateAfterCombinedMove(CooperativeSpiesGameState state, IEnumerable<CooperativeSpiesGameMove> combinedMove) => new CooperativeSpiesGameState(state, combinedMove);

        public bool IsClaim(CooperativeSpiesGameMove move, out int[] receivers, out Func<CooperativeSpiesGameState, bool> claim,
                            out Func<CooperativeSpiesGameState, bool> lieClaim, out int id)
        {
            switch (move)
            {
                case CooperativeSpiesGameMove.CLAIM_RED:
                    receivers = new[] { 1 };
                    claim = state => state.isWireToCutRed;
                    lieClaim = state => !state.isWireToCutRed;
                    id = 100;
                    return true;

                case CooperativeSpiesGameMove.CLAIM_BLUE:
                    receivers = new[] { 1 };
                    claim = state => !state.isWireToCutRed;
                    lieClaim = state => state.isWireToCutRed;
                    id = 101;
                    return true;

                default:
                    receivers = null;
                    claim = null;
                    lieClaim = null;
                    id = 0;
                    return false;
            }
        }
    }

    class CooperativeSpiesGameState : IGameState<CooperativeSpiesGameMove>
    {
        internal readonly CooperativeSpiesGameState initialState;
        internal readonly IEnumerable<(IEnumerable<CooperativeSpiesGameMove> move, CooperativeSpiesGameState nextState)> history;
        internal readonly bool isWireToCutRed;

        internal CooperativeSpiesGameState(bool isWireToCutRed)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<CooperativeSpiesGameMove> move, CooperativeSpiesGameState nextState)>();
            this.isWireToCutRed = isWireToCutRed;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal CooperativeSpiesGameState(CooperativeSpiesGameState lastState, IEnumerable<CooperativeSpiesGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            isWireToCutRed = lastState.isWireToCutRed;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<CooperativeSpiesGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<CooperativeSpiesGameMove>> CreateLegalMoves() => Turn == 0 ? new[] {
            new[] { CooperativeSpiesGameMove.CLAIM_RED, CooperativeSpiesGameMove.CLAIM_BLUE },
            new[] { CooperativeSpiesGameMove.NO_OP },
        } : new[] {
            new[] { CooperativeSpiesGameMove.NO_OP },
            new[] { CooperativeSpiesGameMove.CUT_RED, CooperativeSpiesGameMove.CUT_BLUE },
        };

        public bool IsTerminal => Turn == 2;

        public int Turn => history.Count();

        public int PlayingPlayer => -1;

        public InformationSet GetInformationSet(int player)
        {
            return new InformationSet("", 0);
        }

        public String GetKey(int player)
        {
            return "";
        }

        public List<CooperativeSpiesGameMove> BluffingMoves(int player)
        {
            var legalMoves = LegalMovesByPlayer.ElementAt(player);
            return legalMoves.ToList();
        }

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var cutWire = history.Last().move.ElementAt(1);
            var utility = (cutWire == CooperativeSpiesGameMove.CUT_RED) == isWireToCutRed ? 100 : 0;

            return new[] { utility, utility };
        }

        public bool IsMovePossible(IEnumerable<CooperativeSpiesGameMove> combinedMove)
        {
            return true;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (CooperativeSpiesGameState)obj;

            return isWireToCutRed == other.isWireToCutRed
                && Turn == other.Turn
                && Enumerable
                    .Range(0, Turn)
                    .All(turn => history.ElementAt(turn).move.SequenceEqual(other.history.ElementAt(turn).move));
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Turn;
        }
    }

    enum CooperativeSpiesGameMove
    {
        CUT_RED,
        CUT_BLUE,
        NO_OP,
        CLAIM_RED,
        CLAIM_BLUE,
    }
}
