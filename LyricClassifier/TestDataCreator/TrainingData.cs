using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDataCreator
{
    public static class TrainingData
    {
        public static async Task Create()
        {
            DocumentDBRepository<SongRecord>.Initialize();

            var popSongs = new List<SongRecord>
            {
                await DocumentDBRepository<SongRecord>.GetItemAsync("1889"), //baby one more time
                await DocumentDBRepository<SongRecord>.GetItemAsync("2261"), // take on me
                await DocumentDBRepository<SongRecord>.GetItemAsync("1147"), // Ariana Into You
                await DocumentDBRepository<SongRecord>.GetItemAsync("1245"), //Avicci Levels
                await DocumentDBRepository<SongRecord>.GetItemAsync("2044"), // Beyonce freedom
                await DocumentDBRepository<SongRecord>.GetItemAsync("181"), // Blondie Denis
                await DocumentDBRepository<SongRecord>.GetItemAsync("413"), // Bros When will I be famous
                await DocumentDBRepository<SongRecord>.GetItemAsync("278"), // Bruno Mars 24K Magic
                await DocumentDBRepository<SongRecord>.GetItemAsync("1201"), // Calvin Harris How Deep is Your Love
                await DocumentDBRepository<SongRecord>.GetItemAsync("1047"), // Dua Lipa One
                await DocumentDBRepository<SongRecord>.GetItemAsync("1803"), // Jonas Blue Rise
            };
            popSongs.ForEach(x => x.Genre = "Pop");


            var hipHopSongs = new List<SongRecord>
            {
                await DocumentDBRepository<SongRecord>.GetItemAsync("2752"), // Snoop Drop it like its hot 
                await DocumentDBRepository<SongRecord>.GetItemAsync("1119"), // Bartier Cardi
                await DocumentDBRepository<SongRecord>.GetItemAsync("1940"), // Eminem Business
                await DocumentDBRepository<SongRecord>.GetItemAsync("1509"), // A$AP Rocky The Lord
                await DocumentDBRepository<SongRecord>.GetItemAsync("1839"), // 50Cent If I Can't
                await DocumentDBRepository<SongRecord>.GetItemAsync("1490"), // Dizzee Rascal Holiday
                await DocumentDBRepository<SongRecord>.GetItemAsync("55"), // Kendrick DNA
                await DocumentDBRepository<SongRecord>.GetItemAsync("2760"), // Drake Worst Behaviour
                await DocumentDBRepository<SongRecord>.GetItemAsync("284"), // Jay-Z Hard Knock Life
                await DocumentDBRepository<SongRecord>.GetItemAsync("436"), // MC Hammer U Can't touch this
            };
            hipHopSongs.ForEach(x => x.Genre = "Hip Hop");


            var ballads = new List<SongRecord>
            {
                await DocumentDBRepository<SongRecord>.GetItemAsync("309"), // Adele hello
                await DocumentDBRepository<SongRecord>.GetItemAsync("2235"), // Ariana God is A Woman
                await DocumentDBRepository<SongRecord>.GetItemAsync("461"), // Coldplay Trouble
                await DocumentDBRepository<SongRecord>.GetItemAsync("153"), // Another Day on Paradise
                await DocumentDBRepository<SongRecord>.GetItemAsync("2179"), // Kelly Clarkson Because of You
                await DocumentDBRepository<SongRecord>.GetItemAsync("1403"), // Everything I Do I Do it for you
                await DocumentDBRepository<SongRecord>.GetItemAsync("1474"), // John Legend All of Me
                await DocumentDBRepository<SongRecord>.GetItemAsync("1310"), // Ed Sheeran Thinking out loud
                await DocumentDBRepository<SongRecord>.GetItemAsync("1335"), // Sam Smith Stay with me
                await DocumentDBRepository<SongRecord>.GetItemAsync("329"), // Let it go
                
            };
            ballads.ForEach(x => x.Genre = "Ballad");


            var rockSongs = new List<SongRecord>
            {
                await DocumentDBRepository<SongRecord>.GetItemAsync("1414"), // Bon Jovi You give love
                await DocumentDBRepository<SongRecord>.GetItemAsync("1630"), // Deep Purple Smoke on the Water
                await DocumentDBRepository<SongRecord>.GetItemAsync("775"), // Dire Straits Money For Nothing
                await DocumentDBRepository<SongRecord>.GetItemAsync("1925"), // Paradise City
                await DocumentDBRepository<SongRecord>.GetItemAsync("2327"), // Welcome to the Black Parade
                await DocumentDBRepository<SongRecord>.GetItemAsync("746"), // Bruise Pristine
                await DocumentDBRepository<SongRecord>.GetItemAsync("823"), // Mr Brightside
                await DocumentDBRepository<SongRecord>.GetItemAsync("844"), // I can't get no satisfaction
                await DocumentDBRepository<SongRecord>.GetItemAsync("68"), // Teenage Dirtbag
                await DocumentDBRepository<SongRecord>.GetItemAsync("1477"), // Bring Me the Horizon Mantra
            };
            rockSongs.ForEach(x => x.Genre = "Rock");

            var songs = popSongs
                        .Concat(hipHopSongs)
                        .Concat(ballads)
                        .Concat(rockSongs)
                        .ToList();

            DocumentDBRepository<SongRecord>.CollectionId = "SongsTrainingData";

            var task = songs.Select(x => DocumentDBRepository<SongRecord>.UpsertItemAsync(x.id, x));
            await Task.WhenAll(task);
        }
    }
}
