using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Data;

namespace MachineLearningCommon
{
    public class Lyric
    {
        [LoadColumn(0)]
        public string Text { get; set; }

        [LoadColumn(1), ColumnName("Label")]
        public bool Genre { get; set; }
    }

    public class GenrePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }
}
