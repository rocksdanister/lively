namespace Lively.Models.Gallery.API
{
    public static class ApiErrors
    {
        public static readonly string NotFound = "NOT_FOUND";
        public static readonly string CodeCannotBeNullOrEmpty = "CODE_CANNOT_BE_NULL_OR_EMPTY";
        public static readonly string InvalidAccessToken = "INVALID_ACCESS_TOKEN";
        public static readonly string RefreshTokensDoesntMatch = "REFRESH_TOKENS_DOESNT_MATCH";
        public static readonly string InvalidSortingType = "INVALID_SORTING_TYPE";
        public static readonly string InvalidPageIndex = "INVALID_PAGE_INDEX";
        public static readonly string InvalidContentType = "INVALID_CONTENT_TYPE";
        public static readonly string MalformedArchive = "MALFORMED_WALLPAPER_ARCHIVE";
        public static readonly string AlreadySubscribedToWallpaper = "ALREADY_SUBSCRIBED_TO_WALLPAPER";
        public static readonly string NotSubscribedToWallpaper = "NOT_SUBSCRIBED_TO_WALLPAPER";
        public static readonly string TokensExpired = "TOKENS_EXPIRED";
    }
}
