using MachineLearningCommon;
using SharedClassifierFunction;
using System;
using System.Collections.Generic;
using System.Text;

namespace LyricClassifierFunction
{
    public class GenreResults
    {
        public GenreResults()
        {
            Results = new List<GenreResult>();
        }

        public string Message { get; set; }

        public List<GenreResult> Results { get; set; }
    }
}
