using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace MusicFlow
{
    public sealed class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler PlayRequested;


        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

         bool SetProperty<T>(ref T storage,T value,[CallerMemberName] string properyName=null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(properyName);
            return true;
        }

        private void OnPlayRequested([CallerMemberName] string propertyName=null)
        {
            if (this.PlayRequested != null)
            {
                this.PlayRequested(this, EventArgs.Empty);
            }
            this.nowplaying.Play();
        }

        private string title;
        private string album;
        private string cover;
        private MediaElement nowplaying;


        public string Title 
        {
            get { return this.title; }
            set { this.SetProperty(ref this.title, value); OnPropertyChanged("Name"); }
        }
        public string Album
        {
            get { return this.album; }
            set { this.SetProperty(ref this.album, value); OnPropertyChanged("Album"); }
        }
        public string Cover
        {
            get { return this.cover; }
            set { this.SetProperty(ref this.cover, value); OnPropertyChanged("Cover"); }
        }
        public MediaElement NowPlaying
        {
            get { return this.nowplaying; }
            set { this.SetProperty(ref this.nowplaying, value); OnPropertyChanged("NowPlaying"); }
        }

    }
}
