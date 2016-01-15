using BackgroundAudioShared;
using BackgroundAudioShared.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MusicFlow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<SongModel> songs = new ObservableCollection<SongModel>();
        public ObservableCollection<SongModel> Songs
        {
            get
            {
                return songs;
            }
        }
        private AutoResetEvent backgroundAudioTaskStarted;
        private bool _isMyBackgroundTaskRunning = false;
        private Dictionary<string, BitmapImage> albumArtCache = new Dictionary<string, BitmapImage>();
        const int RPC_S_SERVER_UNAVAILABLE = -2147023174;

        Music myMusic = new Music();
        public Song NowPlayingSongData;
        public ApplicationViewTitleBar titlebar;
        //public MediaElement MusicFlowME = new MediaElement();
        public MainPage()
        {
            backgroundAudioTaskStarted = new AutoResetEvent(false);
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.InitializeComponent();
            titlebar = ApplicationView.GetForCurrentView().TitleBar;
            Color black = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            Color white = new Color() { A = 255, R = 255, G = 255, B = 255 };
            Color grey = new Color() { A = 255, R = 100, G = 100, B = 100 };
            titlebar.BackgroundColor = black;
            titlebar.InactiveBackgroundColor = black;
            titlebar.ButtonBackgroundColor = black;
            titlebar.ButtonInactiveBackgroundColor = black;
            titlebar.ForegroundColor = white;
            titlebar.InactiveForegroundColor = white;
            titlebar.ButtonForegroundColor = white;
            titlebar.ButtonInactiveForegroundColor = white;
            titlebar.ButtonHoverForegroundColor = grey;
            titlebar.ButtonHoverBackgroundColor = black;

            ((ViewModel)Application.Current.Resources["ViewModel"]).Title = "MusicFlow";
            ((ViewModel)Application.Current.Resources["ViewModel"]).Cover = "ms-appx:///Assets/main.png";
            ((ViewModel)Application.Current.Resources["ViewModel"]).Album = "For Windows 10";
            ((ViewModel)Application.Current.Resources["ViewModel"]).PropertyChanged += new PropertyChangedEventHandler(ViewModel_prpChanged);
            SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) =>
            {
                
                //var pageType = (Window.Current.Content as Frame).Content.GetType().Name;
                var currentPage = MainFrame.CurrentSourcePageType.ToString();
                switch (currentPage)
                {
                    case "MusicFlow.AlbumView":
                        ShowPlayer.Begin();
                        break;

                    case "MusicFlow.AlbumDetail":
                        ShowPlayer.Begin();
                        e.Handled = true;
                        MainFrame.GoBack();
                        break;

                    case "MusicFlow.NowPlaying":
                        ShowPlayer.Begin();
                        e.Handled = true;
                        MainFrame.GoBack();
                        break;

                    default :
                        break;
                }
            };
            MusicFlowME.MediaEnded += NowPlayingSong_MediaEnded;
        }

        private void NowPlayingSong_MediaEnded(object sender, RoutedEventArgs e)
        {
            Pause.Icon = new SymbolIcon(Symbol.Play);
        }

        private void ViewModel_prpChanged(object sender, PropertyChangedEventArgs e)
        {
            NowPlayingFlip.Stop();
            NowPlayingFlip2.Stop();
            NowPlayingFlip.Begin();
            NowPlayingFlip2.Begin();
            Pause.Icon = new SymbolIcon(Symbol.Pause);
        }

        
        public async Task GetMusic()
        {
            var folder = KnownFolders.MusicLibrary;
            var songfilelist = new ObservableCollection<StorageFile>();
            await initCollection(folder,songfilelist);
            myMusic = await createSongList(songfilelist);
            await serialize(myMusic);
            
        }

        private async Task initCollection(StorageFolder folder,ObservableCollection<StorageFile>songfilelist)
        {
            foreach (var item in await folder.GetFilesAsync())
            {
                if (item.FileType == ".mp3" || item.FileType == ".m4a")
                {
                    songfilelist.Add(item);
                }
            }
            foreach (var item in await folder.GetFoldersAsync())
            {
               await initCollection(item,songfilelist);
            }
        }

        private async Task<Music> createSongList(ObservableCollection<StorageFile> songfilelist)
        {
            StorageFolder localFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("CoverArt", CreationCollisionOption.OpenIfExists);
            
            var songList = new ObservableCollection<Song>();
            var albumList = new ObservableCollection<Song>();
            var albumTitleList = new ObservableCollection<string>();
            foreach(var p in songfilelist)
            {
               var prop = await p.Properties.GetMusicPropertiesAsync();
                
                StorageItemThumbnail currentThumb = await p.GetThumbnailAsync(
                   ThumbnailMode.MusicView,
                   200,
                   ThumbnailOptions.UseCurrentScale);

                var song = new Song();
                song.Title = prop.Title;
                song.Artist = prop.Artist;
                song.Album = prop.Album;
                song.AlbumCover = string.Format("ms-appdata:///local/CoverArt/Cover_{0}_{1}.jpg", song.Album, song.Artist);
                song.SongFile = p.Path;
                song.AlbumArtist = prop.AlbumArtist;
                try { song.Genre = prop.Genre[0].ToString(); }
                catch { }
                song.TrackNo = prop.TrackNumber.ToString();
                song.Year = prop.Year.ToString();
                song.Length = prop.Duration.Minutes.ToString()+":" + prop.Duration.Seconds.ToString();
                songList.Add(song);
                if (!albumTitleList.Contains(song.Album))
                {
                    albumTitleList.Add(song.Album);
                    albumList.Add(song);
                    var fname = string.Format("Cover_{0}_{1}.jpg", song.Album, song.Artist);
                    try {
                        var file = await localFolder.CreateFileAsync(fname);
                        Windows.Storage.Streams.Buffer buff = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(currentThumb.Size));
                        IBuffer iBuff = await currentThumb.ReadAsync(buff, buff.Capacity, InputStreamOptions.None);
                        using (var strm = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await strm.WriteAsync(iBuff);

                        }
                    }
                    catch { }
                }
            }
            Music music = new Music(songList, albumList);
            return music;
        }
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                myMusic = await deserialize();
            }
            catch { }
            if (myMusic.albumList == null || myMusic.songList == null)
            {
                await GetMusic();
            }
            ProgressbarMain.ShowPaused = true;
            MainFrame.Navigate(typeof(AlbumView), myMusic);


            var MediaElementObject = new MediaElement();

            Application.Current.Suspending += ForegroundApp_Suspending;
            Application.Current.Resuming += ForegroundApp_Resuming;
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Active.ToString());
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_isMyBackgroundTaskRunning)
            {
                RemoveMediaPlayerEventHandlers();
                ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Running.ToString());
            }

            base.OnNavigatedFrom(e);
        }

        private void StackPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MainFrame.Navigate(typeof(NowPlaying));
            HidePlayer.Begin();
        }

        private async Task serialize(Music mymusic)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ObservableCollection<Song>));
            var file1 = await ApplicationData.Current.LocalFolder.CreateFileAsync("MusicData.dat", CreationCollisionOption.ReplaceExisting);
            var file2 = await ApplicationData.Current.LocalFolder.CreateFileAsync("AlbumData.dat", CreationCollisionOption.ReplaceExisting);
            Stream ms = await file1.OpenStreamForWriteAsync();
            Stream ms2 = await file2.OpenStreamForWriteAsync();
            serializer.WriteObject(ms, mymusic.songList);
            serializer.WriteObject(ms2, mymusic.albumList);
            ms.Dispose();
            ms2.Dispose();
        }

        private async Task<Music> deserialize()
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ObservableCollection<Song>));
            var f1 = await ApplicationData.Current.LocalFolder.CreateFileAsync("MusicData.dat", CreationCollisionOption.OpenIfExists);
            var f2 = await ApplicationData.Current.LocalFolder.CreateFileAsync("AlbumData.dat", CreationCollisionOption.OpenIfExists);
            Stream ms = await f1.OpenStreamForReadAsync();
            Stream ms2 = await f2.OpenStreamForReadAsync();
            var songlist = (ObservableCollection<Song>)serializer.ReadObject(ms);
            var albumlist = (ObservableCollection<Song>)serializer.ReadObject(ms2);
            ms.Dispose();
            ms2.Dispose();
            var music = new Music(songlist, albumlist);
            return music;
        }

        private async Task Deletecache()
        {
            var f1 = await ApplicationData.Current.LocalFolder.GetFileAsync("AlbumData.dat");
            var f2 = await ApplicationData.Current.LocalFolder.GetFileAsync("MusicData.dat");
            await f1.DeleteAsync();
            await f2.DeleteAsync();
            myMusic.albumList = null;
            myMusic.songList = null;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (songs.Count == 0)
            {

            }
            // Start the background task if it wasn't running
            else if (!IsMyBackgroundTaskRunning || MediaPlayerState.Closed == CurrentPlayer.CurrentState)
            {
                try
                {
                    // First update the persisted start track
                    ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, songs[0].MediaUri.ToString());
                    ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, new TimeSpan().ToString());

                    // Start task
                    StartBackgroundAudioTask();
                }
                catch { }
    
                
            }
            if (MediaPlayerState.Paused == CurrentPlayer.CurrentState)
            {
                CurrentPlayer.Play();
            }

            if (MediaPlayerState.Playing == CurrentPlayer.CurrentState)
            {
                CurrentPlayer.Pause();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(typeof(BlankPage));
            ProgressbarMain.ShowPaused = false;
            await Deletecache();
            await GetMusic();
            ProgressbarMain.ShowPaused = true;
            MainFrame.Navigate(typeof(AlbumView), myMusic);
        }

        public bool IsMyBackgroundTaskRunning
        {
            get
            {
                if (_isMyBackgroundTaskRunning)
                    return true;

                string value = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.BackgroundTaskState) as string;
                if (value == null)
                {
                    return false;
                }
                else
                {
                    try
                    {
                        _isMyBackgroundTaskRunning = EnumHelper.Parse<BackgroundTaskState>(value) == BackgroundTaskState.Running;
                    }
                    catch (ArgumentException)
                    {
                        _isMyBackgroundTaskRunning = false;
                    }
                    return _isMyBackgroundTaskRunning;
                }
            }
        }

        public MediaPlayer CurrentPlayer
        {
            get
            {
                MediaPlayer mp = null;
                int retryCount = 2;

                while (mp == null && --retryCount >= 0)
                {
                    try
                    {
                        mp = BackgroundMediaPlayer.Current;
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
                        {
                            // The foreground app uses RPC to communicate with the background process.
                            // If the background process crashes or is killed for any reason RPC_S_SERVER_UNAVAILABLE
                            // is returned when calling Current. We must restart the task, the while loop will retry to set mp.
                            ResetAfterLostBackground();
                            StartBackgroundAudioTask();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (mp == null)
                {
                    throw new Exception("Failed to get a MediaPlayer instance.");
                }

                return mp;
            }
        }

        private void ResetAfterLostBackground()
        {
            BackgroundMediaPlayer.Shutdown();
            _isMyBackgroundTaskRunning = false;
            backgroundAudioTaskStarted.Reset();
            //prevButton.IsEnabled = true;
            //nextButton.IsEnabled = true;
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Unknown.ToString());
            Pause.Icon = new SymbolIcon(Symbol.Pause);

            try
            {
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            }
            catch (Exception ex)
            {
                if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
                {
                    throw new Exception("Failed to get a MediaPlayer instance.");
                }
                else
                {
                    throw;
                }
            }
        }

        private Uri GetCurrentTrackIdAfterAppResume()
        {
            object value = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.TrackId);
            if (value != null)
                return new Uri((String)value);
            else
                return null;
        }

        void ForegroundApp_Resuming(object sender, object e)
        {
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Active.ToString());

            // Verify the task is running
            if (IsMyBackgroundTaskRunning)
            {
                // If yes, it's safe to reconnect to media play handlers
                AddMediaPlayerEventHandlers();

                // Send message to background task that app is resumed so it can start sending notifications again
                MessageService.SendMessageToBackground(new AppResumedMessage());

                UpdateTransportControls(CurrentPlayer.CurrentState);

                var trackId = GetCurrentTrackIdAfterAppResume();
                NowPlayingTitle.Text = trackId == null ? string.Empty : songs.Single(s => s.MediaUri == trackId).Title; 
                //txtCurrentState.Text = CurrentPlayer.CurrentState.ToString();
            }
            else
            {
                Pause.Icon = new SymbolIcon(Symbol.Play);     // Change to play button
                NowPlayingTitle.Text = string.Empty;
                //txtCurrentState.Text = "Background Task Not Running";
            }
        }

        void ForegroundApp_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // Only if the background task is already running would we do these, otherwise
            // it would trigger starting up the background task when trying to suspend.
            if (IsMyBackgroundTaskRunning)
            {
                // Stop handling player events immediately
                RemoveMediaPlayerEventHandlers();

                // Tell the background task the foreground is suspended
                MessageService.SendMessageToBackground(new AppSuspendedMessage());
            }

            // Persist that the foreground app is suspended
            ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Suspended.ToString());

            deferral.Complete();
        }

        async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            var currentState = sender.CurrentState; // cache outside of completion or you might get a different value
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Update state label
                //txtCurrentState.Text = currentState.ToString();

                // Update controls
                UpdateTransportControls(currentState);
            });
        }

        async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            TrackChangedMessage trackChangedMessage;
            if (MessageService.TryParseMessage(e.Data, out trackChangedMessage))
            {
                // When foreground app is active change track based on background message
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // If playback stopped then clear the UI
                    if (trackChangedMessage.TrackId == null)
                    {
                        //playlistView.SelectedIndex = -1;
                        //NowPlatingCover.Source = null;
                        //NowPlayingTitle.Text = string.Empty;
                        //NowPlayingArtist.Text = string.Empty;
                        return;
                    }

                    var songIndex = songs.IndexOf(songs.FirstOrDefault(p => p.MediaUri == trackChangedMessage.TrackId));
                    var song = songs[songIndex];

                    NowPlatingCover.Source = new BitmapImage(new Uri(song.AlbumArtUri.ToString(), UriKind.Absolute));
                    NowPlayingTitle.Text = song.Title;
                    NowPlayingArtist.Text = song.Artist;
                    NowPlayingFlip.Stop();
                    NowPlayingFlip2.Stop();
                    NowPlayingFlip.Begin();
                    NowPlayingFlip2.Begin();
                    //prevButton.IsEnabled = true;
                    ////nextButton.IsEnabled = true;
                });
                return;
            }

            BackgroundAudioTaskStartedMessage backgroundAudioTaskStartedMessage;
            if (MessageService.TryParseMessage(e.Data, out backgroundAudioTaskStartedMessage))
            {
                // StartBackgroundAudioTask is waiting for this signal to know when the task is up and running
                // and ready to receive messages
                Debug.WriteLine("BackgroundAudioTask started");
                backgroundAudioTaskStarted.Set();
                return;
            }
        }

        //private void Pause_Click(object sender, ItemClickEventArgs e)
        //{
        //    var random = new Random();
        //    var s1 = myMusic.songList[random.Next(0,myMusic.songList.Count())];
        //    var song1 = new SongModel();
        //    song1.Title = s1.Title;
        //    song1.MediaUri = new Uri(s1.SongFile);
        //    song1.AlbumArtUri = new Uri(s1.AlbumCover);
        //    Songs.Add(song1);
           

        //    // Start the background task if it wasn't running
        //    if (!IsMyBackgroundTaskRunning || MediaPlayerState.Closed == CurrentPlayer.CurrentState)
        //    {
        //        // First update the persisted start track
        //        ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, song1.MediaUri.ToString());
        //        ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, new TimeSpan().ToString());

        //        // Start task
        //        StartBackgroundAudioTask();
        //    }
        //    else
        //    {
        //        // Switch to the selected track
        //        MessageService.SendMessageToBackground(new TrackChangedMessage(song1.MediaUri));
        //    }

        //    if (MediaPlayerState.Paused == CurrentPlayer.CurrentState)
        //    {
        //        CurrentPlayer.Play();
        //    }
        //}

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Play button pressed from App");
            if (IsMyBackgroundTaskRunning)
            {
                if (MediaPlayerState.Playing == CurrentPlayer.CurrentState)
                {
                    CurrentPlayer.Pause();
                }
                else if (MediaPlayerState.Paused == CurrentPlayer.CurrentState)
                {
                    CurrentPlayer.Play();
                }
                else if (MediaPlayerState.Closed == CurrentPlayer.CurrentState)
                {
                    StartBackgroundAudioTask();
                }
            }
            else
            {
                StartBackgroundAudioTask();
            }
        }

        private void UpdateTransportControls(MediaPlayerState state)
        {
            if (state == MediaPlayerState.Playing)
            {
                Pause.Icon = new SymbolIcon(Symbol.Pause);    // Change to pause button
            }
            else
            {
                Pause.Icon = new SymbolIcon(Symbol.Play);   // Change to play button
            }
        }

        private void RemoveMediaPlayerEventHandlers()
        {
            try
            {
                BackgroundMediaPlayer.Current.CurrentStateChanged -= this.MediaPlayer_CurrentStateChanged;
                BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
            }
            catch (Exception ex)
            {
                if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
                {
                    // do nothing
                }
                else
                {
                    throw;
                }
            }
        }

        private void AddMediaPlayerEventHandlers()
        {
            CurrentPlayer.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;

            try
            {
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            }
            catch (Exception ex)
            {
                if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
                {
                    // Internally MessageReceivedFromBackground calls Current which can throw RPC_S_SERVER_UNAVAILABLE
                    ResetAfterLostBackground();
                }
                else
                {
                    throw;
                }
            }
        }

        public void StartBackgroundAudioTask()
        {
            AddMediaPlayerEventHandlers();

            var startResult = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bool result = backgroundAudioTaskStarted.WaitOne(10000);
                //Send message to initiate playback
                if (result == true)
                {
                    MessageService.SendMessageToBackground(new UpdatePlaylistMessage(songs.ToList()));
                    MessageService.SendMessageToBackground(new StartPlaybackMessage());
                }
                else
                {
                    throw new Exception("Background Audio Task didn't start in expected time");
                }
            });
            startResult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
        }

        private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            MessageService.SendMessageToBackground(new SkipNextMessage());
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            MessageService.SendMessageToBackground(new SkipPreviousMessage());
        }
    }
}
