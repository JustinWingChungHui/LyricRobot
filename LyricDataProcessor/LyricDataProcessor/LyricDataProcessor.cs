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

namespace LyricDataProcessor
{
    public static class LyricDataProcessor
    {
        [FunctionName("ProcessStartLyrics")]
        public static async Task<IActionResult> ProcessStartLyrics(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function ProcessStartLyrics processed a request.");

            var lyrics = await GetLyrics(log);
            
            // Count all the start words
            var dict = new Dictionary<string, SuccessorCount>();
            foreach (var lyric in lyrics)
            {
                var firstWord = lyric.Split(" ").FirstOrDefault();

                if (firstWord != null)
                {
                    if (!dict.ContainsKey(firstWord))
                    {
                        dict.Add(firstWord, new SuccessorCount { Word = firstWord });
                    }

                    dict[firstWord].Count++;
                }
            }

            // Create object to save to db
            log.LogInformation("Creating document object");
            var word = new Word
            {
                id = "Start of Line",
                successors = dict
            };

            int cumulativeCount = 0;
            foreach (var entry in dict)
            {
                cumulativeCount += entry.Value.Count;
                entry.Value.CumulativeCount = cumulativeCount;
            }

            // Save to db
            log.LogInformation("Initialising Words Repo");
            DocumentDBRepository<Word>.Initialize("Words");

            log.LogInformation("Saving to db");
            await DocumentDBRepository<Word>.UpsertItemAsync(word.id, word);


            return new OkObjectResult($"OK");
        }

        [FunctionName("ProcessChainLyrics")]
        public static async Task<IActionResult> ProcessChainLyrics(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function ProcessChainLyrics processed a request.");

            var lyrics = await GetLyrics(log);

            var docObj = new Dictionary<string, Word>();
            foreach (var lyric in lyrics)
            {
                var words = lyric.ToLowerInvariant().Split(" ");

                for(int i = 0; i < words.Length - 1; i++)
                {
                    var pred = words[i];
                    var succ = words[i + 1];

                    TallySuccessor(docObj, pred, succ);
                }

                // Add in terminator
                TallySuccessor(docObj, words.Last(), Environment.NewLine);
            }

            // Fill in Cumulative counts
            foreach (var word in docObj.Values)
            {
                int cumulativeCount = 0;
                foreach (var succ in word.successors.Values)
                {
                    cumulativeCount += succ.Count;
                    succ.CumulativeCount = cumulativeCount;
                }
            }

            // Save to db
            log.LogInformation("Initialising Words Repo");
            DocumentDBRepository<Word>.Initialize("Words");

            log.LogInformation("Saving to db");
            var tasks = new List<Task>();

            docObj.Values.ToList().ForEach(w => tasks.Add(DocumentDBRepository<Word>.UpsertItemAsync(w.id, w)));

            await Task.WhenAll(tasks);

            return new OkObjectResult($"OK");
        }

        private static void TallySuccessor(Dictionary<string, Word> docObj, string pred, string succ)
        {
            if (!docObj.ContainsKey(pred))
            {
                docObj.Add(pred, new Word { id = pred });
            }

            if (!docObj[pred].successors.ContainsKey(succ))
            {
                docObj[pred].successors.Add(succ, new SuccessorCount { Word = succ });
            }

            docObj[pred].successors[succ].Count++;
        }

        private static async Task<IEnumerable<string>> GetLyrics(ILogger log)
        {
            // Get lyrics out of db
            log.LogInformation("Initialising Songs Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            log.LogInformation("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == true, -1);

            log.LogInformation("Processing lyrics");

            // Remove punctuation and remove blank lines
            var lyrics = songs
                .SelectMany(
                    s => new string(
                        s.Lyrics
                            .Where(c => !char.IsPunctuation(c)).ToArray())
                            .Split(Environment.NewLine))
                .Where(l => l.Count() > 0); ;

            return lyrics;
        }

    }
}
