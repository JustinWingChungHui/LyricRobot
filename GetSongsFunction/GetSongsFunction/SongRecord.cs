using MusicDemons.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GetSongsFunction
{
    public class SongRecord : Song
    {
        public bool LyricsDownloaded { get; set; }
    }
}
