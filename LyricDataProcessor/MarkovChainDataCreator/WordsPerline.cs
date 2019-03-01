using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarkovChainDataCreator
{
    public static class WordsPerline
    {
        public static LineLengthDistribution GetLineLengthCount(IEnumerable<string> lyrics)
        {
            var result = new LineLengthDistribution();

            var lineLengthCount = new SortedDictionary<int, int>();

            // Count the lines
            foreach (var lyricLine in lyrics)
            {
                var wordCount = lyricLine.Split(' ').Length;

                if (!lineLengthCount.ContainsKey(wordCount))
                {
                    lineLengthCount.Add(wordCount, 0);
                }

                lineLengthCount[wordCount]++;
                result.TotalLines++;
            }

            // Calculate cumulative count
            int count = 0;
            foreach (var kvp in lineLengthCount)
            {
                count += kvp.Value;
                var probability = (double) count / result.TotalLines;
                result.CumulativeLineLengthCount.Add(kvp.Key, probability);
            }

            return result;
        }
    }
}
