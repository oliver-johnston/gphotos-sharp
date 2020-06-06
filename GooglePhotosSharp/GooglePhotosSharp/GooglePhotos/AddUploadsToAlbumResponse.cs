using System.Collections.Generic;

namespace GooglePhotosSharp.GooglePhotos
{
    public class AddUploadsToAlbumResponse
    {
        public IList<NewMediaItem> NewMediaItemResults { get; set; }
    }

    public class NewMediaItem
    {
        public string UploadToken { get; set; }
        
        public Status Status { get; set; }
        
        public MediaItem MediaItem { get; set; }
    }

    public class MediaItem
    {
        public string Id { get; set; }
        public string ProductUrl { get; set; }
        public string Description { get; set; }
        public string BaseUrl { get; set; }
    }

    public class Status
    {
        public string Message { get; set; }
    }
}