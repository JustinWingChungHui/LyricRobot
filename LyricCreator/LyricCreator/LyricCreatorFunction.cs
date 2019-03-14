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

            // Can call function to warm it up
            if (!string.IsNullOrEmpty(req.Query["warmup"]))
            {
                return new OkObjectResult("OK");
            }

            int lines;
            if (!int.TryParse(req.Query["lines"], out lines))
            {
                lines = 10;
            }

            var suffix = string.Empty;
            if (string.IsNullOrEmpty(req.Query["profanities"]) || req.Query["profanities"] == "false")
            {
                suffix = "Clean";
            }

            var prefix = string.Empty;
            if (!string.IsNullOrEmpty(req.Query["genre"]))
            {
                var genres = (Genre[])Enum.GetValues(typeof(Genre));

                if (genres.Any(g => g.ToString() == req.Query["genre"]))
                {
                    prefix = req.Query["genre"];
                }
            }

            var chain1Name = $"{prefix}MarkovChainOrder1{suffix}";
            var chain2Name = $"{prefix}MarkovChainOrder2{suffix}";
            var lineLengthDistName = $"{prefix}LineLengthDistribution";

            var output = new List<string>();

            log.LogInformation("Loading Markov Data");
            var markovModel = await BlobRepository<MarkovChain>.Get(chain1Name);
            var markovOrder2Model = await BlobRepository<MarkovChain>.Get(chain2Name);
            var lineLengthDistribution = await BlobRepository<LineLengthDistribution>.Get(lineLengthDistName);

            for (int i = 0; i < Math.Min(lines, 200); i++)
            {

                var lyricLine = new List<string>();
               
                var rand = new Random();
                string pred1 = null;
                var pred2 = Word.StartOfLine;
                string successor = null;

                var endOfLine = false;

                log.LogInformation("Building lyric");

                while (!endOfLine)
                {
                    var lineEndProbability = lineLengthDistribution.GetLineEndProbability(lyricLine.Count);

                    // Use first order markov for beginning
                    if (pred1 == null)
                    {
                        successor = GetNextSuccessor(rand, markovModel, pred2, lineEndProbability);
                    }
                    else
                    {
                        // Use 2nd order markov
                        successor = GetNextSuccessor(rand, markovOrder2Model, $"{pred1} {pred2}", lineEndProbability);

                        // if 2nd order fails, fall back to first order
                        if (string.IsNullOrEmpty(successor))
                        {
                            successor = GetNextSuccessor(rand, markovModel, pred2, lineEndProbability);
                        }
                    }

                    if (successor == Environment.NewLine || lyricLine.Count > 50)
                    {
                        endOfLine = true;
                    }  
                    else
                    {
                        lyricLine.Add(successor);
                        pred1 = pred2;
                        pred2 = successor.ToLowerInvariant();
                    }
                    
                }

                output.Add(string.Join(" ", lyricLine));
            }

            return new JsonResult(output);                
        }

        private static string GetNextSuccessor(Random rand, MarkovChain markovChain, string pred, double lineEndProbability)
        {
            string successor = null;

            if (markovChain.Words.ContainsKey(pred))
            {
                var predecessor = markovChain.Words[pred];

                double k_factor = 0;

                if (predecessor.EndOfLineCount > 0 && lineEndProbability > 0.9)                
                {
                    k_factor = (lineEndProbability * predecessor.Successors.Count + lineEndProbability - 1) / (1 - lineEndProbability);
                }

                var endFactor = predecessor.EndOfLineCount + (int)k_factor;
                
                var normalisedCount = predecessor.SuccessorCountTotal + endFactor;
               
                var roll = rand.Next(normalisedCount);

                var successors = predecessor.Successors.OrderBy(s => s.Value.CumulativeCount).ToList();

                if (predecessor.EndOfLineCount > 0)
                {
                    // Add in end of line normalised count
                    successors.Add(new KeyValuePair<string, SuccessorCount>(Environment.NewLine, new SuccessorCount { CumulativeCount = normalisedCount }));
                }

                successor = successors.First(s => s.Value.CumulativeCount >= roll).Key;
            }

            return successor;
        }
    }
}
