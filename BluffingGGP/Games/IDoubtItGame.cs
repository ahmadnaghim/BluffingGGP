using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

namespace GamePlayer
{
    class IDoubtItGame : IGame<IDoubtItGameMove, IDoubtItGameState>
    {
        public int NumberOfPlayers => 2;

        public bool IsSpecialGame => false;

        public IEnumerable<(IEnumerable<IDoubtItGameMove> combinedMove, IDoubtItGameState nextState)> GetHistory(IDoubtItGameState state) => state.history;

        public IDoubtItGameState GetInitialStateFromCurrent(IDoubtItGameState state) => state.initialState;

        public IEnumerable<(IDoubtItGameState state, int weight, int id)> GetInitialStates()
        {
            List<int> cardDistribution;
            var initialStates = new List<(IDoubtItGameState state, int weight, int id)>();
            var id = 300;

            // Get all possible card distribution
            for (var i = 0; i < 6; i++)
            {
                // Get player 0 1st card
                cardDistribution = new List<int> { -1, -1, -1, -1, -1, -1 };
                // Assign card i to player 0
                cardDistribution[i] = 0;

                for (var j = i + 1; j < 6; j++)
                {
                    // Assign card j to player 0
                    var cardDistribution_2 = new List<int>(cardDistribution);
                    cardDistribution_2[j] = 0;
                    for (var k = j + 1; k < 6; k++)
                    {
                        // Assign card k to player 0
                        var cardDistribution_3 = new List<int>(cardDistribution_2);
                        cardDistribution_3[k] = 0;
                        // Assign the remaining cards to player 1
                        for (var l = 0; l < 6; l++)
                        {
                            if (cardDistribution_3[l] == -1)
                            {
                                cardDistribution_3[l] = 1;
                            }
                        }
                        initialStates.Add((new IDoubtItGameState(cardDistribution_3, true), 1, id++));
                        initialStates.Add((new IDoubtItGameState(cardDistribution_3, false), 1, id++));
                    }
                }
            }
            //for (var i = 0; i < 4; i++)
            //{
            //    // Get player 0 1st card
            //    cardDistribution = new List<int> { -1, -1, -1, -1 };
            //    // Assign card i to player 0
            //    cardDistribution[i] = 0;

            //    for (var j = i + 1; j < 4; j++)
            //    {
            //        // Assign card j to player 0
            //        var cardDistribution_2 = new List<int>(cardDistribution);
            //        cardDistribution_2[j] = 0;
            //        // Assign the remaining cards to player 1
            //        for (var l = 0; l < 4; l++)
            //        {
            //            if (cardDistribution_2[l] == -1)
            //            {
            //                cardDistribution_2[l] = 1;
            //            }
            //        }
            //        initialStates.Add((new IDoubtItGameState(cardDistribution_2, true), 1, id++));
            //        initialStates.Add((new IDoubtItGameState(cardDistribution_2, false), 1, id++));
            //    }
            //}
            return initialStates;
        }

        public IEnumerable<(IDoubtItGameState state, int weight, int id)> GetSpecialInitialStates()
        {
            return GetInitialStates();
        }

        public IEnumerable<(IDoubtItGameState state, int weight, int id)> GetPerceivedInitialStatesFromActual(IDoubtItGameState state, int player)
        {
            return GetInitialStates().Where(initial => state.cards[player].Contains(initial.state.cards[player][0]) &&
                                                        state.cards[player].Contains(initial.state.cards[player][1]) &&
                                                        state.cards[player].Contains(initial.state.cards[player][2]) &&
                                                        state.isThemeBlack == initial.state.isThemeBlack);
        }

        public (Func<IDoubtItGameState, bool>, int)[] GetPerceptsFromMove(IDoubtItGameState state, IEnumerable<IDoubtItGameMove> combinedMove)
        {
            var oriState = state;
            return new (Func<IDoubtItGameState, bool>, int)[] {
                (state => CheckPercepts(oriState, state, 0), 200),
                (state => CheckPercepts(oriState, state, 1), 200),
            };
        }

        private bool CheckPercepts(IDoubtItGameState oriState, IDoubtItGameState checkState, int player)
        {
            // All cards in player hands must be the same
            if (oriState.cards[player].Except(checkState.cards[player]).ToList().Any() ||
                checkState.cards[player].Except(oriState.cards[player]).ToList().Any())
            {
                return false;
            }
            // The number of cards in the stack must be the same
            if (oriState.cards[2].Count() != checkState.cards[2].Count())
            {
                return false;
            }
            
            for (var i = 0; i < oriState.Turn; i++)
            {
                // The history of the player must be the same
                if (oriState.history.ElementAt(i).move.ElementAt(player) != checkState.history.ElementAt(i).move.ElementAt(player))
                {
                    return false;
                }
                // The history of opponent doubt it action must be the same
                if (oriState.history.ElementAt(i).move.ElementAt(1 - player) == IDoubtItGameMove.DOUBT_IT &&
                    checkState.history.ElementAt(i).move.ElementAt(1 - player) != IDoubtItGameMove.DOUBT_IT)
                {
                    return false;
                }
            }
            return true;
        }

        public IDoubtItGameState GetStateAfterCombinedMove(IDoubtItGameState state, IEnumerable<IDoubtItGameMove> combinedMove) => new IDoubtItGameState(state, combinedMove);

        public bool IsClaim(IDoubtItGameMove move, out int[] receivers, out Func<IDoubtItGameState, bool> claim,
                            out Func<IDoubtItGameState, bool> lieClaim, out int id)
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

    class IDoubtItGameState : IGameState<IDoubtItGameMove>
    {
        internal readonly IDoubtItGameState initialState;
        internal readonly IEnumerable<(IEnumerable<IDoubtItGameMove> move, IDoubtItGameState nextState)> history;
        internal readonly int firstPlayer;
        internal readonly List<List<IDoubtItGameCard>> cards;
        internal readonly bool isThemeBlack;
        internal readonly bool isTerminal;

        internal IDoubtItGameState(List<int> cardDistribution, bool isThemeBlack)
        {
            cards = new List<List<IDoubtItGameCard>>();
            cards.Add(new List<IDoubtItGameCard>());
            cards.Add(new List<IDoubtItGameCard>());
            cards.Add(new List<IDoubtItGameCard>());

            initialState = this;

            history = Enumerable.Empty<(IEnumerable<IDoubtItGameMove> move, IDoubtItGameState nextState)>();

            // Initiate the cards
            for (int i = 0; i < 6; i++)
            {
                cards[cardDistribution[i]].Add((IDoubtItGameCard)i);
            }

            // Initiate the card theme
            this.isThemeBlack = isThemeBlack;
            firstPlayer = 0;

            isTerminal = false;
            LegalMovesByPlayer = CreateLegalMoves();
        }

        internal IDoubtItGameState(IDoubtItGameState lastState, IEnumerable<IDoubtItGameMove> move)
        {
            initialState = lastState.initialState;

            var history = lastState.history.ToList();
            history.Add((move, this));
            this.history = history;

            // Copy the condition from last state
            cards = lastState.cards.Select(playerCards => new List<IDoubtItGameCard>(playerCards)).ToList();
            isThemeBlack = lastState.isThemeBlack;
            isTerminal = lastState.isTerminal;
            firstPlayer = lastState.firstPlayer;

            // Get the move of the last player
            var lastPlayer = (lastState.Turn + lastState.firstPlayer) % 2;
            var secondLastPlayer = 1 - lastPlayer;
            var lastMove = move.ElementAt(lastPlayer);

            // If the move is doubt it, check the top card
            // If the card put fits the theme, the doubter gets the cards,
            // Else, the doubted gets the cards
            // The stacked card is cleared
            if (lastMove == IDoubtItGameMove.DOUBT_IT)
            {
                var secondLastMove = lastState.history.Last().move.ElementAt(secondLastPlayer);
                var topCard = cards[2].Last();
                if (isThemeBlack)
                {
                    if ((int)topCard >= 3)
                    {
                        cards[lastPlayer].AddRange(cards[2]);
                        cards[2].Clear();
                    }
                    else
                    {
                        cards[secondLastPlayer].AddRange(cards[2]);
                        firstPlayer = 1 - firstPlayer;
                        cards[2].Clear();
                    }
                }
                else
                {
                    if ((int)topCard < 3)
                    {
                        cards[lastPlayer].AddRange(cards[2]);
                        cards[2].Clear();
                    }
                    else
                    {
                        cards[secondLastPlayer].AddRange(cards[2]);
                        firstPlayer = 1 - firstPlayer;
                        cards[2].Clear();
                    }
                }
            }
            else
            {
                // Remove put card from player's hand to the stacked card
                var putCard = (IDoubtItGameCard)(int)lastMove;
                cards[2].Add(putCard);
                cards[lastPlayer].Remove(putCard);
            }

            // If any of the players hands are empty, the game is over
            if (cards[0].Count == 0 || cards[1].Count == 0)
            {
                isTerminal = true;
            }

            // Set the game to the maximum of 8 turns
            if (Turn == 8)
            {
                isTerminal = true;
            }

            LegalMovesByPlayer = CreateLegalMoves();
        }

        public IEnumerable<IEnumerable<IDoubtItGameMove>> LegalMovesByPlayer { get; }

        private IEnumerable<IEnumerable<IDoubtItGameMove>> CreateLegalMoves()
        {
            var currentPlayer = (Turn + firstPlayer) % 2;
            // Default to no-op
            var legalMoves = new[]
            {
                new[] { IDoubtItGameMove.NO_OP },
                new[] { IDoubtItGameMove.NO_OP },
            };
            // For each card in current player's hand, add it to the legal moves
            foreach (var card in cards[currentPlayer])
            {
                legalMoves[currentPlayer] = legalMoves[currentPlayer].Append((IDoubtItGameMove)card.GetIndex()).ToArray();
            }
            // Current player can doubt it if the last move is put cards, add it to the legal moves
            if (history.Any() && history.Last().move.Any(m => m >= IDoubtItGameMove.PUT_RED_1 && m < IDoubtItGameMove.DOUBT_IT))
            {
                legalMoves[currentPlayer] = legalMoves[currentPlayer].Append(IDoubtItGameMove.DOUBT_IT).ToArray();
            }
            // Remove NO_OP from current player moves
            legalMoves[currentPlayer] = legalMoves[currentPlayer].Where(m => m != IDoubtItGameMove.NO_OP).ToArray();

            return legalMoves;
        }

        public bool IsTerminal => isTerminal;

        public int Turn => history.Count();

        public int PlayingPlayer => (Turn + firstPlayer) % 2;

        public int[] GetUtilities()
        {
            if (!IsTerminal) throw new InvalidOperationException("Utilities can only be retrieved from a terminal state");

            var utilities = new int[2];

            // If there is no player with 0 cards, all get 0 utilities
            if (cards[0].Count != 0 && cards[1].Count != 0)
            {
                utilities[0] = 0;
                utilities[1] = 0;
                return utilities;
            }

            // Get the players with 0 cards
            var winningPlayer = cards[0].Count == 0 ? 0 : 1;
            utilities[winningPlayer] = 100;
            utilities[1 - winningPlayer] = 0;
            
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

            // If theme is black, add 0, else add 1
            key += isThemeBlack ? "0-" : "1-";

            // Add the turn number
            key += player == 0 ? ((Turn + firstPlayer) % 2).ToString() : ((Turn + 1 + firstPlayer) % 2).ToString();
            key += "-";

            // For each card in player's hand, add it to the key
            cards[player].Sort();
            var putCards = new List<char>();
            foreach (var card in cards[player])
            {
                if (card >= IDoubtItGameCard.BLACK_1) putCards.Add('b');
                else putCards.Add('r');
            }
            putCards.Sort();
            foreach (var putCard in putCards)
            {
                key += putCard;
            }
            key += "-";

            // Add the number of cards in the stack
            key += cards[2].Count.ToString() + "-";

            // For each card put by the player to the stack, add it to the key
            putCards.Clear();
            var previousTurn = history.Count() - 1;
            while (previousTurn >= 0)
            {
                var previousHistory = history.ElementAt(previousTurn);
                // If any of the previous move is doubt it, break
                if (previousHistory.move.Where(x => x == IDoubtItGameMove.DOUBT_IT).Any()) break;

                // If the player put any cards, add to sets
                if (previousHistory.move.ElementAt(player) < IDoubtItGameMove.DOUBT_IT)
                {
                    // If black, add b to the string, else add r
                    if (previousHistory.move.ElementAt(player) >= IDoubtItGameMove.PUT_BLACK_1)
                    {
                        putCards.Add('b');
                    }
                    else
                    {
                        putCards.Add('r');
                    }
                }

                previousTurn--;
            }
            putCards.Sort();
            foreach (var putCard in putCards)
            {
                key += putCard;
            }

            return key;
        }

        public List<IDoubtItGameMove> BluffingMoves(int player)
        {
            var legalMoves = LegalMovesByPlayer.ElementAt(player);
            var tempTurn = Turn + (player == 0 ? 0 : 1);
            tempTurn = tempTurn % 2;
            // If turn to ask, ask cards in own hands
            var bluffingMoves = new List<IDoubtItGameMove>();
            if (tempTurn == 0)
            {
                foreach (var move in legalMoves)
                {
                    if (move == IDoubtItGameMove.NO_OP) continue;
                    if (cards[player].Contains((IDoubtItGameCard)(int)move))
                    {
                        bluffingMoves.Add(move);
                    }
                }
            }
            else
            {
                bluffingMoves.Add(IDoubtItGameMove.NO_OP);
            }
            return bluffingMoves;
        }

        public bool IsMovePossible(IEnumerable<IDoubtItGameMove> combinedMove)
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

            var other = (IDoubtItGameState)obj;

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

    enum IDoubtItGameMove
    {
        PUT_RED_1 = 0,
        PUT_RED_2 = 1,
        PUT_RED_3 = 2,
        PUT_BLACK_1 = 3,
        PUT_BLACK_2 = 4,
        PUT_BLACK_3 = 5,
        DOUBT_IT = 6,
        NO_OP = 7,
    }

    enum IDoubtItGameCard
    {
        RED_1 = 0, RED_2 = 1, 
        RED_3 = 2,
        BLACK_1 = 3, BLACK_2 = 4, 
        BLACK_3 = 5,
    }
}
