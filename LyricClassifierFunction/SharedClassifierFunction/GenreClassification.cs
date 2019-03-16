using LyricRobotCommon;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using MachineLearningCommon;

namespace SharedClassifierFunction
{
    public static class GenreClassification
    {
        public static async Task<GenreResult> GetGenreResult(Genre genre, string lyrics, ILogger log)
        {
            var timer = new Stopwatch();
            timer.Start();
            log.LogInformation($"Loading {genre.ToString()} prediction model");

            var mlContext = new MLContext(seed: 0);
            ITransformer model;

            using (var stream = await BlobRepository<object>.GetAsStream($"{genre.ToString()}GenrePrediction"))
            {
                var task = new Task<ITransformer>(() => mlContext.Model.Load(stream));
                task.Start();
                model = await task;
                //model = mlContext.Model.Load(stream);
            }

            log.LogInformation($"Finished loading {genre.ToString()} prediction model. {timer.Elapsed.TotalSeconds}sec");
            timer.Restart();

            var predEngineTask = new Task<PredictionEngine<Lyric, GenrePrediction>>(() => model.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext));
            predEngineTask.Start();
            var predEngine = await predEngineTask;
            //var predEngine = model.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext);

            log.LogInformation($"Created {genre.ToString()} prediction model {timer.Elapsed.TotalSeconds}sec");
            timer.Restart();

            var lyricObj = new Lyric
            {
                Text = lyrics
            };

            log.LogInformation($"Predicting {genre.ToString()}...");

            var genreResultTask = new Task<GenrePrediction>(() => predEngine.Predict(lyricObj));
            genreResultTask.Start();
            var genreResult = await genreResultTask;
            //var genreResult = predEngine.Predict(lyricObj);

            var result = new GenreResult
            {
                Genre = genre.ToString(),
                Result = genreResult
            };

            log.LogInformation($"Finished {genre.ToString()} prediction {timer.Elapsed.TotalSeconds}sec");

            return result;
        }
    }
}
