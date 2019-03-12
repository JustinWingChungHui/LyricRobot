using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using LyricRobotCommon;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using MusicDemons.Core.Entities;
using Newtonsoft.Json;

namespace GetLyricsFunction
{
    public static class GetLyricsFunction
    {
        [FunctionName("GetLyricsFunction")]
        public static async Task Run([TimerTrigger("0 */20 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var httpClient = new HttpClient();

            log.LogInformation("Initialising Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            log.LogInformation("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == false, 10);
            log.LogInformation($"{songs.FirstOrDefault()?.Title}");

            log.LogInformation("Getting lyrics async");
            var tasks = new List<Task>();

            songs.Take(30).ToList().ForEach(s => tasks.Add(UpdateSong(s, httpClient, log)));

            await Task.WhenAll(tasks);

        }


        private static async Task UpdateSong(SongRecord song, HttpClient httpClient, ILogger log)
        {
            log.LogInformation($"Getting lyrics for '{song.Title}'");

            var lyrics = await httpClient.GetStringAsync($"https://musicdemons.com/api/v1/song/{song.id}/lyrics");

            song.LyricsDownloaded = true;
            song.Lyrics = lyrics;

            await DocumentDBRepository<SongRecord>.UpdateItemAsync(song.id, song);
        }

        [FunctionName("GetArtistFunction")]
        public static async Task GetArtist([TimerTrigger("0 */20 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var httpClient = new HttpClient();

            log.LogInformation("Initialising Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            log.LogInformation("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => true, 10);
            log.LogInformation($"{songs.FirstOrDefault()?.Title}");

            log.LogInformation("Getting artists async");
            var tasks = new List<Task>();

            songs.Where(s => string.IsNullOrEmpty(s.Artist)).Take(30).ToList()
                .ForEach(s => tasks.Add(UpdateArtist(s, httpClient, log)));

            await Task.WhenAll(tasks);

        }

        private static async Task UpdateArtist(SongRecord song, HttpClient httpClient, ILogger log)
        {
            log.LogInformation($"Getting artist for '{song.Title}'");

            var response = await httpClient.GetStringAsync($"https://musicdemons.com/api/v1/song/{song.id}/artists");
            var artists = JsonConvert.DeserializeObject<List<Artist>>(response);

            song.Artist = artists.FirstOrDefault()?.Name;

            await DocumentDBRepository<SongRecord>.UpdateItemAsync(song.id, song);
        }
    }
}
