using System;
using System.Collections.Generic;
using System.Text;

namespace LyricRobotCommon
{
    public class MarkovChain
    {
        public MarkovChain()
        {
            Words = new Dictionary<string, Word>();
        }

        public string id { get; set; }

        public Dictionary<string, Word> Words { get; set; }
    }
}
