using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace GamePlayer
{
    class CluedoGame : IGame<CluedoGameMove, CluedoGameState>
    {
        public int NumberOfPlayers => 2;

        public bool IsSpecialGame => false;

        public IEnumerable<(IEnumerable<CluedoGameMove> combinedMove, CluedoGameState nextState)> GetHistory(CluedoGameState state) => state.history;

        public CluedoGameState GetInitialStateFromCurrent(CluedoGameState state) => state.initialState;

        public IEnumerable<(CluedoGameState state, int weight, int id)> GetInitialStates()
        {
            List<int> cardDistribution;
            var initialStates = new List<(CluedoGameState state, int weight, int id)>();
            var id = 300;
            
            // Iterate through all possible hidden cards
            for (var i = 0; i < 5; i++)
            {
                var cards = new List<int> { 0, 1, 2, 3, 4 };
                cardDistribution = new List<int> { -1, -1, -1, -1, -1 };
                // Assign card i to the hidden card
                cardDistribution[i] = 2;
                cards.Remove(i);
                // Assign 2 cards to player 0
                for (var j = 0; j < 4; j++)
                {
                    var cardDistribution_2 = new List<int>(cardDistribution);
                    cardDistribution_2[cards[j]] = 0;
                    for (var k = j + 1; k < 4; k++)
                    {
                        var cardDistribution_3 = new List<int>(cardDistribution_2);
                        cardDistribution_3[cards[k]] = 0;
                        // Assign the remaining cards to player 1
                        for (var l = 0; l < 5; l++)
                        {
                            if (cardDistribution_3[l] == -1)
                            {
                                cardDistribution_3[l] = 1;
                            }
                        }
                        initialStates.Add((new CluedoGameState(cardDistribution_3), 1, id++));
                    }
                }
            }
            return initialStates;
        }
        
        public IEnumerable<(CluedoGameState state, int weight, int id)> GetSpecialInitialStates() => GetInitialStates();

        public IEnumerable<(CluedoGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(CluedoGameState state, int player)
        {
            return GetInitialStates().Where(initial => state.cards[player].Contains(initial.state.cards[player][0]) &&
                                                        state.cards[player].Contains(initial.state.cards[player][1]));
        }

        public (Func<CluedoGameState, bool>, int)[] GetPerceptsFromMove(CluedoGameState state, IEnumerable<CluedoGameMove> combinedMove)
        {
            var oriState = state;
            return new (Func<CluedoGameState, bool>, int)[] {
                (state => CheckPercepts(oriState, state, 0), 200),
                (state => CheckPercepts(oriState, state, 1), 200),
            };
        }

        private bool CheckPercepts(CluedoGameState oriState, CluedoGameState checkState, int player)
        {
            var lastMove = oriState.history.Last().move;
            var enemyPlayer = 1 - player;

            // Get turn of the game
            var tempTurn = oriState.Turn + (player == 0 ? 0 : 2);
            tempTurn = tempTurn % 4;

            // Every moves can be seen
            for (var i = 0; i < NumberOfPlayers; i++)
            {
                if (oriState.history.Last().move.ElementAt(i) != checkState.history.Last().move.ElementAt(i))
                {
                    //Console.WriteLine(player + " " + i + " " + oriState.history.Last().move.ElementAt(i) + " " + checkState.history.Last().move.ElementAt(i));
                    return false;
                }
            }

            // If player asked in the last turn, player must know whether the card is in the enemy player hand
            if (tempTurn == 1)
            {
                if (lastMove.ElementAt(player) <= CluedoGameMove.ASK_CARD_5)
                {
                    if (oriState.cards[enemyPlayer].Contains((CluedoCard)(int)lastMove.ElementAt(player)) !=
                        checkState.cards[enemyPlayer].Contains((CluedoCard)(int)lastMove.ElementAt(player)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public CluedoGameState GetStateAfterCombinedMove(CluedoGameState state, IEnumerable<CluedoGameMove> combinedMove) => new CluedoGameState(state, combinedMove);

        public bool IsClaim(CluedoGameMove move, out int[] receivers, out Func<CluedoGameState, bool> claim,
                            out Func<CluedoGameState, bool> lieClaim, out int id)
        {
            switch (move)
            {
                default:
                    receivers = null;
                    claim = null;
                    lieClaim = null;
                    id = 0;
                    return false;
            }
        }
    }

    class CluedoGameState : IGameState<CluedoGameMove>
    {
        internal readonly CluedoGameState initialState;
        internal readonly IEnumerable<(IEnumerable<CluedoGameMove> move, CluedoGameState nextState)> history;
        internal readonly List<List<CluedoCard>> cards;
        internal readonly bool isTerminal;

        internal CluedoGameState(List<int> cardDistribution)
        {
            cards = new List<List<CluedoCard>>();
            cards.Add(new List<CluedoCard>());
            cards.Add(new List<CluedoCard>());
            cards.Add(new List<CluedoCard>());

            initialState = this;

            history = Enumerable.Empty<(IEnumerable<CluedoGameMove> move, CluedoGameState nextState)>();

            // Initiate the cluedo cards
            for (int i = 0; i < 5; i++)
            {
                cards[cardDistribution[i]].Add((CluedoCard)i);
            }

            isTerminal = false;
            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal CluedoGameState(CluedoGameState lastState, IEnumerable<CluedoGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            // If one of the previous actions was a guess, the game is over
            isTerminal = move.Any(m => m >= CluedoGameMove.GUESS_CARD_1 && m <= CluedoGameMove.GUESS_CARD_5);

            // Copy the cards from the last state
            cards = lastState.cards.Select(playerCards => new List<CluedoCard>(playerCards)).ToList();

            // Set the game to the maximum of 8 turns
            if (Turn == 12)
            {
                isTerminal = true;
            }

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<CluedoGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<CluedoGameMove>> CreateLegalMoves()
        {
            var temp = Turn % 4;
            return (temp) switch
            {
                0 => new[] {
                        new[] { CluedoGameMove.ASK_CARD_1, CluedoGameMove.ASK_CARD_2, CluedoGameMove.ASK_CARD_3,
                                CluedoGameMove.ASK_CARD_4, CluedoGameMove.ASK_CARD_5 },
                        new[] { CluedoGameMove.NO_OP},
                    },
                1 => new[] {
                        new[] { CluedoGameMove.GUESS_CARD_1, CluedoGameMove.GUESS_CARD_2, CluedoGameMove.GUESS_CARD_3,
                                CluedoGameMove.GUESS_CARD_4, CluedoGameMove.GUESS_CARD_5, CluedoGameMove.NO_OP },
                        new[] { CluedoGameMove.NO_OP},
                    },
                2 => new[] {
                        new[] { CluedoGameMove.NO_OP },
                        new[] { CluedoGameMove.ASK_CARD_1, CluedoGameMove.ASK_CARD_2, CluedoGameMove.ASK_CARD_3,
                                CluedoGameMove.ASK_CARD_4, CluedoGameMove.ASK_CARD_5 },
                    },
                3 => new[] {
                        new[] { CluedoGameMove.NO_OP},
                        new[] { CluedoGameMove.GUESS_CARD_1, CluedoGameMove.GUESS_CARD_2, CluedoGameMove.GUESS_CARD_3,
                                CluedoGameMove.GUESS_CARD_4, CluedoGameMove.GUESS_CARD_5, CluedoGameMove.NO_OP },
                    },
                _ => new[] {
                        new[] { CluedoGameMove.NO_OP },
                        new[] { CluedoGameMove.NO_OP }
                }
            };
        }

        public bool IsTerminal => isTerminal;

        public int Turn => history.Count();

        public int PlayingPlayer => (Turn % 4) >= 2 ? 1 : 0;

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var utilities = new int[2];

            // Get last move and the guess move and player that is guessing
            var lastMove = history.Last().move;

            // If the last move not a guess, all players get 0 utilities
            if (!lastMove.Any(m => m >= CluedoGameMove.GUESS_CARD_1 && m <= CluedoGameMove.GUESS_CARD_5))
            {
                utilities[0] = 0;
                utilities[1] = 0;
            }
            else
            {
                // If the last move was a guess, calculate the utilities
                for (int i = 0; i < 2; i++)
                {
                    if (lastMove.ElementAt(i) >= CluedoGameMove.GUESS_CARD_1 && lastMove.ElementAt(i) <= CluedoGameMove.GUESS_CARD_5)
                    {
                        var guessMove = lastMove.ElementAt(i);
                        var guessCard = (CluedoCard)(guessMove - CluedoGameMove.GUESS_CARD_1);
                        var guessPlayer = i;

                        var otherPlayer = 1 - guessPlayer;

                        // Check if the guess is the correct one
                        if (cards[2].First() == guessCard)
                        {
                            utilities[guessPlayer] = 100;
                            utilities[otherPlayer] = 0;
                        }
                        else
                        {
                            utilities[guessPlayer] = 0;
                            utilities[otherPlayer] = 100;
                        }
                        break;
                    }
                }
            }
            
            return utilities;
        }

        public InformationSet GetInformationSet(int player)
        {
            String key = GetKey(player);
            var informationSet = new InformationSet(key, LegalMovesByPlayer.ElementAt(player).Count());

            return informationSet;
        }

        // Get key of Information Set
        public String GetKey(int player)
        {
            String key = "";
            var enemyPlayer = 1 - player;
            // For each card in player's hand, add it to the key
            foreach (var card in cards[player])
            {
                key += ((int) card).ToString();
            }
            key += "-";
            // For each asked card, add it to the key if it is in the other player hands
            // If asked cards not in the other player hands, then add it in the last
            var start = new List<int>();
            var end = new List<int>();
            foreach (var turn in history)
            {
                if (turn.move.ElementAt(player) <= CluedoGameMove.ASK_CARD_5)
                {
                    if (cards[enemyPlayer].Contains((CluedoCard)(int) turn.move.ElementAt(player)))
                    {
                        start.Add((int) turn.move.ElementAt(player));
                    }
                    else
                    {
                        end.Add((int) turn.move.ElementAt(player));
                    }
                }
            }
            start.Sort();
            foreach (var startKey in start.Distinct())
            {
                key += startKey.ToString();
            }
            key += "-";
            end.Sort();
            foreach (var endKey in end.Distinct())
            {
                key += endKey.ToString();
            }
            key += "-";
            start.Clear();
            end.Clear();
            // Now add for each asked card by the other player
            foreach (var turn in history)
            {
                if (turn.move.ElementAt(enemyPlayer) <= CluedoGameMove.ASK_CARD_5)
                {
                    if (cards[player].Contains((CluedoCard)(int) turn.move.ElementAt(enemyPlayer)))
                    {
                        start.Add((int) turn.move.ElementAt(enemyPlayer));
                    }
                    else
                    {
                        end.Add((int) turn.move.ElementAt(enemyPlayer));
                    }
                }
            }
            start.Sort();
            foreach (var startKey in start.Distinct())
            {
                key += startKey.ToString();
            }
            key += "-";
            end.Sort();
            foreach (var endKey in end.Distinct())
            {
                key += endKey.ToString();
            }
            key += "-";

            // Lastly, add whether the current turn is asking or guessing
            key += player == 0 ? (Turn % 4).ToString() : ((Turn + 2) % 4).ToString();
            return key;
        }

        public List<CluedoGameMove> BluffingMoves(int player)
        {
            var legalMoves = LegalMovesByPlayer.ElementAt(player);
            var tempTurn = Turn + (player == 0 ? 0 : 2);
            tempTurn = tempTurn % 4;
            // If turn to ask, ask cards in own hands
            var bluffingMoves = new List<CluedoGameMove>();
            if (tempTurn == 0)
            {
                foreach (var move in legalMoves)
                {
                    if (move == CluedoGameMove.NO_OP) continue;
                    if (cards[player].Contains((CluedoCard)(int)move))
                    {
                        bluffingMoves.Add(move);
                    }
                }
            }
            else
            {
                bluffingMoves.Add(CluedoGameMove.NO_OP);
            }
            return bluffingMoves;
        }

        public bool IsMovePossible(IEnumerable<CluedoGameMove> combinedMove)
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

            var other = (CluedoGameState)obj;

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

    enum CluedoGameMove
    {
        ASK_CARD_1 = 0,
        ASK_CARD_2 = 1,
        ASK_CARD_3 = 2,
        ASK_CARD_4 = 3,
        ASK_CARD_5 = 4,
        GUESS_CARD_1 = 5,
        GUESS_CARD_2 = 6,
        GUESS_CARD_3 = 7,
        GUESS_CARD_4 = 8,
        GUESS_CARD_5 = 9,
        NO_OP = 10
    }

    enum CluedoCard
    {
        WEAPON_1 = 0, WEAPON_2 = 1, WEAPON_3 = 2,
        WEAPON_4 = 3, WEAPON_5 = 4
        //PEOPLE_1, PEOPLE_2, PEOPLE_3,
        //PEOPLE_4, PEOPLE_5, PEOPLE_6,
        //ROOM_1, ROOM_2, ROOM_3,
        //ROOM_4, ROOM_5, ROOM_6,
        //ROOM_7, ROOM_8, ROOM_9,
    }
}
