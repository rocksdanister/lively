using Lively.Common;
using Lively.Gallery.Client.Interfaces;
using Lively.Models.Gallery;
using Lively.Models.Gallery.API;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace Lively.Gallery.Client
{
    public class GalleryClient
    {
        public event EventHandler<string> WallpaperUnsubscribed;
        public event EventHandler<string> WallpaperSubscribed;

        private HttpClient _client;
        private ITokenStore _tokenStore;
        private string _oneTimeAuthCode;
        private ManualResetEventSlim _slim;
        private string _authLink;
        private readonly string _githubAuthLink;

        public GalleryClient(IHttpClientFactory httpClientFactory, string host, string authLink, string githubAuthLink, ITokenStore tokenStore)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri(host);
            _tokenStore = tokenStore;
            _slim = new ManualResetEventSlim();
            _authLink = authLink;
            _githubAuthLink = githubAuthLink;
            Host = host;
        }

        public string Host { get; }
        public ProfileDto CurrentUser { get; set; }
        public TokensModel Tokens => _tokenStore.Get();
        public bool IsLoggedIn { get => CurrentUser != null && _tokenStore.Get().Expiration > DateTime.UtcNow; }
        public event EventHandler<object> LoggedIn;
        public event EventHandler<object> LoggedOut;

        private WatsonWebserver.Server _server;

        public async Task InitializeAsync()
        {
            var result = await GetMeAsync();
            CurrentUser = result;
            if (CurrentUser != null)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
        }
        #region Users
        public async Task<ProfileDto> GetMeAsync()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "users/@me");
            var result = await SendAsync<ProfileDto>(message);
            return result.Data;
        }

        public async Task<string?> DeleteAccountAsync()
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, "users/@me");
            var result = await SendAsync<object?>(message);
            var error = result.Errors?.FirstOrDefault();
            if (error == null)
            {
                CurrentUser = null;
                LoggedOut?.Invoke(this, EventArgs.Empty);
            }
            return error;
        }
        #endregion  
        #region Authentication
        public async Task<string> RequestCodeAsync(string provider)
        {
            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            _server = new WatsonWebserver.Server("127.0.0.1", 43821, false);
            switch (provider.ToUpperInvariant())
            {
                case "GITHUB":
                    _server.Routes.Static.Add(WatsonWebserver.HttpMethod.GET, "signin-oidc-github", GithubCallback);
                    break;
                case "GOOGLE":
                    _server.Routes.Static.Add(WatsonWebserver.HttpMethod.GET, "signin-oidc", GoogleCallback);
                    break;
            }
            _server.Start();

            LinkUtil.OpenBrowser(
                provider switch
                {
                    "GOOGLE" => _authLink,
                    "GITHUB" => _githubAuthLink,
                    _ => throw new InvalidOperationException(),
                });
            _slim.Wait();
            await Task.Delay(200);
            try
            {
                _server.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ehh... {ex}");
            }
            _slim.Reset();
            _server = null;
            return _oneTimeAuthCode;
        }

        private async Task GithubCallback(WatsonWebserver.HttpContext ctx)
        {
            var code = ctx.Request.Query.Elements["code"];
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send("Authenticated. You can close this window now");
            _oneTimeAuthCode = code;
            _slim.Set();
        }
        public async Task<TokensModel> AuthenticateGoogleAsync(string googleCode)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"auth/google-token?code={googleCode}&provider=GOOGLE");
            var result = await SendAsync<TokensModel>(message, false);

            var tokens = result.Data;
            _tokenStore.Set(tokens.AccessToken, tokens.RefreshToken, "GOOGLE", tokens.Expiration);
            CurrentUser = await GetMeAsync();
            if (CurrentUser != null)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
            return result.Data;
        }
        public async Task<TokensModel> AuthenticateGithubAsync(string githubCode)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"auth/google-token?code={githubCode}&provider=GITHUB");
            var result = await SendAsync<TokensModel>(message, false);

            var tokens = result.Data;
            _tokenStore.Set(tokens.AccessToken, tokens.RefreshToken, "GITHUB", tokens.Expiration);
            CurrentUser = await GetMeAsync();
            if(CurrentUser != null)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
            return result.Data;
        }

        private async Task<TokensModel> RefreshTokensAsync()
        {
            if (Tokens?.AccessToken == null || Tokens?.RefreshToken == null)
                throw new UnauthorizedAccessException("Couldn't refresh tokens. You have to log in again");
            var message = new HttpRequestMessage(HttpMethod.Post, "auth/refresh")
                .WithJsonContent(Tokens);
            var result = await SendAsync<TokensModel>(message, false, true);
            return result.Data;
        }

        public async Task<bool> LogoutAsync()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "auth/logout");
            var result = await SendAsync<object?>(message);
            CurrentUser = null;
            _tokenStore.Set(null, null, null, DateTime.MinValue);
            LoggedOut?.Invoke(this, EventArgs.Empty);
            return result.Success;
        }
        #endregion     
        #region Gallery
        public async Task DownloadWallpaperAsync(string id, string fileName, CancellationToken ct, Action<float, float, float> progressCallback = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"gallery/{id}/download");
            await DownloadFile(message, fileName, ct, true, progressCallback);
        }

        public async Task<WallpaperDto> UploadWallpaperAsync(FileStream stream)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, "gallery");
            var form = new MultipartFormDataContent();
            var file = new StreamContent(stream);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            form.Add(file, "zip", "lively.zip");
            message.Content = form;
            var result = await SendAsync<WallpaperDto>(message, true);
            return result.Data;
        }

        public async Task<WallpaperDto> GetWallpaperInfoAsync(string id)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"gallery/{id}");
            var response = await SendAsync<WallpaperDto>(message);
            response.Data.Preview = response.Data.IsPreviewAvailable ? $"{Host}gallery/{response.Data.Id}/preview" : null;
            response.Data.Thumbnail = $"{Host}gallery/{response.Data.Id}/thumbnail";
            return response.Data;
        }

        public async Task<Page<WallpaperDto>> SearchWallpapers(SearchQuery searchQuery)
        {
            var uri = "gallery/search?";
            uri += $"sortBy={searchQuery.SortingType}";
            uri += $"&page={searchQuery.Page}";
            uri += $"&perPage={searchQuery.Limit}";
            if (searchQuery.Name != null)
                uri += $"&query={searchQuery.Name}";
            if (searchQuery.Tags != null)
                uri += $"&tags={string.Join(",", searchQuery.Tags)}";
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await SendAsync<Page<WallpaperDto>>(message);
            foreach (var item in response.Data.Data)
            {
                item.Preview = item.IsPreviewAvailable ? $"{Host}gallery/{item.Id}/preview" : null;
                item.Thumbnail = $"{Host}gallery/{item.Id}/thumbnail";
            }
            return response.Data;
        }

        #endregion 
        #region Wallpaper Subscriptions
        public async Task<List<WallpaperDto>> GetWallpaperSubscriptions()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"users/@me/wallpapers");
            var result = await SendAsync<List<WallpaperDto>>(message);
            foreach (var item in result.Data)
            {
                item.Preview = item.IsPreviewAvailable ? $"{Host}gallery/{item.Id}/preview" : null;
                item.Thumbnail = $"{Host}gallery/{item.Id}/thumbnail";
            }
            return result.Data;
        }

        //TODO: Make it return result instead of throwing? or remove bool return?
        public async Task<bool> SubscribeToWallpaperAsync(string id)
        {
            try
            {
                var message = new HttpRequestMessage(HttpMethod.Put, $"users/@me/wallpapers/{id}");
                var result = await SendAsync<object?>(message);
                WallpaperSubscribed?.Invoke(this, id);
                return result.Success;
            }
            catch (ApiException e)
            {
                if (e.Errors.Contains(ApiErrors.AlreadySubscribedToWallpaper))
                {
                    WallpaperSubscribed?.Invoke(this, id);
                }
                throw;
            }
        }

        public async Task<bool> UnsubscribeFromWallpaperAsync(string id)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, $"users/@me/wallpapers/{id}");
            var result = await SendAsync<object?>(message);
            WallpaperUnsubscribed?.Invoke(this, id);
            return result.Success;
        }
        #endregion
        #region Other
        public async Task<HealthResult> GetBackendHealthAsync()
        {
            var message = new HttpRequestMessage(HttpMethod.Get, "health");
            var result = await _client.SendAsync(message);
            if (!result.IsSuccessStatusCode)
                return null;
            var content = await result.Content.ReadAsStringAsync();
            var healthResult = JsonConvert.DeserializeObject<HealthResult>(content);
            return healthResult!;
        }
        #endregion
        #region Internals
        private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage message, bool requireAuth = true, bool isRetry = false)
        {
            var httpResp = await InternalSendAsync(message, isRetry, requireAuth, HttpCompletionOption.ResponseContentRead);
            if (httpResp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException(ApiErrors.TokensExpired);
            var content = await httpResp.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
            if (response == null)
                response = new();
            if (response.Errors != null)
            {
                throw new ApiException(response.Errors);
            }
            response.StatusCode = (int)httpResp.StatusCode;
            return response;
        }

        private async Task<HttpResponseMessage> InternalSendAsync(HttpRequestMessage message, bool isRetry, bool requireAuth = true, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            if (requireAuth && Tokens?.AccessToken == null)
                throw new UnauthorizedAccessException("Token not found.");

            if (requireAuth && Tokens?.AccessToken != null)
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Tokens.AccessToken);
            //The request message was already sent. Cannot send the same request message multiple times.
            var clone = message.Clone();
            var httpResp = await _client.SendAsync(message, option);
            var requireRefreshingTokens = !isRetry && httpResp.StatusCode == HttpStatusCode.Unauthorized;
            if (requireRefreshingTokens)
            {
                var result = await RefreshTokensAsync();

                var tokens = result;
                _tokenStore.Set(tokens.AccessToken, tokens.RefreshToken, _tokenStore.Get().Provider, tokens.Expiration);
                return await InternalSendAsync(clone, true, requireAuth, option);

            }
            clone.Dispose();
            return httpResp;
        }

        private async Task<List<string>> DownloadFile(HttpRequestMessage message, string fileName, CancellationToken ct, bool requireAuth = true, Action<float, float, float> progressCallback = null)
        {
            var httpResp = await InternalSendAsync(message, false, requireAuth, HttpCompletionOption.ResponseHeadersRead);
            if (httpResp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Couldn't refresh tokens. You have to log in again");
            if (httpResp.IsSuccessStatusCode)
            {
                using (Stream contentStream = await httpResp.Content.ReadAsStreamAsync(), fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var totalRead = 0L;
                    var totalReads = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;
                    var length = httpResp.Content.Headers.ContentLength;
                    var succ = length != 0;
                    //Some wallpapers are downloaded as single chunk, this is to show starting animation.
                    progressCallback.Invoke(0, 0, (float)length);
                    do
                    {
                        ct.ThrowIfCancellationRequested();

                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            totalReads += 1;
                        }
                        if (succ && progressCallback != null && totalRead % 30 == 0)
                            progressCallback.Invoke((int)(totalRead / (float)length * 100f), totalRead, (float)length);
#if DEBUG
                        await Task.Delay(5);
#endif
                    } while (isMoreToRead);
                    if (succ && progressCallback != null)
                        progressCallback.Invoke(100, (float)length, (float)length);
                    return null;
                }
            }
            else
            {
                return new[] { httpResp.StatusCode.ToString() }.ToList();
            }
        }

        //private void OpenUrl(string url)
        //{
        //    try
        //    {
        //        Process.Start(url);
        //    }
        //    catch
        //    {
        //        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        //        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //        {
        //            url = url.Replace("&", "^&");
        //            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        //        }
        //        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //        {
        //            Process.Start("xdg-open", url);
        //        }
        //        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //        {
        //            Process.Start("open", url);
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //}

        private async Task GoogleCallback(WatsonWebserver.HttpContext ctx)
        {
            var code = ctx.Request.Query.Elements["code"];
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send("Authenticated. You can close this window now");
            _oneTimeAuthCode = code;
            _slim.Set();
        }
        #endregion
    }
}
