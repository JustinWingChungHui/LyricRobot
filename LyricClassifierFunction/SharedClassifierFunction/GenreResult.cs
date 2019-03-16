using MachineLearningCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedClassifierFunction
{
    public class GenreResult
    {
        public GenreResult()
        {
            Result = new GenrePrediction();
        }

        public string Genre { get; set; }

        public GenrePrediction Result { get; set; }

        public override string ToString()
        {
            return $"Genre:{Genre} Prediction:{Result.Prediction} Probablility:{Result.Probability} Score:{Result.Score}";
        }
    }
}
