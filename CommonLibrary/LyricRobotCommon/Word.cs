using System;
using System.Collections.Generic;
using System.Text;

namespace LyricRobotCommon
{
    public class Word
    {
        public Word()
        {
            successors = new Dictionary<string, SuccessorCount>();
        }

        public string id { get; set; }

        public Dictionary<string, SuccessorCount> successors { get; set; }

        public int SuccessorCountTotal { get; set; }
    }
}
