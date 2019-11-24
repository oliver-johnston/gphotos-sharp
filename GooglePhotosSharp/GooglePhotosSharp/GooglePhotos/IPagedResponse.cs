using System.Collections.Generic;

namespace GooglePhotosSharp.GooglePhotos
{
    public interface IPagedResponse<T>
    {
        string NextPageToken { get; }
        IList<T> Items { get; }
    }
}