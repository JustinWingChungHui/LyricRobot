using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MusicDemons.Core.Entities;
using Newtonsoft.Json.Linq;

namespace LyricScraper
{
    class Program
    {

        static HttpClient _httpClient;

        static long _totalSongCount;
        static long _progressCount;

        static System.Timers.Timer _timer;

        static void Main(string[] args)
        {
            LoadData().Wait();
        }

        public async static Task LoadData()
        {
            _httpClient = new HttpClient();

            Console.WriteLine("Getting song ids...");
            var songIds = await GetSongIds();
            _totalSongCount = songIds.LongCount();

            Console.WriteLine("Getting Lyrics...");
            _timer = new System.Timers.Timer();
            _timer.Elapsed += UpdateProgress;
            _timer.Interval = 1000;
            _timer.Start();

            var lyricLines = await GetLyricLines(songIds);           
        }

        private static void UpdateProgress(object sender, ElapsedEventArgs e)
        {
            Console.Write($"\rDownloaded {_progressCount} songs out of {_totalSongCount}");
        }

        private async static Task<IEnumerable<long>> GetSongIds()
        {
            
            var response = await _httpClient.GetStringAsync("https://musicdemons.com/api/v1/song");

            var json = JArray.Parse(response);
            var ids = json.Select(j => long.Parse(j["id"].ToString()));

            return ids;
        }

        private static async Task<List<string>> GetLyricLines(IEnumerable<long> songIds)
        {
            var lyricLines = new ConcurrentBag<string>();
            var tasks = new List<Task>();
            
            foreach (var songId in songIds)
            {
                tasks.Add(AddLyricLines(songId, lyricLines));
            }

            await Task.WhenAll(tasks);

            return lyricLines.ToList();
        }

        private async static Task AddLyricLines(long songId, ConcurrentBag<string> lyricLines)
        {
            var url = $"https://musicdemons.com/api/v1/song/{songId}/lyrics";
            var response = await _httpClient.GetStringAsync(url);



            var strippedOfPunctuation = new string(response.Where(c => !char.IsPunctuation(c)).ToArray());

            var lines = strippedOfPunctuation
                            .Split(Environment.NewLine)
                            .Where(l => l.Count() > 0);

            foreach (var line in lines)
            {
                lyricLines.Add(line);
            }

            _progressCount++;
            //Interlocked.Increment(ref _progressCount);
        }
    }
}
