using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using GooglePhotosSharp.Database;
using NUnit.Framework;

namespace GooglePhotosSharp.Tests.Database
{
    /// <summary>
    /// Tests that reading/writing to the database works.
    /// Actually saves to the file system so we can test that the serialization works.
    /// </summary>
    [TestFixture(Category = "Functional")]
    public class DatabaseRepositoryTests
    {
        [Test]
        public void TestAddRetrieveUpdatePhotos()
        {
            var temp = Path.GetTempPath();
            var guid = Guid.NewGuid().ToString().Substring(0, 6);
            var directory = Path.Combine(temp, guid);
            Directory.CreateDirectory(directory);
            try
            {
                var repository = new DatabaseRepository(directory);

                var photos = repository.GetPhotos();
                Assert.That(photos, Is.Empty);

                for (int i = 0; i < 1000; i++)
                {
                    repository.AddOrUpdate(new Photo
                    {
                        Path = i.ToString(),
                        AlbumName = $"Album{i}",
                        GoogleUploadId = $"Uploader{i}"
                    });
                }
                
                photos = repository.GetPhotos();
                Assert.That(photos.Count, Is.EqualTo(1000));

                for (int i = 0; i < photos.Count; i++)
                {
                    var photo = photos[i];
                    photo.GoogleAlbumId = $"GAlbum{i}";
                    photo.GoogleMediaItemId = $"MediaItem{i}";
                    repository.AddOrUpdate(photo);
                }
                
                photos = repository.GetPhotos();
                Assert.That(photos.Count, Is.EqualTo(1000));
                Assert.That(photos.All(p => p.GoogleAlbumId != null));
                Assert.That(photos.All(p => p.GoogleMediaItemId != null));

                var dbFile = Path.Combine(directory, "gpo.db");
                Assert.That(File.Exists(dbFile));
                Assert.That(File.ReadAllBytes(dbFile).Length, Is.GreaterThan(0));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}