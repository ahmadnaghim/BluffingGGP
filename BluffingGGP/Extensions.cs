using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlayer
{
    public static class Extensions
    {
        internal static T Choose<T>(this Random random, IEnumerable<T> list) => list.ElementAt(random.Next(list.Count()));
        internal static int GetIndex<T>(this T enumValue) where T : Enum => Array.IndexOf(Enum.GetValues(enumValue.GetType()), enumValue);
        internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> list) => list.SelectMany(e => e);
        internal static IEnumerable<T> Maximise<T>(this IEnumerable<T> list, Func<T, double> getScore) => list.Maximise(e => e, getScore);

        internal static T ChooseWithWeights<T, U>(this Random random, IEnumerable<(T, int, U)> list)
        {
            var weightedAllocations = new List<T>();

            foreach ((var value, var weight, var _) in list)
            {
                for (var i = 0; i < weight; i++) weightedAllocations.Add(value);
            }

            return random.Choose(weightedAllocations);
        }

        internal static IEnumerable<ResultT> Maximise<StartT, ResultT>(this IEnumerable<StartT> list, Func<StartT, ResultT> mapElement, Func<StartT, double> getScore)
        {
            double bestScore = 0;

            return list.Aggregate(new List<ResultT>(), (maximalElements, e) =>
            {
                var score = getScore(e);

                if (score > bestScore)
                {
                    bestScore = score;
                    maximalElements.Clear();
                    maximalElements.Add(mapElement(e));
                }
                else if (score == bestScore)
                {
                    maximalElements.Add(mapElement(e));
                }

                return maximalElements;
            });
        }

        /**
         * Taken from https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
         */
        internal static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }

    public class InformationSet
    {
        internal String key;
        internal int totalMove;
        internal List<double> regretSum, strategySum, strategy;
        internal double reachPr, reachPrSum;

        public InformationSet(String key, int totalMove)
        {
            // Initiate variable
            this.key = key;
            this.totalMove = totalMove;
            this.regretSum = new List<double>(new double[totalMove]);
            this.strategySum = new List<double>(new double[totalMove]);
            this.strategy = new List<double>(new double[totalMove]);
            for (var i = 0; i < totalMove; i++)
            {
                this.strategy[i] = 1 / totalMove;
            }
            this.reachPr = 0;
            this.reachPrSum = 0;
        }

        internal void NextStrategy()
        {
            for (var i = 0; i < totalMove; i++)
            {
                strategySum[i] += reachPr * strategy[i];
            }
            strategy = CalcStrategy();
            reachPrSum += reachPr;
            reachPr = 0;
        }

        internal List<double> CalcStrategy()
        {
            var strategy = MakePositive(regretSum);
            var total = strategy.Sum();
            if (total > 0)
            {
                strategy = strategy.Select(e => e / total).ToList();
            }
            else
            {
                strategy = strategy.Select(e => 1.0 / totalMove).ToList();
            }
            this.strategy = strategy;
            return strategy;
        }

        internal List<double> GetAverageStrategy()
        {
            var strategy = strategySum.Select(x => x / reachPrSum).ToList();
            // Remove value less than 0.001
            strategy = strategy.Select(x => x < 0.001 ? 0 : x).ToList();

            // Normalize the strategy
            var total = strategy.Sum();
            strategy = strategy.Select(x => x / total).ToList();

            return strategy;
        }

        internal double GetAverageRegret()
        { 
            var regret = MakePositive(regretSum);
            var total = regret.Sum() / totalMove;
            return total;
        }


        internal List<double> MakePositive(List<double> x)
        {
            return x.Select(e => Math.Max(e, 0)).ToList();
        }
    }
}
