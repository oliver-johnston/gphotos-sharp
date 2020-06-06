using System;

namespace GooglePhotosSharp.Database
{
    public class Photo
    {
        public string _id => Path;

        public string Path { get; set; }

        public string AlbumName { get; set; }
        
        public DateTime? UploadTime { get; set; }

        public string GoogleUploadId { get; set; }

        public string GoogleMediaItemId { get; set; }

        public string GoogleAlbumId { get; set; }
    }
}