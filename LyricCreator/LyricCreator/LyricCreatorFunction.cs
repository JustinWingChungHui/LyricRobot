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
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger LyricCreatorFunction processed a request.");

            var lyrics = new List<string>();

            log.LogInformation("Initialising Words Repo");
            DocumentDBRepository<Word>.Initialize("Words");

            var rand = new Random();
            var predId = "Start of Line";
           
            var endOfLine = false;

            log.LogInformation("Building lyric");

            while (!endOfLine)
            {
                var predecessor = await DocumentDBRepository<Word>.GetItemAsync(predId);
                var roll = rand.Next(predecessor.SuccessorCountTotal);

                var successors = predecessor.successors.Values.OrderBy(s => s.CumulativeCount).ToList();
                string successor = successors.First(s => s.CumulativeCount >= roll).Word;

                log.LogInformation(successor);

                lyrics.Add(successor);

                endOfLine = successor == Environment.NewLine;
                predId = successor.ToLowerInvariant();
            }

            return new JsonResult(string.Join(" ", lyrics));
                
        }
    }
}
