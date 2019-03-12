using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LyricRobotCommon
{
    public static class GenreMatcher
    {
        public static Genre? Match(string genreDesc)
        {
            var desc = genreDesc.ToLowerInvariant();

            var allGenres = (Genre[])Enum.GetValues(typeof(Genre));

            // Check for exact match
            foreach (var genre in allGenres)
            {
                if (genre.ToString().ToLowerInvariant() == desc)
                {
                    return genre;
                }
            }

            // Alternative spellings and synonyms exact match
            var hiphop = new HashSet<string> { "hip hop", "hip-hop", "grime", "rap" };

            if (hiphop.Contains(desc))
            {
                return Genre.HipHop;
            }

            var singerSongwriter = new HashSet<string> { "singer-songwriter", "singer songwriter" };
            if (singerSongwriter.Contains(desc))
            {
                return Genre.SingerSongWriter;
            }

            var electronic = new HashSet<string> { "industrial", "hardcore", "electronica", "trance" };
            if (electronic.Contains(desc))
            {
                return Genre.Electronic;
            }

            var femaleVocalists = new HashSet<string> { "female-vocalists", "female vocalists", "female vocalist", "female-vocalist" };
            if (femaleVocalists.Contains(desc))
            {
                return Genre.Electronic;
            }

            // Check for containing match
            foreach (var genre in allGenres)
            {
                if (desc.Contains(genre.ToString().ToLowerInvariant()))
                {
                    return genre;
                }
            }

            // Alternative spellings and synonyms containing match
            if (hiphop.Any(x => desc.Contains(x)))
            {
                return Genre.HipHop;
            }

            if (singerSongwriter.Any(x => desc.Contains(x)))
            {
                return Genre.SingerSongWriter;
            }

            if (electronic.Any(x => desc.Contains(x)))
            {
                return Genre.Electronic;
            }

            if (femaleVocalists.Any(x => desc.Contains(x)))
            {
                return Genre.FemaleVocalists;
            }


            // No match
            return null;
        }
    }

    
    public enum Genre
    {
        //Top LastFM tags
        Rock,
        Electronic, 
        Alternative,
        Indie,
        Pop,
        FemaleVocalists,
        Metal,
        Jazz,
        Experimental,
        Folk,
        Punk,
        HipHop,
        SingerSongWriter,
        Dance,
        Soul,
        Acoustic,
        Funk,
        Eurovision
    }
}
