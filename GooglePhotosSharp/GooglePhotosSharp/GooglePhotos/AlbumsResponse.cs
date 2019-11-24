using System.Collections.Generic;

namespace GooglePhotosSharp.GooglePhotos
{
    public class AlbumsResponse : IPagedResponse<GooglePhotosAlbum>
    {
        public IList<GooglePhotosAlbum> Albums { get; set; }
        public string NextPageToken { get; set; }
        
        IList<GooglePhotosAlbum> IPagedResponse<GooglePhotosAlbum>.Items => Albums;
    }
}