using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    class Program
    {
        // Change to configure the number of simulations to run
        const int N_RUNS = 10000;

        static void Main()
        {
            EvaluateBattleOfTheSexesGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateRockPaperScissorsGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateCooperativeSpiesGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateCluedoGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();

            EvaluateIDoubtItGame(N_RUNS);

            Console.WriteLine(new string('-', 59));
            Console.WriteLine();
        }

        static void EvaluateCooperativeSpiesGame(int numberOfRuns)
        {
            var game = new CooperativeSpiesGame();

            var player1 = new IPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>[]
            {
                new RandomPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new HonestPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new LiarPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new BeliefRevisionPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new ProposedPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
            };

            var player2 = new IPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>[]
            {
                new RandomPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new HonestPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new LiarPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new BeliefRevisionPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
                new ProposedPlayer<CooperativeSpiesGameMove, CooperativeSpiesGame, CooperativeSpiesGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }


        static void EvaluateBattleOfTheSexesGame(int numberOfRuns)
        {
            var game = new BattleOfTheSexesModifiedGame();

            var player1 = new IPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>[]
            {
                new RandomPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new HonestPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new LiarPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new BeliefRevisionPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new ProposedPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
            };

            var player2 = new IPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>[]
            {
                new RandomPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new HonestPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new LiarPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new BeliefRevisionPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
                new ProposedPlayer<BattleOfTheSexesModifiedGameMove, BattleOfTheSexesModifiedGame, BattleOfTheSexesModifiedGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateRockPaperScissorsGame(int numberOfRuns)
        {
            var game = new RockPaperScissorsGame();

            var player1 = new IPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>[]
            {
                new RandomPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new HonestPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new LiarPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new BeliefRevisionPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new ProposedPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
            };

            var player2 = new IPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>[]
            {
                new RandomPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new HonestPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new LiarPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new BeliefRevisionPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
                new ProposedPlayer<RockPaperScissorsGameMove, RockPaperScissorsGame, RockPaperScissorsGameState>(),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateCluedoGame(int numberOfRuns)
        {
            var game = new CluedoGame();

            var player1 = new IPlayer<CluedoGameMove, CluedoGame, CluedoGameState>[]
            {
                new RandomPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(),
                new BluffingPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(),
                new VanillaCFRPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(null, 5),
            };

            var player2 = new IPlayer<CluedoGameMove, CluedoGame, CluedoGameState>[]
            {
                new RandomPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(),
                new BluffingPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(),
                new VanillaCFRPlayer<CluedoGameMove, CluedoGame, CluedoGameState>(null, 5),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }

        static void EvaluateIDoubtItGame(int numberOfRuns)
        {
            var game = new IDoubtItGame();

            var player1 = new IPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>[]
            {
                new RandomPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 4),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 6),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 11),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 21),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 25),
            };

            var player2 = new IPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>[]
            {
                new RandomPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 4),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 6),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 11),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 21),
                new VanillaCFRPlayer<IDoubtItGameMove, IDoubtItGame, IDoubtItGameState>(null, 25),
            };

            EvaluateTwoPlayerGame(game, player1, player2, numberOfRuns);
        }
        
        static void EvaluateTwoPlayerGame<MoveT, GameT, GameStateT>(GameT game, IEnumerable<IPlayer<MoveT, GameT, GameStateT>> player1, IEnumerable<IPlayer<MoveT, GameT, GameStateT>> player2, int numberOfRuns)
            where GameT : IGame<MoveT, GameStateT>
            where GameStateT : IGameState<MoveT>
        {
            Console.WriteLine(game.GetType().Name.Substring(0, game.GetType().Name.Length));
            Console.WriteLine();
            Console.WriteLine($"\t   {string.Join(" | ", player2.Select(p2 => p2.GetType().Name[..^8].PadLeft(14)))}");

            foreach (var p1 in player1)
            {
                Console.Write(p1.GetType().Name[..^8].PadRight(8));

                foreach (var p2 in player2)
                {
                    var totals = new double[game.NumberOfPlayers];
                    var totalWin = new double[game.NumberOfPlayers];

                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        var terminalState = GameManager.Play(game, p1, p2);
                        var utilities = terminalState.GetUtilities();
                        if (utilities[0] > utilities[1]) totalWin[0]++;
                        else if (utilities[1] > utilities[0]) totalWin[1]++;
                        for (int player = 0; player < utilities.Length; player++) totals[player] += utilities[player];
                    }
                    Console.Write(" | " + string.Join(", ", totals.Select(total => string.Format("{0:0.00}", total / numberOfRuns))).PadLeft(14));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
