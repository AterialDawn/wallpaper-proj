using System.Collections.Generic;
using System.IO;

//Temporarily ignore warnings since the playlist manager isn't ready.
#pragma warning disable 0414
namespace player.Core.Audio.Playlist
{
    //This class will manage music playback, such as starting/stopping playback, determining when music playback is nearing the end of the current song to begin preloading the next song, and other stuff
    class PlaylistManager
    {
        private static PlaylistManager _instance = new PlaylistManager();
        public static PlaylistManager Instance { get { return _instance; } }

        public IEnumerable<Song> SongList { get { foreach (KeyValuePair<int, Song> kvp in songList) yield return kvp.Value; } } //Check if performance of this is acceptable

        private Dictionary<int, Song> songList = new Dictionary<int, Song>();
        private PlayMode playMode = PlayMode.Normal;
        private int currentSongIndex = 0;


        private PlaylistManager()
        {

        }

        public Song AddSongToPlaylist(string filePath)
        {
            //Verify that the file exists, and add the song to playlist
            if (!File.Exists(filePath)) throw new FileNotFoundException("No file", filePath);
            int songId = filePath.GetHashCode();
            if (songList.ContainsKey(songId))
            {
                return songList[songId];
            }
            else
            {
                Song currentSong = new Song(filePath, songId);
                songList.Add(songId, currentSong);
                return currentSong;
            }

        }

        public void RemoveSongFromPlaylist(Song song)
        {
            if (songList.ContainsKey(song.SongID))
            {
                songList.Remove(song.SongID);
            }
        }

        public void RemoveSongFromPlaylist(int songID)
        {
            if (songList.ContainsKey(songID))
            {
                songList.Remove(songID);
            }
        }

        public void ClearPlaylist(bool stopPlayback)
        {
            songList.Clear();
            if (stopPlayback)
            {
                StopPlayback();
            }
        }

        public void StartPlayback(bool restart)
        {
            if (restart)
            {
                currentSongIndex = 0;
            }

        }

        public void StopPlayback()
        {
        }

        public void SetPlaymode(PlayMode newMode)
        {
            if (newMode != playMode)
            {

            }
        }
    }
}
