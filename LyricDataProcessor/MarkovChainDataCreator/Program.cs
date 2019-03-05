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

            var lyrics = await GetLyrics();
           
            var markovChain = FirstOrderMarkov.CreateChain(lyrics);

            var markovChain2 = SecondOrderMarkov.CreateChain(lyrics);

            var lineLengthDistribution = WordsPerline.GetLineLengthCount(lyrics);           

            var cleanLyrics = GetCleanLyrics(lyrics);

            var markovChainClean = FirstOrderMarkov.CreateChain(cleanLyrics);

            var markovChain2Clean = SecondOrderMarkov.CreateChain(cleanLyrics);

            // Save to to blob storage           
            Console.WriteLine("Saving order 1 to blob");
            await BlobRepository<MarkovChain>.Create("MarkovChainOrder1", markovChain);

            Console.WriteLine("Saving order 2 to blob");
            await BlobRepository<MarkovChain>.Create("MarkovChainOrder2", markovChain2);

            Console.WriteLine("Saving line length distribution");
            await BlobRepository<LineLengthDistribution>.Create("LineLengthDistribution", lineLengthDistribution);

            Console.WriteLine("Saving clean order 1 to blob");
            await BlobRepository<MarkovChain>.Create("MarkovChainOrder1Clean", markovChainClean);

            Console.WriteLine("Saving clean order 2 to blob");
            await BlobRepository<MarkovChain>.Create("MarkovChainOrder2Clean", markovChain2Clean);
        }

        private static IEnumerable<string> GetCleanLyrics(IEnumerable<string> lyrics)
        {
            var blacklist = GetBlacklistedWords();

            var cleanLyrics = lyrics.Where(l => !l.Split(' ').Any(w => blacklist.Contains(w.ToLowerInvariant())));

            return cleanLyrics;
        }

        private static async Task<IEnumerable<string>> GetLyrics()
        {
            // Get lyrics out of db
            Console.WriteLine("Initialising Songs Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            Console.WriteLine("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == true, -1);

            Console.WriteLine("Processing lyrics");

            var excpList = new HashSet<char>(new[]{ '@', '"', '!', '(', ')', '.', '?', ',', '“', ';' });

            // Remove punctuation and remove blank lines
            var lyrics = songs
                .SelectMany(
                    s => new string(
                        s.Lyrics
                            .Where(c => !excpList.Contains(c)).ToArray())
                            .Split(Environment.NewLine.ToCharArray()))
                .Where(l => l.Count() > 0); ;

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
