using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LyricRobotCommon
{
    public class SuccessorCount
    {
        [JsonIgnore]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "c")]
        public int CumulativeCount { get; set; }
    }
}
