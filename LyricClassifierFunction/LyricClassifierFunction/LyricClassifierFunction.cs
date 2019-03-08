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

            string lyrics = req.Query["lyrics"];

            if (!string.IsNullOrWhiteSpace(lyrics))
            {
                var mlContext = new MLContext(seed: 0);
                ITransformer model;

                using (var stream = await BlobRepository<object>.GetAsStream("GenrePrediction"))
                {
                    model = mlContext.Model.Load(stream);
                }

                var predEngine = model.CreatePredictionEngine<Lyric, GenrePrediction>(mlContext);

                var lyricObj = new Lyric
                {
                    Text = lyrics
                };

                var result = predEngine.Predict(lyricObj);

                return new JsonResult(result);
            }
            else
            {
                return new BadRequestObjectResult("No lyrics specified");
            }
        }
    }
}
