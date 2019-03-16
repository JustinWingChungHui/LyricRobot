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
using SharedClassifierFunction;
using System.Net.Http;
using System.Text;

namespace LyricClassifierFunction
{
    public static class LyricClassifierFunction
    {

        [FunctionName("LyricClassifierFunctionAsync")]
        public static async Task<IActionResult> LyricClassifierFunctionAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string lyrics = data?.lyrics;
            bool warmup = !string.IsNullOrEmpty((string)data?.warmup);

            var httpClient = new HttpClient();

            var endPoint1 = "https://lyricclassifierfunction1.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=51GteH0xptU9sq0rnaMsXBtuGdaYJtVnGXiIhIuo5MO1HSkX01orIQ==";
            var endPoint2 = "https://lyricclassifierfunction2.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=8CxXn9jHxCa3wqjTKCBbisK9ApAC03/Jutp5enqjajAi1NyIHG6u7A==";
            var endPoint3 = "https://lyricclassifierfunction3.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=aya1wD5harodwkuJeaDlLB/fRLMdRyOOaFx6OWIdd0Ue0UnfWxX0xw==";
            var endPoint4 = "https://lyricclassifierfunction4.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=YAniGrTlK8KLFkPteb/TGdgCvXI4Su9vXjYtZr/tXKkL4HolxiZmJQ==";
            var endPoint5 = "https://lyricclassifierfunction5.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=O9n7i8kurkEujUTWm05NSaHsP4GTpnpG8GXo6jcLBuu3AwXoFtDuWw==";
            var endPoint6 = "https://lyricclassifierfunction6.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=KazhZP0UHHJyt4ShC2TL1O8Hl8mAaObJOQuIMrpqwovZp6AzbYxpHA==";
            var endPoint7 = "https://lyricclassifierfunction7.azurewebsites.net/api/LyricClassifierFunctionByGenre?code=nIY0sn/tzgE5k5AM38DuL7O/izbz2Z/oeL6jMaPKUizDUXypRR53FA==";

            var endPointByGenre = new Dictionary<Genre, string>
            {
                {Genre.Rock, endPoint1 },
                {Genre.Electronic, endPoint2 },
                {Genre.Alternative, endPoint3 },
                {Genre.Indie, endPoint4 },
                {Genre.Pop, endPoint5 },
                {Genre.Metal, endPoint6 },
                {Genre.Folk, endPoint7 },
                {Genre.Punk, endPoint1 },
                {Genre.HipHop, endPoint2 },
                {Genre.SingerSongWriter, endPoint3 },
                {Genre.Dance, endPoint4 },
                {Genre.Soul, endPoint5 },
                {Genre.Acoustic, endPoint6 },
                {Genre.Funk, endPoint7 },

            };

            var result = new GenreResults();

            try
            {
                var tasks = new List<Task<GenreResult>>();

                var basePostData = new Dictionary<string, string>
                {
                    { "lyrics", lyrics }
                };

                if (warmup)
                {
                    basePostData.Add("warmup", "true");
                };

                foreach (var kvp in endPointByGenre)
                {
                    tasks.Add(CreateHttpCalls(httpClient, kvp.Key,kvp.Value, basePostData, warmup, log));
                }

                var results = await Task.WhenAll(tasks);

                if (warmup)
                {
                    log.LogInformation($"All endpoints warmed up");
                    return new OkObjectResult("OK");
                };

                result.Results = results.OrderByDescending(g => g.Result.Probability).ToList();

                result.Message = "Success";

            }
            catch (Exception ex)
            {
                log.LogInformation($"Error: {ex.Message}");
                result.Message = ex.Message;
            }

            return new JsonResult(result);
        }

        private static async Task<GenreResult> CreateHttpCalls(
            HttpClient httpClient, 
            Genre genre, 
            string endpoint, 
            Dictionary<string, string> 
            basePostData, 
            bool warmUp,
            ILogger log)
        {
            log.LogInformation($"Assessing {genre.ToString()} at endpoint {endpoint}");

            var postData = new Dictionary<string, string>(basePostData.Select(x => x));
            postData.Add("genre", genre.ToString());

            var json = JsonConvert.SerializeObject(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content);

            if (warmUp)
            {
                return null;
            }

            var stringContent = await response.Content.ReadAsStringAsync();
            var genreResult = JsonConvert.DeserializeObject<GenreResult>(stringContent);

            log.LogInformation($"Received StatusCode:{response.StatusCode} from {endpoint}");

            return genreResult;
        }

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
                log.LogInformation($"Warmup called");
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
                        var genreResult = await GenreClassification.GetGenreResult(genre, lyrics, log);
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

        [FunctionName("LyricClassifierFunctionByGenre")]
        public static async Task<IActionResult> RunByGenre(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string lyrics = data?.lyrics;
            string genreName = data?.genre;
            string warmup = data?.warmup;

            // Can call function to warm it up
            if (!string.IsNullOrEmpty(warmup))
            {
                log.LogInformation($"Warmup called");
                return new OkObjectResult("OK");
            }

            var result = new GenreResult();

            try
            {
                if (!string.IsNullOrWhiteSpace(lyrics) && !string.IsNullOrWhiteSpace(genreName) && Enum.TryParse(genreName, true, out Genre genre))
                {
                    result = await GenreClassification.GetGenreResult(genre, lyrics, log);
                    return new JsonResult(result);
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error: {ex.Message}");
            }

            return new JsonResult(result);
        }
    }
}
