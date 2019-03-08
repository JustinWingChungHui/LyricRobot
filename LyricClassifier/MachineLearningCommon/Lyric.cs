using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Data;

namespace MachineLearningCommon
{
    public class Lyric
    {
        public string Text { get; set; }

        public string Genre { get; set; }
    }

    public class GenrePrediction
    {
        [ColumnName("PredictedLabel")]
        public string Genre { get; set; }
    }
}
