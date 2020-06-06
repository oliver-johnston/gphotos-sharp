using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;

namespace GooglePhotosSharp.Database
{
    public class DatabaseRepository : IDatabaseRepository
    {
        private const string PhotosTable = "photos";

        private readonly string _path;

        private readonly string _connectionString;

        public DatabaseRepository(string path)
        {
            _path = path;
            var databaseFile = Path.Combine(path, "gpo.db");
            _connectionString = $"filename={databaseFile};mode=Exclusive";
        }

        public IList<Photo> GetPhotos()
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                return db.GetCollection<Photo>(PhotosTable)
                    .FindAll()
                    .Select(p => new Photo
                    {
                        Path = Path.Combine(_path, p.Path),
                        AlbumName = p.AlbumName,
                        GoogleUploadId = p.GoogleUploadId,
                        GoogleMediaItemId = p.GoogleMediaItemId,
                        GoogleAlbumId = p.GoogleAlbumId,
                        UploadTime = p.UploadTime
                    })
                    .ToList();
            }
        }

        public void AddOrUpdate(Photo p)
        {
            var dbPhoto = new Photo
            {
                Path = Path.GetRelativePath(_path, p.Path),
                AlbumName = p.AlbumName,
                GoogleUploadId = p.GoogleUploadId,
                GoogleMediaItemId = p.GoogleMediaItemId,
                GoogleAlbumId = p.GoogleAlbumId,
                UploadTime = p.UploadTime
            };

            using (var db = new LiteDatabase(_connectionString))
            {
                var table = db.GetCollection<Photo>(PhotosTable);
                table.Upsert(dbPhoto);
            }
        }
    }
}