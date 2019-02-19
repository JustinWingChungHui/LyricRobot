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
        private static int objectsSaved = 0;
        private static int TotalObjects = 0;

        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Starting Lyrics processor");

            var lyrics = await GetLyrics();

            await ProcessStartLyrics(lyrics);

            var docObj = new Dictionary<string, Word>();
            foreach (var lyric in lyrics)
            {
                var words = lyric.ToLowerInvariant().Split(' ');

                for (int i = 0; i < words.Length - 1; i++)
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

                word.SuccessorCountTotal = cumulativeCount;
            }

            // Save to db
            Console.WriteLine("Initialising Words Repo");
            DocumentDBRepository<Word>.Initialize("MarkovChain1");

            Console.WriteLine("Saving to db");
            var tasks = new List<Task>();
            TotalObjects = docObj.Values.Count;

            docObj.Values.ToList().ForEach(w => tasks.Add(UpsertItem(w.id, w)));

            await Task.WhenAll(tasks);
        }

        private static async Task UpsertItem(string id, Word w)
        {
            await DocumentDBRepository<Word>.UpsertItemAsync(w.id, w);
            objectsSaved++;
            Console.Write($"\rSaved {objectsSaved} out of {TotalObjects}");
        }

        private static async Task ProcessStartLyrics(IEnumerable<string> lyrics)
        {
            Console.WriteLine("Processing start lyrics");

            // Count all the start words
            var dict = new Dictionary<string, SuccessorCount>();
            foreach (var lyric in lyrics)
            {
                var firstWord = lyric.Split(' ').FirstOrDefault();

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
            Console.WriteLine("Creating document object");
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

            word.SuccessorCountTotal = cumulativeCount;

            // Save to db
            Console.WriteLine("Initialising Words Repo");
            DocumentDBRepository<Word>.Initialize("MarkovChain1");

            Console.WriteLine("Saving to db");
            await DocumentDBRepository<Word>.UpsertItemAsync(word.id, word);
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

        private static async Task<IEnumerable<string>> GetLyrics()
        {
            // Get lyrics out of db
            Console.WriteLine("Initialising Songs Repo");
            DocumentDBRepository<SongRecord>.Initialize();

            Console.WriteLine("Getting songs out of db");
            var songs = await DocumentDBRepository<SongRecord>.GetItemsAsync(s => s.LyricsDownloaded == true, -1);

            Console.WriteLine("Processing lyrics");

            var excpList = new HashSet<char>(new[]{ '@', '"', '!', '(', ')', '.', '?', ',' });

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
