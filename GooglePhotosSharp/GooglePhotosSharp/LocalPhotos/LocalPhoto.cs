using System.IO.Abstractions;

namespace GooglePhotosSharp.LocalPhotos
{
    public class LocalPhoto
    {
        private readonly IFileSystem _fileSystem;

        public LocalPhoto(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public string Path { get; set; }

        public string AlbumName { get; set; }

        public byte[] ReadAllBytes()
        {
            return _fileSystem.File.ReadAllBytes(Path);
        }
    }
}