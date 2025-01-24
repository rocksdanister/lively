using Lively.Models.Services;
using NPSMLib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Lively.Common.Services
{
    public class NpsmNowPlayingService : INowPlayingService
    {
        public event EventHandler<NowPlayingEventArgs> NowPlayingTrackChanged;

        private static readonly bool isWindows11_OrGreater = Environment.OSVersion.Version.Build >= 22000;
        private readonly NowPlayingSessionManager manager = new NowPlayingSessionManager();
        private static readonly object lockObject = new object();
        private MediaPlaybackDataSource src;
        private NowPlayingSession session;
        private NowPlayingEventArgs model;

        public NpsmNowPlayingService() { }

        public NowPlayingEventArgs CurrentTrack => model;

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
            UpdateMedia();
        }

        private void SetupEvents()
        {
            if (session is not null)
            {
                src = session.ActivateMediaPlaybackDataSource();
                src.MediaPlaybackDataChanged += MediaPlaybackDataChanged;
            }
        }

        private void MediaPlaybackDataChanged(object sender, MediaPlaybackDataChangedArgs e) => UpdateMedia();

        private void UpdateMedia()
        {
            if (session != null)
            {
                lock (lockObject)
                {
                    try
                    {
                        var media = src.GetMediaObjectInfo();
                        var mediaPlaybackInfo = src.GetMediaPlaybackInfo();
                        using var thumbnail = src.GetThumbnailStream();
                        var thumbnailString = thumbnail is null ? null : CreateThumbnail(thumbnail);

                        switch (mediaPlaybackInfo.PlaybackState)
                        {
                            case MediaPlaybackState.Playing:
                            case MediaPlaybackState.Changing:
                            case MediaPlaybackState.Opened:
                            case MediaPlaybackState.Paused:
                                {
                                    //ignore if title is missing
                                    if (string.IsNullOrEmpty(media.Title))
                                        break;

                                    //Media playback started.
                                    //Rough media changed check.
                                    //Thumbnail available (stream becomes available.)
                                    //Thumbnail updated (stream updated to latest art.)
                                    if (model is null
                                        || media.Title != model.Title || media.Artist != model.Artist
                                        || model.Thumbnail is null && thumbnailString != null
                                        || model.Thumbnail != null && thumbnailString != null && !thumbnailString.Equals(model.Thumbnail))
                                    {
                                        model = new NowPlayingEventArgs
                                        {
                                            AlbumArtist = media.AlbumArtist,
                                            AlbumTitle = media.AlbumTitle,
                                            AlbumTrackCount = (int)media.AlbumTrackCount,
                                            Artist = media.Artist,
                                            Genres = media.Genres?.ToList(),
                                            PlaybackType = MediaPlaybackDataSource.MediaSchemaToMediaPlaybackMode(media.MediaClassPrimaryID).ToString(),
                                            Subtitle = media.Subtitle,
                                            Thumbnail = thumbnailString,
                                            Title = media.Title,
                                            TrackNumber = (int)media.TrackNumber
                                        };
                                        NowPlayingTrackChanged?.Invoke(this, model);
                                    }
                                }
                                break;
                            case MediaPlaybackState.Closed:
                            case MediaPlaybackState.Stopped:
                            case MediaPlaybackState.Unknown:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
            else
            {
                lock (lockObject)
                {
                    model = null;
                    NowPlayingTrackChanged?.Invoke(this, model);
                }
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

        private static bool IsPixelAlpha(Bitmap bmp, int x, int y) => bmp.GetPixel(x, y).A == 0;
    }
}
