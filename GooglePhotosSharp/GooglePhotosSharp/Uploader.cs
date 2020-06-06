using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GooglePhotosSharp.Config;
using GooglePhotosSharp.Database;
using GooglePhotosSharp.GooglePhotos;
using GooglePhotosSharp.LocalPhotos;
using MoreLinq;
using Serilog;

namespace GooglePhotosSharp
{
    public class Uploader
    {
        private readonly PhotoLibraryConfig _config;
        private readonly ILocalPhotoLibrary _library;
        private readonly IDatabaseRepository _database;
        private readonly IGooglePhotosApi _api;

        public Uploader(
            PhotoLibraryConfig config,
            ILocalPhotoLibrary library,
            IDatabaseRepository database,
            IGooglePhotosApi api)
        {
            _config = config;
            _library = library;
            _database = database;
            _api = api;
        }

        public async Task UploadPhotos()
        {
            Log.Information($"Loading photos for {_config.Path}");

            var libraryPhotos = _library.GetAllPhotos();
            var databasePhotos = _database.GetPhotos().ToDictionary(x => x.Path);

            var googleAlbums = (await _api.GetAlbumsAsync()).Where(a => a.IsWriteable).ToList();

            // add any photos that have been uploaded but haven't been added to albums yet (e.g. if the program terminated)
            await AddUploadedPhotosMissingFromAlbums(databasePhotos.Values.ToList(), googleAlbums);

            var toUpload = libraryPhotos.Where(p =>
                {
                    if (!databasePhotos.TryGetValue(p.Path, out var databasePhoto))
                    {
                        return true;
                    }

                    // If a photo is in the library but doesn't have a media item yet then we need to re-upload it.
                    return databasePhoto.GoogleMediaItemId == null;
                })
                .Where(f => f.FileLength > 0)
                .ToList();

            Log.Information($@"Found {toUpload.Count:n0} photos to upload");

            int uploadedCount = 0;

            foreach (var album in toUpload.GroupBy(p => p.AlbumName).OrderBy(a => a.Key))
            {
                Log.Information($"Uploading {album.Count():n0} photos in {album.Key}");
                var googleAlbum = googleAlbums.FirstOrDefault(a => a.Title == album.Key);

                if (googleAlbum == null)
                {
                    googleAlbum = await _api.CreateAlbumAsync(album.Key);
                }

                var uploadedPhotos = new List<Photo>();
                foreach (var photo in album.OrderBy(p => p.Path))
                {
                    Log.Information($"Uploading {photo.Path}");
                    try
                    {
                        var description = RemoveDiacritics(photo.Path);
                        var uploadedPhoto = await _api.UploadPhotoAsync(description, photo.ReadAllBytes());
                        var databasePhoto = new Photo
                        {
                            Path = photo.Path,
                            AlbumName = photo.AlbumName,
                            GoogleUploadId = uploadedPhoto.UploadToken,
                            UploadTime = DateTime.UtcNow
                        };
                        _database.AddOrUpdate(databasePhoto);
                        uploadedPhotos.Add(databasePhoto);

                        uploadedCount++;

                        if (uploadedCount % 100 == 0)
                        {
                            Log.Information($"Uploaded [{uploadedCount:n0}/{toUpload.Count:n0}] photos");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Unable to upload {photo.Path}.", e);
                    }
                }

                await AddPhotosToAlbum(uploadedPhotos, googleAlbum);
            }

            Log.Information($"Uploaded {uploadedCount:n0} photos");
        }

        private async Task AddUploadedPhotosMissingFromAlbums(
            IList<Photo> databasePhotos,
            List<GooglePhotosAlbum> googleAlbums)
        {
            // If the program terminated before
            var toAddToAlbums = databasePhotos
                .Where(p => p.GoogleUploadId != null
                            && p.GoogleAlbumId == null
                            && p.GoogleMediaItemId == null
                            && p.UploadTime != null
                            // An upload token is only valid for 24 hours. After this we need to re-upload the photo.
                            && (DateTime.UtcNow - p.UploadTime.Value) < TimeSpan.FromHours(24))
                .ToList();

            foreach (var album in toAddToAlbums.GroupBy(p => p.AlbumName))
            {
                var googleAlbum = googleAlbums.FirstOrDefault(a => a.Title == album.Key);

                if (googleAlbum == null)
                {
                    googleAlbum = await _api.CreateAlbumAsync(album.Key);
                }

                await AddPhotosToAlbum(album.ToList(), googleAlbum);
            }
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task AddPhotosToAlbum(IList<Photo> uploadedPhotos, GooglePhotosAlbum googleAlbum)
        {
            Log.Information($"Adding {uploadedPhotos.Count:n0} to album {googleAlbum.Title}");

            foreach (var batch in uploadedPhotos.Batch(50))
            {
                var mediaItems = await _api.AddUploadsToAlbumAsync(googleAlbum.Id, batch.Select(p =>
                    new UploadedPhoto
                    {
                        FileName = p.Path,
                        UploadToken = p.GoogleUploadId
                    }).ToList());

                foreach (var mediaItem in mediaItems)
                {
                    var databasePhoto = batch.Single(u => u.GoogleUploadId == mediaItem.UploadToken);
                    if (mediaItem.Status.Message == "Success")
                    {
                        databasePhoto.GoogleMediaItemId = mediaItem.MediaItem.Id;
                        databasePhoto.GoogleAlbumId = googleAlbum.Id;
                        _database.AddOrUpdate(databasePhoto);
                    }
                    else
                    {
                        Log.Error($"Error uploading {databasePhoto.Path}: {mediaItem.Status.Message}");
                    }
                }
            }
        }
    }
}