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
                return CumulativeLineLengthCount[wordCount];
            }

            return 0;
        }
    }
}
