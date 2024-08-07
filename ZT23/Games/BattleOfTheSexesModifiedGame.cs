using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class BattleOfTheSexesModifiedGame : IGame<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGameState>
    {
        public int NumberOfPlayers => 2;

        public bool IsSpecialGame => true;

        public IEnumerable<(IEnumerable<BattleOfTheSexesModifiedGameMove> combinedMove, BattleOfTheSexesModifiedGameState nextState)> GetHistory(BattleOfTheSexesModifiedGameState state) => state.history;

        public BattleOfTheSexesModifiedGameState GetInitialStateFromCurrent(BattleOfTheSexesModifiedGameState state) => state.initialState;

        public IEnumerable<(BattleOfTheSexesModifiedGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new BattleOfTheSexesModifiedGameState(), 1, 300)
        };

        public IEnumerable<(BattleOfTheSexesModifiedGameState state, int weight, int id)> GetSpecialInitialStates() => new[] {
            (new BattleOfTheSexesModifiedGameState(true, true), 1, 301),
            (new BattleOfTheSexesModifiedGameState(true, false), 1, 302),
            (new BattleOfTheSexesModifiedGameState(false, true), 1, 303),
            (new BattleOfTheSexesModifiedGameState(false, false), 1, 304),
        };

        public IEnumerable<(BattleOfTheSexesModifiedGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(BattleOfTheSexesModifiedGameState state, int player)
            => GetInitialStates();

        public (Func<BattleOfTheSexesModifiedGameState, bool>, int)[] GetPerceptsFromMove(BattleOfTheSexesModifiedGameState state, IEnumerable<BattleOfTheSexesModifiedGameMove> combinedMove)
        {
            var isTerminal = state.IsTerminal;
            switch (state.Turn)
            {
                case 1:
                    var moveSeen_1 = state.history.First().move.First();
                    return new (Func<BattleOfTheSexesModifiedGameState, bool>, int)[] {
                        (state => true, 200),
                        (state => state.history.First().move.First() == moveSeen_1, 203 + moveSeen_1.GetIndex()),
                    };
                default: 
                    return new (Func<BattleOfTheSexesModifiedGameState, bool>, int)[] {
                        (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                        (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                    };

            }
        }

        public BattleOfTheSexesModifiedGameState GetStateAfterCombinedMove(BattleOfTheSexesModifiedGameState state, IEnumerable<BattleOfTheSexesModifiedGameMove> combinedMove) => new BattleOfTheSexesModifiedGameState(state, combinedMove);

        public bool IsClaim(BattleOfTheSexesModifiedGameMove move, out int[] receivers, out Func<BattleOfTheSexesModifiedGameState, bool> claim,
                            out Func<BattleOfTheSexesModifiedGameState, bool> lieClaim, out int id)
        {
            switch (move)
            {
                // Player 1 claiming he will pick football
                case BattleOfTheSexesModifiedGameMove.CLAIM_FOOTBALL_1:
                    receivers = new[] { 1 };
                    claim = state => state.husbandChoice == 0 || state.husbandChoice == -1;
                    lieClaim = state => state.husbandChoice == 1 || state.husbandChoice == -1;
                    id = 100;
                    return true;

                // Player 2 claiming he will pick theatre
                case BattleOfTheSexesModifiedGameMove.CLAIM_THEATRE_1:
                    receivers = new[] { 1 };
                    claim = state => state.husbandChoice == 1 || state.husbandChoice == -1;
                    lieClaim = state => state.husbandChoice == 0 || state.husbandChoice == -1;
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

    class BattleOfTheSexesModifiedGameState : IGameState<BattleOfTheSexesModifiedGameMove>
    {
        internal readonly BattleOfTheSexesModifiedGameState initialState;
        internal readonly IEnumerable<(IEnumerable<BattleOfTheSexesModifiedGameMove> move, BattleOfTheSexesModifiedGameState nextState)> history;
        internal readonly int husbandChoice, wifeChoice;

        internal BattleOfTheSexesModifiedGameState()
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<BattleOfTheSexesModifiedGameMove> move, BattleOfTheSexesModifiedGameState nextState)>();

            // Initiate choices to -1 to indicate that no choice has been made yet
            husbandChoice = -1;
            wifeChoice = -1;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal BattleOfTheSexesModifiedGameState(bool husband, bool wife)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<BattleOfTheSexesModifiedGameMove> move, BattleOfTheSexesModifiedGameState nextState)>();

            // Initiate choices to -1 to indicate that no choice has been made yet
            husbandChoice = husband ? 0 : 1;
            wifeChoice = wife ? 0 : 1;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal BattleOfTheSexesModifiedGameState(BattleOfTheSexesModifiedGameState lastState, IEnumerable<BattleOfTheSexesModifiedGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            // Update the state choices made by the players
            if (Turn == 2)
            {
                husbandChoice = move.First() == BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL ? 0 : 1;
                wifeChoice = lastState.wifeChoice;
                //wifeChoice = move.ElementAt(1) == BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL ? 0 : 1;
            }
            else if (Turn == 3)
            {
                husbandChoice = lastState.husbandChoice;
                wifeChoice = move.ElementAt(1) == BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL ? 0 : 1;
            }
            // Copy the choices from the previous state
            else
            {
                husbandChoice = lastState.husbandChoice;
                wifeChoice = lastState.wifeChoice;
            }

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<BattleOfTheSexesModifiedGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<BattleOfTheSexesModifiedGameMove>> CreateLegalMoves()
        {
            return Turn switch
            {
                // First player claiming what they want to choose in the second turn
                0 => new[] {
                        new[] { BattleOfTheSexesModifiedGameMove.CLAIM_FOOTBALL_1, BattleOfTheSexesModifiedGameMove.CLAIM_THEATRE_1 },
                        new[] { BattleOfTheSexesModifiedGameMove.NO_OP },
                    },
                // Each player choose their preferred option
                //1 => new[] {
                //        new[] { BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL, BattleOfTheSexesModifiedGameMove.CHOOSE_THEATRE },
                //        new[] { BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL, BattleOfTheSexesModifiedGameMove.CHOOSE_THEATRE },
                //    },
                1 => new[] {
                        new[] { BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL, BattleOfTheSexesModifiedGameMove.CHOOSE_THEATRE },
                        new[] { BattleOfTheSexesModifiedGameMove.NO_OP },
                    },
                2 => new[] {
                        new[] { BattleOfTheSexesModifiedGameMove.NO_OP },
                        new[] { BattleOfTheSexesModifiedGameMove.CHOOSE_FOOTBALL, BattleOfTheSexesModifiedGameMove.CHOOSE_THEATRE },
                    },
                _ => null,
            };
        }

        public bool IsTerminal => Turn == 3;

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

        public List<BattleOfTheSexesModifiedGameMove> BluffingMoves(int player)
        {
            var legalMoves = LegalMovesByPlayer.ElementAt(player);
            return legalMoves.ToList();
        }

        public bool IsMovePossible(IEnumerable<BattleOfTheSexesModifiedGameMove> combinedMove)
        {
            var nextState = new BattleOfTheSexesModifiedGameState(this, combinedMove);
            if (this.husbandChoice == -1 || this.wifeChoice == -1) return true;
            if (nextState.husbandChoice != this.husbandChoice) return false;
            if (nextState.wifeChoice != this.wifeChoice) return false;
            return true;
        }

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            int husbandUtility, wifeUtility;

            // Both want to go to the football match
            if (husbandChoice == 0 && wifeChoice == 0)
            {
                husbandUtility = 100; wifeUtility = 50;
            }
            // Both want to go to the theatre
            else if (husbandChoice == 1 && wifeChoice == 1)
            {
                husbandUtility = 50; wifeUtility = 100;
            }
            // Both want to go to different places
            else
            {
                husbandUtility = 0; wifeUtility = 0;
            }

            return new[] { husbandUtility, wifeUtility };
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

            var other = (BattleOfTheSexesModifiedGameState)obj;

            return Turn == other.Turn
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

    enum BattleOfTheSexesModifiedGameMove
    {
        CLAIM_FOOTBALL_1,
        CLAIM_THEATRE_1,
        NO_OP,
        CHOOSE_FOOTBALL,
        CHOOSE_THEATRE,
    }
}
