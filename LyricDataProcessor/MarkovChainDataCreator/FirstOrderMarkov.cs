using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovChainDataCreator
{
    public static class FirstOrderMarkov
    {
        public static MarkovChain CreateChain(string id, IEnumerable<string> lyrics)
        {
            var markovChain = new MarkovChain
            {
                id = id,
            };

            var chainStart = ProcessStartLyrics(lyrics);
            markovChain.Words.Add(Word.StartOfLine, chainStart);

            Console.WriteLine(string.Empty);
            Console.WriteLine($"Starting Markov Chain Order 1 Data for {id}");


            foreach (var lyric in lyrics)
            {
                var words = lyric.ToLowerInvariant().Split(' ');

                for (int i = 0; i < words.Length - 1; i++)
                {

                    var pred = words[i];
                    var succ = words[i + 1];

                    TallySuccessor(markovChain.Words, pred, succ);
                }

                // Add in terminator
                TallySuccessor(markovChain.Words, words.Last(), Environment.NewLine);
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

        private static Word ProcessStartLyrics(IEnumerable<string> lyrics)
        {
            Console.WriteLine("Creating start word data");

            // Count all the start words
            var dict = new Dictionary<string, SuccessorCount>();
            foreach (var lyric in lyrics)
            {
                var firstWord = lyric.Split(' ').FirstOrDefault();

                if (firstWord != null)
                {
                    if (!dict.ContainsKey(firstWord))
                    {
                        dict.Add(firstWord, new SuccessorCount());
                    }

                    dict[firstWord].Count++;
                }
            }

            // Create object to save to db
            Console.WriteLine("Creating document object");
            var startOfChain = new Word
            {
                Successors = dict
            };

            int cumulativeCount = 0;
            foreach (var entry in dict)
            {
                cumulativeCount += entry.Value.Count;
                entry.Value.CumulativeCount = cumulativeCount;
            }

            startOfChain.SuccessorCountTotal = cumulativeCount;

            return startOfChain;
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
