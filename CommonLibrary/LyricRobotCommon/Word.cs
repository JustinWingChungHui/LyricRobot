using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LyricRobotCommon
{
    public class Word
    {
        public static string StartOfLine = "Start Of Line";

        public Word()
        {
            Successors = new Dictionary<string, SuccessorCount>();
        }

        [JsonProperty(PropertyName = "s")]
        public Dictionary<string, SuccessorCount> Successors { get; set; }

        [JsonProperty(PropertyName = "c")]
        public int SuccessorCountTotal { get; set; }
    }
}
