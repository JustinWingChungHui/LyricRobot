using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricDataProcessorConsole
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

            var markovChain = new MarkovChain
            {
                id = "MarkovChainOrder1",
            };

            var lyrics = await GetLyrics();

            var chainStart = ProcessStartLyrics(lyrics);
            markovChain.Words.Add(Word.StartOfLine, chainStart);

            Console.WriteLine(string.Empty);
            Console.WriteLine("Starting Markov Chain Order 1 Data");


            foreach (var lyric in lyrics)
            {
                var words = lyric.ToLowerInvariant().Split(' ');

                for (int i = 0; i < words.Length - 1; i++)
                {

                    var pred = words[i];
                    var succ = words[i + 1];

                    TallySuccessor(markovChain.Words, pred, succ);
                }

                // Add in terminator
                TallySuccessor(markovChain.Words, words.Last(), Environment.NewLine);
            }

            // Fill in Cumulative counts
            foreach (var word in markovChain.Words.Values)
            {
                int cumulativeCount = 0;
                foreach (var succ in word.Successors.Values)
                {
                    cumulativeCount += succ.Count;
                    succ.CumulativeCount = cumulativeCount;
                }

                word.SuccessorCountTotal = cumulativeCount;
            }

            // Save to to blob storage           
            Console.WriteLine("Saving to blob");
            await BlobRepository<MarkovChain>.Create("MarkovChainOrder1", markovChain);      
        }

       
        private static Word ProcessStartLyrics(IEnumerable<string> lyrics)
        {
            Console.WriteLine("Createing start word data");

            // Count all the start words
            var dict = new Dictionary<string, SuccessorCount>();
            foreach (var lyric in lyrics)
            {
                var firstWord = lyric.Split(' ').FirstOrDefault();

                if (firstWord != null)
                {
                    if (!dict.ContainsKey(firstWord))
                    {
                        dict.Add(firstWord, new SuccessorCount());
                    }

                    dict[firstWord].Count++;
                }
            }

            // Create object to save to db
            Console.WriteLine("Creating document object");
            var startOfChain = new Word
            {
                Successors = dict
            };

            int cumulativeCount = 0;
            foreach (var entry in dict)
            {
                cumulativeCount += entry.Value.Count;
                entry.Value.CumulativeCount = cumulativeCount;
            }

            startOfChain.SuccessorCountTotal = cumulativeCount;

            return startOfChain;
        }

        private static void TallySuccessor(Dictionary<string, Word> docObj, string pred, string succ)
        {
            if (!docObj.ContainsKey(pred))
            {
                docObj.Add(pred, new Word());
            }

            if (!docObj[pred].Successors.ContainsKey(succ))
            {
                docObj[pred].Successors.Add(succ, new SuccessorCount());
            }

            docObj[pred].Successors[succ].Count++;
        }

        private static async Task<IEnumerable<string>> GetLyrics()
        {
            // Get lyrics out of db
            Console.WriteLine("Initialising Songs Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            Console.WriteLine("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == true, -1);

            Console.WriteLine("Processing lyrics");

            var excpList = new HashSet<char>(new[]{ '@', '"', '!', '(', ')', '.', '?', ',', '“' });

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
    }
}
