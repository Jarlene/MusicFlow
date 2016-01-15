using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicFlow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumView : Page
    {
        Music myMusic = new Music();
        ObservableCollection<Song> albumList;
        static double scposition;

        public AlbumView()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            myMusic = (Music)e.Parameter;
            albumList = myMusic.albumList;
            try
            {
                SC.ScrollToVerticalOffset(scposition);
            }
            catch { }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            scposition =SC.VerticalOffset;
        }

        private void albumView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedAlbum = new ObservableCollection<Song>();
            var clickedItem = (Song)e.ClickedItem;
            var al = clickedItem.Album;
            foreach(var p in myMusic.songList)
            {
                if (p.Album == al)
                {
                    selectedAlbum.Add(p);
                }
            }
            Frame.Navigate(typeof(AlbumDetail), selectedAlbum);
        }
       
    }
}
