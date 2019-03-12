using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LyricRobotCommon;
using MusicDemons.Core.Entities;
using Newtonsoft.Json;

namespace LastfmTopTags
{
    class Program
    {
        static void Main(string[] args)
        {
            
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            DocumentDBRepository<SongRecord>.Initialize();

            Console.WriteLine("Get songs out of CosmosDB");
            var songsNoGenres = await DocumentDBRepository<SongRecord>.GetItemsAsync(x => true, -1);

            var httpclient = new HttpClient();
            var apiKey = ConfigurationManager.AppSettings["LastfmAPIKey"];

            foreach (var song in songsNoGenres.Where(s => !string.IsNullOrEmpty(s.Artist)))
            {
                // Strip out unwanted characters
                var artist = Regex.Replace(song.Artist.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
                artist = artist.Replace(' ', '+');
                
                var title = Regex.Replace(song.Title.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
                title = title.Replace(' ', '+');

                var request = $"http://ws.audioscrobbler.com/2.0/?method=track.gettoptags&artist={artist}&track={title}&api_key={apiKey}&format=json";

                //Console.WriteLine($"Requesting {artist}: {title}");

                var tagResponse = await httpclient.GetStringAsync(request);
                var tags = JsonConvert.DeserializeObject<LastFMTagResponse>(tagResponse);

                song.Genre = GetGenre(tags);
                var genres = string.Join(',', song.Genre);
                Console.WriteLine($"Artist:{artist} Title:{title} Genres:{genres}");

                await DocumentDBRepository<SongRecord>.UpdateItemAsync(song.id, song);

            }
        }

        static List<string> GetGenre(LastFMTagResponse tags)
        {
            var result = new HashSet<string>();

            if (tags?.toptags?.tag != null)
            {
                foreach (var tag in tags.toptags.tag.Take(5))
                {
                    var match = GenreMatcher.Match(tag.name);
                    if (match.HasValue && !result.Contains(match.Value.ToString()))
                    {
                        result.Add(match.Value.ToString());
                    }

                }
            }

            return result.ToList();
        }
    }
}
