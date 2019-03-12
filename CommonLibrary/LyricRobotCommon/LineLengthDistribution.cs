using System;
using System.Collections.Generic;
using System.Text;

namespace LyricRobotCommon
{
    public class LineLengthDistribution 
    {
        public LineLengthDistribution()
        {
            CumulativeLineLengthCount = new SortedDictionary<int, double>();
        }

        public int TotalLines { get; set; }

        public SortedDictionary<int, double> CumulativeLineLengthCount { get; set; }

        public double GetLineEndProbability(int wordCount)
        {
            if (CumulativeLineLengthCount.ContainsKey(wordCount))
            {
                return Math.Min(CumulativeLineLengthCount[wordCount], 0.99);
            }

            if (wordCount > CumulativeLineLengthCount.Count)
            {
                return 0.99;
            }

            return 0;
        }
    }
}
