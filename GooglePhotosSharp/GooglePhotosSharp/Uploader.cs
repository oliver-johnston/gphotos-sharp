using System;
using System.Collections.Generic;
using System.Linq;
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

            var toAddToAlbums = databasePhotos.Values
                .Where(p => p.GoogleUploadId != null
                            && p.GoogleAlbumId == null
                            && p.GoogleMediaItemId == null)
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

            var toUpload = libraryPhotos.Where(p => !databasePhotos.ContainsKey(p.Path)).ToList();
            Log.Information($@"Found {toUpload.Count():n0} photos to upload");

            int uploadedCount = 0;

            foreach (var album in toUpload.GroupBy(p => p.AlbumName).OrderByDescending(a => a.Key))
            {
                Log.Information($"Uploading photos in {album.Key}");
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
                        var uploadedPhoto = await _api.UploadPhotoAsync(photo.Path, photo.ReadAllBytes());
                        var databasePhoto = new Photo
                        {
                            Path = photo.Path,
                            AlbumName = photo.AlbumName,
                            GoogleUploadId = uploadedPhoto.UploadToken
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