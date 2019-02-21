using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using LyricRobotCommon;
using System.Collections.Generic;

namespace LyricCreator
{
    public static class LyricCreatorFunction
    {
        [FunctionName("LyricCreatorFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger LyricCreatorFunction processed a request.");

            int lines;
            if (!int.TryParse(req.Query["lines"], out lines))
            {
                lines = 10;
            }

            var output = new List<string>();

            log.LogInformation("Loading Markov Data");
            var markovModel = await BlobRepository<MarkovChain>.Get("MarkovChainOrder1");

            for (int i = 0; i < Math.Min(lines, 200); i++)
            {

                var lyricLine = new List<string>();
               
                var rand = new Random();
                var predId = Word.StartOfLine;

                var endOfLine = false;

                log.LogInformation("Building lyric");

                while (!endOfLine)
                {
                    var predecessor = markovModel.Words[predId];
                    var roll = rand.Next(predecessor.SuccessorCountTotal);

                    var successors = predecessor.Successors.OrderBy(s => s.Value.CumulativeCount).ToList();
                    string successor = successors.First(s => s.Value.CumulativeCount >= roll).Key;

                    if (successor == Environment.NewLine)
                    {
                        endOfLine = true;
                    }
                    else
                    {
                        lyricLine.Add(successor);
                        predId = successor.ToLowerInvariant();
                    }
                }

                output.Add(string.Join(" ", lyricLine));
            }

            return new JsonResult(output);                
        }
    }
}
