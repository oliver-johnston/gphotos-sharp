using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;

namespace GooglePhotosSharp.GooglePhotos
{
    public class GooglePhotosApi : IGooglePhotosApi
    {
        private readonly string _user;
        private readonly string _clientSecretFile;
        private readonly IClock _clock;

        private UserCredential _credential;

        public GooglePhotosApi(string user, string clientSecretFile, IClock clock = null)
        {
            _user = user;
            _clientSecretFile = clientSecretFile;
            _clock = clock ?? SystemClock.Default;
        }

        public Task<IReadOnlyList<GooglePhotosAlbum>> GetAlbumsAsync()
        {
            return GetPagedItemsAsync<AlbumsResponse, GooglePhotosAlbum>(
                Method.GET,
                "v1/albums",
                (request, nextPageToken) =>
                {
                    request.AddParameter("pageSize", 50);
                    if (!string.IsNullOrEmpty(nextPageToken))
                    {
                        request.AddParameter(new Parameter("pageToken", nextPageToken,
                            ParameterType.QueryStringWithoutEncode));
                    }
                });
        }

        public async Task<GooglePhotosAlbum> CreateAlbumAsync(string title)
        {
            var client = await GetClient();

            Log.Information($"Creating new album: {title}");

            var request = new RestRequest("v1/albums", Method.POST, DataFormat.Json)
                .AddJsonBody(new
                {
                    album = new
                    {
                        title
                    }
                });

            var response = await client.ExecutePostTaskAsync<GooglePhotosAlbum>(request);

            ThrowOnError(response);

            return response.Data;
        }

        public async Task AddItemsToAlbum(string albumId, IList<string> mediaItemIds)
        {
            var client = await GetClient();

            var request = new RestRequest($"v1/albums/{albumId}:batchAddMediaItems", Method.POST)
                .AddJsonBody(new
                {
                    mediaItemIds
                });

            var response = await client.ExecutePostTaskAsync(request);
            ThrowOnError(response);
        }

        public async Task<UploadedPhoto> UploadPhotoAsync(string filename, byte[] bytes)
        {
            var client = await GetClient();

            var request = new RestRequest("v1/uploads", Method.POST)
                .AddHeader("X-Goog-Upload-File-Name", filename)
                .AddHeader("X-Goog-Upload-Protocol", "raw")
                .AddParameter("body", bytes, "application/octet-stream", ParameterType.RequestBody);

            var response = await client.ExecutePostTaskAsync(request);
            ThrowOnError(response);
            return new UploadedPhoto
            {
                UploadToken = response.Content,
                FileName = filename
            };
        }

        public async Task<IList<NewMediaItem>> AddUploadsToAlbumAsync(string albumId, IList<UploadedPhoto> photos)
        {
            var client = await GetClient();

            var request = new RestRequest("v1/mediaItems:batchCreate", Method.POST)
                .AddJsonBody(new
                {
                    albumId = albumId,
                    newMediaItems = photos.Select(p => new
                    {
                        description = p.FileName,
                        simpleMediaItem = new
                        {
                            uploadToken = p.UploadToken
                        }
                    })
                });

            var response = await client.ExecutePostTaskAsync<AddUploadsToAlbumResponse>(request);
            ThrowOnError(response);
            return response.Data.NewMediaItemResults;
        }

        private async Task<IReadOnlyList<TItem>> GetPagedItemsAsync<TResponse, TItem>(
            Method method,
            string resource,
            Action<RestRequest, string> addParameters)
            where TResponse : IPagedResponse<TItem>, new()
        {
            var client = await GetClient();

            string nextPageToken = null;

            Log.Information($"Requesting {resource}");

            int pageCount = 0;
            var results = new List<TItem>();
            do
            {
                var request = new RestRequest(resource, method);

                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    Log.Information($"Retrieved {results.Count:n0} {resource}. Requesting batch {++pageCount}.");
                }

                addParameters(request, nextPageToken);

                var response = await client.ExecuteTaskAsync<TResponse>(request);

                ThrowOnError(response);

                if (response.Data.Items != null)
                {
                    results.AddRange(response.Data.Items);
                }

                if (nextPageToken == response.Data.NextPageToken)
                    nextPageToken = null;
                else
                    nextPageToken = response.Data.NextPageToken;
            } while (!string.IsNullOrEmpty(nextPageToken));

            Log.Information($"Retrieved {results.Count:n0} {resource}");

            return results;
        }

        private async Task<IRestClient> GetClient()
        {
            if (_credential == null)
            {
                using (var stream = new FileStream(_clientSecretFile, FileMode.Open, FileAccess.Read))
                {
                    _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        new[] {"https://www.googleapis.com/auth/photoslibrary"},
                        _user, CancellationToken.None, new FileDataStore("."));
                }
            }

            if (_credential.Token.IsExpired(_clock))
            {
                await _credential.RefreshTokenAsync(CancellationToken.None);
            }

            var client = new RestClient("https://photoslibrary.googleapis.com/");
            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(_credential.Token.AccessToken);

            return client;
        }

        private void ThrowOnError(IRestResponse response)
        {
            if (!response.IsSuccessful)
            {
                throw new ApplicationException(response.Content);
            }
        }
    }

    public interface IGooglePhotosApi
    {
        Task<IReadOnlyList<GooglePhotosAlbum>> GetAlbumsAsync();
        Task<GooglePhotosAlbum> CreateAlbumAsync(string title);
        Task AddItemsToAlbum(string albumId, IList<string> mediaItemIds);
        Task<UploadedPhoto> UploadPhotoAsync(string filename, byte[] bytes);
        Task<IList<NewMediaItem>> AddUploadsToAlbumAsync(string albumId, IList<UploadedPhoto> photos);
    }
}