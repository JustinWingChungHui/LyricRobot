using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.ML;
using LyricRobotCommon;
using MachineLearningCommon;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections.Async;
using System.Collections.Concurrent;

namespace LyricClassifierFunction
{
    public static class LyricClassifierFunction
    {
        [FunctionName("LyricClassifierFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string lyrics = data?.lyrics;
            string warmup = data?.warmup;

            // Can call function to warm it up
            if (!string.IsNullOrEmpty(warmup))
            {
                return new OkObjectResult("OK");
            }

            var result = new GenreResults();

            try
            {
                if (!string.IsNullOrWhiteSpace(lyrics))
                {
                    // These have enough data to form a prediction
                    var genres = new List<Genre>
                    {
                        Genre.Rock,
                        Genre.Electronic,
                        Genre.Alternative,
                        Genre.Indie,
                        Genre.Pop,
                        Genre.Metal,
                        Genre.Folk,
                        Genre.Punk,
                        Genre.HipHop,
                        Genre.SingerSongWriter,
                        Genre.Dance,
                        Genre.Soul,
                        Genre.Acoustic,
                        Genre.Funk
                    };

                    var bag = new ConcurrentBag<GenreResult>();

                    await genres.ParallelForEachAsync(async genre =>
                    {
                        var genreResult = await GetGenreResult(genre, lyrics, log);
                        bag.Add(genreResult);
                    });

                    result.Results = bag.OrderByDescending(g => g.Result.Probability).ToList();

                    result.Message = "Success";
                }
                else
                {
                    result.Message = "No lyrics submitted";
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error: {ex.Message}");
                result.Message = ex.Message;
            }

            return new JsonResult(result);
        }

        private static async Task<GenreResult> GetGenreResult(Genre genre, string lyrics, ILogger log)
        {
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

            log.LogInformation($"Finished loading {genre.ToString()} prediction model.");

            var predEngineTask = new Task<PredictionEngine<Lyric, GenrePrediction>>(() => model.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext));
            predEngineTask.Start();
            var predEngine = await predEngineTask;
            //var predEngine = model.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext);

            log.LogInformation($"Creating {genre.ToString()} prediction model");

            var lyricObj = new Lyric
            {
                Text = lyrics
            };

            log.LogInformation($"Predicting {genre.ToString()}...");

            var genreResultTask = new Task<GenrePrediction>(() => predEngine.Predict(lyricObj));
            genreResultTask.Start();
            var genreResult = await genreResultTask;
            //var genreResult = predEngine.Predict(lyricObj);

            var result =new GenreResult
            {
                Genre = genre.ToString(),
                Result = genreResult
            };
        
            log.LogInformation($"Finished prediction {result.ToString()}");

            return result;
        }
    }
}
