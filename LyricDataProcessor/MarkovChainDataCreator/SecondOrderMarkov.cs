using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovChainDataCreator
{
    public static class SecondOrderMarkov
    {
        public static MarkovChain CreateChain(string id, IEnumerable<string> lyrics)
        {
            var markovChain = new MarkovChain
            {
                id = id,
            };

            Console.WriteLine(string.Empty);
            Console.WriteLine($"Starting Markov Chain Order 2 Data for {id}");



            foreach (var lyric in lyrics)
            {
                var words = lyric.ToLowerInvariant().Split(' ');
                string combinedPred = null;

                for (int i = 0; i < words.Length - 2; i++)
                {
                    combinedPred = $"{words[i]} {words[i + 1]}";
                    var succ = words[i + 2];

                    TallySuccessor(markovChain.Words, combinedPred, succ);
                }

                // Add in terminator
                if (combinedPred != null)
                {
                    TallySuccessor(markovChain.Words, combinedPred, Environment.NewLine);
                }
            }

            // Fill in Cumulative counts
            foreach (var word in markovChain.Words.Values)
            {
                int cumulativeCount = 0;
                foreach (var succ in word.Successors.Values)
                {
                    cumulativeCount += succ.Count;
                    succ.CumulativeCount = cumulativeCount;
                }

                word.SuccessorCountTotal = cumulativeCount;
            }

            return markovChain;
        }

        private static void TallySuccessor(Dictionary<string, Word> docObj, string pred, string succ)
        {
            if (!docObj.ContainsKey(pred))
            {
                docObj.Add(pred, new Word());
            }

            if (succ == Environment.NewLine)
            {
                docObj[pred].EndOfLineCount++;
            }
            else
            {
                if (!docObj[pred].Successors.ContainsKey(succ))
                {
                    docObj[pred].Successors.Add(succ, new SuccessorCount());
                }

                docObj[pred].Successors[succ].Count++;
            }
        }
    }
}
