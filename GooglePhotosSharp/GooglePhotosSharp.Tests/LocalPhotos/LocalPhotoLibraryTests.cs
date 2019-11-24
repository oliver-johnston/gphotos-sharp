using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using GooglePhotosSharp.Config;
using GooglePhotosSharp.LocalPhotos;
using NUnit.Framework;

namespace GooglePhotosSharp.Tests.LocalPhotos
{
    [TestFixture]
    public class LocalPhotoLibraryTests
    {
        [Test]
        public void TestGetAllPhotos()
        {
            var fileSystem = new MockFileSystem();

            fileSystem.Directory.CreateDirectory("/path/to/library");
            fileSystem.Directory.CreateDirectory("/path/to/library/album1");
            fileSystem.Directory.CreateDirectory("/path/to/library/album2");
            fileSystem.Directory.CreateDirectory("/path/to/library/album2/subfolder");

            fileSystem.File.Create("/path/to/library/album1/pic1.jpg");
            fileSystem.File.Create("/path/to/library/album2/subfolder/pic2.jpg");
            
            var config = new PhotoLibraryConfig
            {
                Path = "/path/to/library"
            };
            var library = new LocalPhotoLibrary(config, fileSystem);

            var photos = library.GetAllPhotos();
            
            Assert.That(photos.Count, Is.EqualTo(2));

            var pic1 = photos.FirstOrDefault(f => f.Path == "/path/to/library/album1/pic1.jpg");
            Assert.That(pic1, Is.Not.Null);
            Assert.That(pic1.AlbumName, Is.EqualTo("album1"));
            
            var pic2 = photos.FirstOrDefault(f => f.Path == "/path/to/library/album2/subfolder/pic2.jpg");
            Assert.That(pic2, Is.Not.Null);
            Assert.That(pic2.AlbumName, Is.EqualTo("album2"));
        }
    }
}