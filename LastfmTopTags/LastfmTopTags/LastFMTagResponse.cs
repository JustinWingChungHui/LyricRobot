using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LastfmTopTags
{
    public class Tag
    {
        public int count { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Attr
    {
        public string artist { get; set; }
        public string track { get; set; }
    }

    public class Toptags
    {
        public List<Tag> tag { get; set; }

        [JsonProperty(PropertyName = "@attr")]
        public Attr attr { get; set; }
    }

    public class LastFMTagResponse
    {
        public Toptags toptags { get; set; }
    }
}


