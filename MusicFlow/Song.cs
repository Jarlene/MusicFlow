using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicFlow
{
    public class Song
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Year { get; set; }
        public string TrackNo { get; set; }
        public string Genre { get; set; }
        public string Length { get; set; }
        public string SongFile { get; set; }
        public string AlbumCover;

    }

    public class Music
    {
        public ObservableCollection<Song> songList;
        public ObservableCollection<Song> albumList;

        public Music(ObservableCollection<Song> x, ObservableCollection<Song> y)
        {
            songList = x;
            albumList = y;
        }

        public Music()
        {
        }
    }
}
