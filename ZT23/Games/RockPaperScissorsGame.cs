using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class RockPaperScissorsGame : IGame<RockPaperScissorsGameMove, RockPaperScissorsGameState>
    {
        public int NumberOfPlayers => 2;

        public bool IsSpecialGame => true;

        public IEnumerable<(IEnumerable<RockPaperScissorsGameMove> combinedMove, RockPaperScissorsGameState nextState)> GetHistory(RockPaperScissorsGameState state) => state.history;

        public RockPaperScissorsGameState GetInitialStateFromCurrent(RockPaperScissorsGameState state) => state.initialState;

        public IEnumerable<(RockPaperScissorsGameState state, int weight, int id)> GetInitialStates() => new[] {
            (new RockPaperScissorsGameState(), 1, 300)
        };

        public IEnumerable<(RockPaperScissorsGameState state, int weight, int id)> GetSpecialInitialStates() {
            var states = new List<(RockPaperScissorsGameState state, int weight, int id)>();
            var id = 300;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    states.Add((new RockPaperScissorsGameState(i, j), 1, id++));
                }
            }
            return states;
        }

        public IEnumerable<(RockPaperScissorsGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(RockPaperScissorsGameState state, int player)
            => GetInitialStates();

        public (Func<RockPaperScissorsGameState, bool>, int)[] GetPerceptsFromMove(RockPaperScissorsGameState state, IEnumerable<RockPaperScissorsGameMove> combinedMove)
        {
            var isTerminal = state.IsTerminal;
            switch (state.Turn)
            {
                case 1:
                    var moveSeen_1 = state.history.First().move.First();
                    var moveSeen_2 = state.history.First().move.ElementAt(1);
                    return new (Func<RockPaperScissorsGameState, bool>, int)[] {
                        (state => state.history.First().move.ElementAt(1) == moveSeen_2, 203 + moveSeen_2.GetIndex()),
                        (state => state.history.First().move.First() == moveSeen_1, 203 + moveSeen_1.GetIndex()),
                    };
                default: 
                    return new (Func<RockPaperScissorsGameState, bool>, int)[] {
                        (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                        (state => state.IsTerminal == isTerminal, isTerminal ? 201 : 202),
                    };

            }
        }

        public RockPaperScissorsGameState GetStateAfterCombinedMove(RockPaperScissorsGameState state, IEnumerable<RockPaperScissorsGameMove> combinedMove) => new RockPaperScissorsGameState(state, combinedMove);

        public bool IsClaim(RockPaperScissorsGameMove move, out int[] receivers, out Func<RockPaperScissorsGameState, bool> claim,
                            out Func<RockPaperScissorsGameState, bool> lieClaim, out int id)
        {
            switch (move)
            {
                case RockPaperScissorsGameMove.CLAIM_ROCK_1:
                    receivers = new[] { 1 };
                    claim = state => state.playerChoice[0] == 0 || state.playerChoice[0] == -1;
                    lieClaim = state => state.playerChoice[0] != 0 || state.playerChoice[0] == -1;
                    id = 100;
                    return true;

                case RockPaperScissorsGameMove.CLAIM_PAPER_1:
                    receivers = new[] { 1 };
                    claim = state => state.playerChoice[0] == 1 || state.playerChoice[0] == -1;
                    lieClaim = state => state.playerChoice[0] != 1 || state.playerChoice[0] == -1;
                    id = 101;
                    return true;

                case RockPaperScissorsGameMove.CLAIM_SCISSORS_1:
                    receivers = new[] { 1 };
                    claim = state => state.playerChoice[0] == 2 || state.playerChoice[0] == -1;
                    lieClaim = state => state.playerChoice[0] != 2 || state.playerChoice[0] == -1;
                    id = 102;
                    return true;

                case RockPaperScissorsGameMove.CLAIM_ROCK_2:
                    receivers = new[] { 0 };
                    claim = state => state.playerChoice[1] == 0 || state.playerChoice[1] == -1;
                    lieClaim = state => state.playerChoice[1] != 0 || state.playerChoice[1] == -1;
                    id = 103;
                    return true;

                case RockPaperScissorsGameMove.CLAIM_PAPER_2:
                    receivers = new[] { 0 };
                    claim = state => state.playerChoice[1] == 1 || state.playerChoice[1] == -1;
                    lieClaim = state => state.playerChoice[1] != 1 || state.playerChoice[1] == -1;
                    id = 104;
                    return true;

                case RockPaperScissorsGameMove.CLAIM_SCISSORS_2:
                    receivers = new[] { 0 };
                    claim = state => state.playerChoice[1] == 2 || state.playerChoice[1] == -1;
                    lieClaim = state => state.playerChoice[1] != 2 || state.playerChoice[1] == -1;
                    id = 105;
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

    class RockPaperScissorsGameState : IGameState<RockPaperScissorsGameMove>
    {
        internal readonly RockPaperScissorsGameState initialState;
        internal readonly IEnumerable<(IEnumerable<RockPaperScissorsGameMove> move, RockPaperScissorsGameState nextState)> history;
        internal readonly int[] playerChoice = new int[2];

        internal RockPaperScissorsGameState()
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<RockPaperScissorsGameMove> move, RockPaperScissorsGameState nextState)>();

            playerChoice[0] = -1;
            playerChoice[1] = -1;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal RockPaperScissorsGameState(int i, int j)
        {
            initialState = this;

            history = Enumerable.Empty<(IEnumerable<RockPaperScissorsGameMove> move, RockPaperScissorsGameState nextState)>();

            playerChoice[0] = i;
            playerChoice[1] = j;

            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal RockPaperScissorsGameState(RockPaperScissorsGameState lastState, IEnumerable<RockPaperScissorsGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            //if (Turn == 2)
            //{
            //    for (var i = 0; i < 2; i++)
            //    {
            //        switch (move.ElementAt(i))
            //        {
            //            case RockPaperScissorsGameMove.CHOOSE_ROCK:
            //                playerChoice[i] = 0;
            //                break;
            //            case RockPaperScissorsGameMove.CHOOSE_PAPER:
            //                playerChoice[i] = 1;
            //                break;
            //            case RockPaperScissorsGameMove.CHOOSE_SCISSORS:
            //                playerChoice[i] = 2;
            //                break;
            //        }
            //    }
            //}
            //else
            //{
            //    playerChoice = lastState.playerChoice;
            //}

            if (Turn == 2)
            {
                switch (move.ElementAt(0))
                {
                    case RockPaperScissorsGameMove.CHOOSE_ROCK:
                        playerChoice[0] = 0;
                        break;
                    case RockPaperScissorsGameMove.CHOOSE_PAPER:
                        playerChoice[0] = 1;
                        break;
                    case RockPaperScissorsGameMove.CHOOSE_SCISSORS:
                        playerChoice[0] = 2;
                        break;
                }
                playerChoice[1] = lastState.playerChoice[1];
            }
            else if (Turn == 3)
            {
                switch (move.ElementAt(1))
                {
                    case RockPaperScissorsGameMove.CHOOSE_ROCK:
                        playerChoice[1] = 0;
                        break;
                    case RockPaperScissorsGameMove.CHOOSE_PAPER:
                        playerChoice[1] = 1;
                        break;
                    case RockPaperScissorsGameMove.CHOOSE_SCISSORS:
                        playerChoice[1] = 2;
                        break;
                }
                playerChoice[0] = lastState.playerChoice[0];
            }
            else
            {
                playerChoice = lastState.playerChoice;
            }

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<RockPaperScissorsGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<RockPaperScissorsGameMove>> CreateLegalMoves()
        {
            return Turn switch
            {
                0 => new[] {
                        new[] {
                            RockPaperScissorsGameMove.CLAIM_ROCK_1, RockPaperScissorsGameMove.CLAIM_PAPER_1,
                            RockPaperScissorsGameMove.CLAIM_SCISSORS_1,
                            //RockPaperScissorsGameMove.NO_OP
                        },
                        new[] {
                            //RockPaperScissorsGameMove.CLAIM_ROCK_2, RockPaperScissorsGameMove.CLAIM_PAPER_2,
                            //RockPaperScissorsGameMove.CLAIM_SCISSORS_2,
                            RockPaperScissorsGameMove.NO_OP
                        },
                    },
                //1 => new[] {
                //        new[] { RockPaperScissorsGameMove.CHOOSE_ROCK, RockPaperScissorsGameMove.CHOOSE_PAPER, RockPaperScissorsGameMove.CHOOSE_SCISSORS },
                //        new[] { RockPaperScissorsGameMove.CHOOSE_ROCK, RockPaperScissorsGameMove.CHOOSE_PAPER, RockPaperScissorsGameMove.CHOOSE_SCISSORS },
                //    },
                1 => new[] {
                        new[] { RockPaperScissorsGameMove.CHOOSE_ROCK, RockPaperScissorsGameMove.CHOOSE_PAPER, RockPaperScissorsGameMove.CHOOSE_SCISSORS },
                        new[] { RockPaperScissorsGameMove.NO_OP },
                    },
                2 => new[] {
                        new[] { RockPaperScissorsGameMove.NO_OP },
                        new[] { RockPaperScissorsGameMove.CHOOSE_ROCK, RockPaperScissorsGameMove.CHOOSE_PAPER, RockPaperScissorsGameMove.CHOOSE_SCISSORS },
                    },
                _ => null,
            };
        }

        public bool IsTerminal => Turn == 3;

        public int Turn => history.Count();

        public int PlayingPlayer => -1;

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            int[] playerUtility = new int[2];

            // Draw case
            if (playerChoice[0] == playerChoice[1])
            {
                playerUtility[0] = 0; playerUtility[1] = 0;
            }
            // Player 2 win
            else if ((playerChoice[0] - playerChoice[1] + 3) % 3 == 2)
            {
                playerUtility[0] = 0; playerUtility[1] = 100;
            }
            // Player 1 win
            else
            {
                playerUtility[0] = 100; playerUtility[1] = 0;
            }

            return playerUtility;
        }

        public InformationSet GetInformationSet(int player)
        {
            return new InformationSet("", 0);
        }

        public String GetKey(int player)
        {
            return "";
        }

        public List<RockPaperScissorsGameMove> BluffingMoves(int player)
        {
            var legalMoves = LegalMovesByPlayer.ElementAt(player);
            return legalMoves.ToList();
        }

        public bool IsMovePossible(IEnumerable<RockPaperScissorsGameMove> combinedMove)
        {
            var nextState = new RockPaperScissorsGameState(this, combinedMove);
            if (this.playerChoice[0] == -1 || this.playerChoice[1] == -1) return true;
            if (nextState.playerChoice[0] != this.playerChoice[0]) return false;
            if (nextState.playerChoice[1] != this.playerChoice[1]) return false;
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

            var other = (RockPaperScissorsGameState)obj;

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

    enum RockPaperScissorsGameMove
    {
        CLAIM_ROCK_1,
        CLAIM_PAPER_1,
        CLAIM_SCISSORS_1,
        CLAIM_ROCK_2,
        CLAIM_PAPER_2,
        CLAIM_SCISSORS_2,
        NO_OP,
        CHOOSE_ROCK,
        CHOOSE_PAPER,
        CHOOSE_SCISSORS,
    }
}
