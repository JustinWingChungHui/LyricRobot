using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkovChainDataCreator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }
        
        public static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Starting...");

            Console.WriteLine("Initialising Songs Repo");
            DocumentDBRepository<SongRecord>.Initialize();
            Console.WriteLine("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == true, -1);

            Console.WriteLine("=============================================");
            Console.WriteLine("Processing All Songs");
            var allLyrics = GetLyrics(songs);
           
            var allMarkovChain = FirstOrderMarkov.CreateChain("MarkovChainOrder1", allLyrics);

            var allMarkovChain2 = SecondOrderMarkov.CreateChain("MarkovChainOrder2", allLyrics);

            var lineLengthDistribution = WordsPerline.GetLineLengthCount(allLyrics);           

            var allCleanLyrics = GetLyrics(songs, null, allowProfanities: false);

            var allMarkovChainClean = FirstOrderMarkov.CreateChain("MarkovChainOrder1Clean", allCleanLyrics);

            var allMarkovChain2Clean = SecondOrderMarkov.CreateChain("MarkovChainOrder2Clean", allCleanLyrics);
            
            // Save to to blob storage           
            Console.WriteLine("Saving order 1 to blob");
            await BlobRepository<MarkovChain>.Create(allMarkovChain.id, allMarkovChain);

            Console.WriteLine("Saving order 2 to blob");
            await BlobRepository<MarkovChain>.Create(allMarkovChain2.id, allMarkovChain2);

            Console.WriteLine("Saving line length distribution");
            await BlobRepository<LineLengthDistribution>.Create("LineLengthDistribution", lineLengthDistribution);

            Console.WriteLine("Saving clean order 1 to blob");
            await BlobRepository<MarkovChain>.Create(allMarkovChainClean.id, allMarkovChainClean);

            Console.WriteLine("Saving clean order 2 to blob");
            await BlobRepository<MarkovChain>.Create(allMarkovChain2Clean.id, allMarkovChain2Clean);

            /// Other Genres
            var genres = (Genre[])Enum.GetValues(typeof(Genre));
            foreach (var genre in genres)
            {
                Console.WriteLine("=============================================");
                Console.WriteLine($"Processing Genre: {genre.ToString()}");
                var genreLyrics = GetLyrics(songs, genre);
                var chain1 = FirstOrderMarkov.CreateChain($"{genre.ToString()}MarkovChainOrder1",genreLyrics);
                var chain2 = SecondOrderMarkov.CreateChain($"{genre.ToString()}MarkovChainOrder2", genreLyrics);
                var lineLengthDist = WordsPerline.GetLineLengthCount(genreLyrics);

                var cleanGenreLyrics = GetLyrics(songs, genre, allowProfanities: false);
                var cleanChain1 = FirstOrderMarkov.CreateChain($"{genre.ToString()}MarkovChainOrder1Clean", cleanGenreLyrics);
                var cleanChain2 = SecondOrderMarkov.CreateChain($"{genre.ToString()}MarkovChainOrder2Clean", cleanGenreLyrics);

                Console.WriteLine("Saving order 1 to blob");
                await BlobRepository<MarkovChain>.Create(chain1.id, chain1);

                Console.WriteLine("Saving order 2 to blob");
                await BlobRepository<MarkovChain>.Create(chain2.id, chain2);

                Console.WriteLine("Saving line length distribution");
                await BlobRepository<LineLengthDistribution>.Create($"{genre.ToString()}LineLengthDistribution", lineLengthDist);

                Console.WriteLine("Saving clean order 1 to blob");
                await BlobRepository<MarkovChain>.Create(cleanChain1.id, cleanChain1);

                Console.WriteLine("Saving clean order 2 to blob");
                await BlobRepository<MarkovChain>.Create(cleanChain2.id, cleanChain2);
            }
        }

        private static IEnumerable<string> GetLyrics(IEnumerable<SongRecord> songs, Genre? genre = null, bool allowProfanities = true)
        {           
            var excpList = new HashSet<char>(new[]{ '@', '"', '!', '(', ')', '.', '?', ',', '“', ';' });

            IEnumerable<SongRecord> filteredSongs;
            if (genre.HasValue)
            {
                filteredSongs = songs.Where(s => s.Genre.Contains(genre.ToString()));
            }
            else
            {
                filteredSongs = songs;
            }

            // Remove punctuation and remove blank lines
            var lyrics = filteredSongs
                .SelectMany(
                    s => new string(
                        s.Lyrics
                            .Where(c => !excpList.Contains(c)).ToArray())
                            .Split(Environment.NewLine.ToCharArray()))
                .Where(l => l.Count() > 0); ;

            

            if (!allowProfanities)
            {
                var blacklist = GetBlacklistedWords();
                lyrics = lyrics.Where(l => !l.Split(' ').Any(w => blacklist.Contains(w.ToLowerInvariant())));
            }

            return lyrics;
        }

        private static HashSet<string> GetBlacklistedWords()
        {
            var result = new HashSet<string>
            {
                "shit", "shitted", "shitting",
                "fuck", "fucked", "fucking", "fucker", "fuckin'",
                "motherfucker", "motherfuckers", "mutherfucka", "mutherfuckas",
                "nigga", "niggas", "nigger", "niggers", "niggerz",
                "cunt", "cunts",
                "bitch", "bitches",                
                "piss", "pissed",
                "cock", "cocks", "cocksucker", "cocksuckers",
                "tit", "tits",
                "dick", "dicks",
                "titty", "titties",
                "pussy", "pussies",
                "ass", 
            };

            return result;
        }
    }
}
