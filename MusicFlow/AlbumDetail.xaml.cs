using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.ViewManagement;
using BackgroundAudioShared;
using BackgroundAudioShared.Messages;
using Lumia.Imaging.Artistic;
using Lumia.Imaging.Adjustments;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicFlow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumDetail : Page
    {
        MainPage mainpage;
        ObservableCollection<Song> songs;
        string Cover;
        string AlbumTitle;
        string AlbumArtist;
        string Year;
        string Genre;
        Lumia.Imaging.Artistic.GrayscaleNegativeEffect ef = new GrayscaleNegativeEffect( );

        public AlbumDetail()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            var f = Window.Current.Content as Frame;
            mainpage = f.Content as MainPage;
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {



            songs = (ObservableCollection<Song>)e.Parameter; ;
            Cover = songs[0].AlbumCover;
            
            AlbumArtist = songs[0].AlbumArtist;
            Year = songs[0].Year;
            Genre = songs[0].Genre;
            if (Genre == null)
            {
                dot.Visibility = Visibility.Collapsed;
            }
            AlbumTitle = songs[0].Album;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedSong = (Song)e.ClickedItem;            
            mainpage.Songs.Clear();
            var song1 = new SongModel();
            song1.Title = clickedSong.Title;
            song1.MediaUri = new Uri(clickedSong.SongFile);
            song1.AlbumArtUri = new Uri(clickedSong.AlbumCover);
            song1.Artist = clickedSong.Artist;
            mainpage.Songs.Add(song1);
            var s1list =new List<SongModel>();
            s1list.Add(song1);
            MessageService.SendMessageToBackground(new UpdatePlaylistMessage(s1list));
            if (!mainpage.IsMyBackgroundTaskRunning || MediaPlayerState.Closed == mainpage.CurrentPlayer.CurrentState)
            {
                // First update the persisted start track
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, song1.MediaUri.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, new TimeSpan().ToString());

                // Start task
                mainpage.StartBackgroundAudioTask();
            }
            MessageService.SendMessageToBackground(new StartPlaybackMessage());
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var clickedSong = (Song)((Button)e.OriginalSource).DataContext;
            var cfile = await StorageFile.GetFileFromPathAsync(clickedSong.SongFile);
            var song1 = new SongModel();
            song1.Title = clickedSong.Title;
            song1.MediaUri = new Uri(clickedSong.SongFile);
            song1.AlbumArtUri = new Uri(clickedSong.AlbumCover);
            song1.Artist = clickedSong.Artist;
            mainpage.Songs.Add(song1);
            MessageService.SendMessageToBackground(new AddToPlaylistMessage(song1));
            
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var g = sender as Grid;
            var rp =  g.Children[1] as RelativePanel;
            var b = rp.Children[2] as Button;
            b.Visibility = Visibility.Collapsed;
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var g = sender as Grid;
            var rp = g.Children[1] as RelativePanel;
            var b = rp.Children[2] as Button;
            b.Visibility = Visibility.Visible;
        }

        
    }

    

}
