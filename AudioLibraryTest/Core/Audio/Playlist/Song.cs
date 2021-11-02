using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace player.Core.Audio.Playlist
{
    class Song
    {
        public string FilePath { get; private set; }
        public int SongID { get; private set; }

        public Song(string filePath, int songId)
        {
            FilePath = filePath;
            SongID = songId;
        }
    }
}
