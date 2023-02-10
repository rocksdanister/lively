using Lively.Common.Models;
using Lively.Common.Services;
using NPSMLib;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Threading;

namespace Lively.PlayerWebView2.Services
{
    public class NpsmNowPlayingService : INowPlayingService
    {
        public event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged;

        private static readonly bool isWindows11_OrGreater = Environment.OSVersion.Version.Build >= 22000;
        private readonly SemaphoreSlim semaphoreSlimLock = new SemaphoreSlim(1, 1);
        private readonly NowPlayingSessionManager manager = new();
        private readonly NowPlayingEventArgs model = new();
        private MediaPlaybackDataSource src;
        private NowPlayingSession session;

        public NpsmNowPlayingService() { }

        public void Start()
        {
            manager.SessionListChanged += SessionListChanged;
            SessionListChanged(null, null);
        }

        public void Stop()
        {
            manager.SessionListChanged -= SessionListChanged;
        }

        private void SessionListChanged(object sender, NowPlayingSessionManagerEventArgs e)
        {
            session = manager.CurrentSession;
            SetupEvents();
            _ = UpdateMedia();
        }

        private void SetupEvents()
        {
            if (session is not null)
            {
                src = session.ActivateMediaPlaybackDataSource();
                src.MediaPlaybackDataChanged += MediaPlaybackDataChanged;
            }
        }

        private void MediaPlaybackDataChanged(object sender, MediaPlaybackDataChangedArgs e) => _ = UpdateMedia();

        private async Task UpdateMedia()
        {
            await semaphoreSlimLock.WaitAsync();

            try
            {
                if (session is not null)
                {
                    var media = src.GetMediaObjectInfo();
                    var mediaPlaybackInfo = src.GetMediaPlaybackInfo();
                    using var thumbnail = src.GetThumbnailStream();

                    switch (mediaPlaybackInfo.PlaybackState)
                    {
                        case MediaPlaybackState.Changing:
                        case MediaPlaybackState.Stopped:
                        case MediaPlaybackState.Playing:
                        case MediaPlaybackState.Paused:
                            {
                                //ignore if title is missing
                                if (string.IsNullOrEmpty(media.Title))
                                    break;

                                //update and fire if track changed or when albumart become available
                                if ((model.Thumbnail is null && thumbnail is not null)
                                    || (media.Title != model.Title && media.AlbumTitle != model.AlbumTitle))
                                {
                                    model.AlbumArtist = media.AlbumArtist;
                                    model.AlbumTitle = media.AlbumTitle;
                                    model.AlbumTrackCount = (int)media.AlbumTrackCount;
                                    model.Artist = media.Artist;
                                    model.Genres = media.Genres?.ToList();
                                    model.PlaybackType = MediaPlaybackDataSource.MediaSchemaToMediaPlaybackMode(media.MediaClassPrimaryID).ToString();
                                    model.Subtitle = media.Subtitle;
                                    model.Thumbnail = thumbnail is null ? null : CreateThumbnail(thumbnail);
                                    model.Title = media.Title;
                                    model.TrackNumber = (int)media.TrackNumber;

                                    NowPlayingTrackChanged?.Invoke(this, model);
                                }
                            }
                            break;
                        case MediaPlaybackState.Unknown:
                        case MediaPlaybackState.Closed:
                        case MediaPlaybackState.Opened:
                            break;
                    }
                }
                else
                {
                    model.Title = model.AlbumTitle = null;
                    NowPlayingTrackChanged?.Invoke(this, null);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                semaphoreSlimLock.Release();
            }
        }

        private static string CreateThumbnail(Stream stream)
        {
            using var ms = new MemoryStream();
            ms.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(ms);
            if (!isWindows11_OrGreater)
            {
                //In Win10 there is transparent borders for some apps
                using var bmp = new Bitmap(ms);
                if (IsPixelAlpha(bmp, 0, 0))
                    return CropImage(bmp, 34, 1, 233, 233);
            }
            var array = ms.ToArray();
            return Convert.ToBase64String(array);
        }

        private static string CropImage(Bitmap bmp, int x, int y, int width, int height)
        {
            var rect = new Rectangle(x, y, width, height);

            using var croppedBitmap = new Bitmap(rect.Width, rect.Height, bmp.PixelFormat);

            var gfx = Graphics.FromImage(croppedBitmap);
            gfx.DrawImage(bmp, 0, 0, rect, GraphicsUnit.Pixel);

            using var ms = new MemoryStream();
            croppedBitmap.Save(ms, ImageFormat.Png);
            byte[] byteImage = ms.ToArray();
            return Convert.ToBase64String(byteImage);
        }

        private static bool IsPixelAlpha(Bitmap bmp, int x, int y) => bmp.GetPixel(x, y).A == (byte)0;
    }
}
