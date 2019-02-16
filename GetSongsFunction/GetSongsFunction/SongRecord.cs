using MusicDemons.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GetSongsFunction
{
    public class SongRecord
    {
        public SongRecord(Song song)
        {
            this.id = song.Id.ToString();
            this.Title = song.Title;
            this.Released = song.Released;
        }

        public string id { get; set; }

        public string Title { get; set; }

        public DateTime? Released { get; set; }

        public bool LyricsDownloaded { get; set; }

    }
}
