using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;



namespace BackgroundAudioShared.Messages
{
    [DataContract]
    public class AddToPlaylistMessage
    {
        public AddToPlaylistMessage(SongModel song)
        {
            this.Song = song;
        }

        [DataMember]
        public SongModel Song;
    }
}