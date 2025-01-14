using Octokit;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lively.Common.Helpers
{
    public static class GithubUtil
    {
        /// <summary>
        /// Get latest release from github after given delay.
        /// </summary>
        public static async Task<Release> GetLatestRelease(string userName, string repositoryName)
        {
            var client = new GitHubClient(new ProductHeaderValue(repositoryName));
            var releases = await client.Repository.Release.GetAll(userName, repositoryName);
            var latest = releases[0];

            return latest;
        }

        public static (string fileName, string url) GetAssetUrl(Release release, string assetName)
        {
            var asset = release.Assets.FirstOrDefault(x => Contains(x.Name, assetName, StringComparison.OrdinalIgnoreCase));
            return (asset?.Name, asset?.BrowserDownloadUrl);
        }

        public static Version GetVersion(Release release)
        {
            return new Version(Regex.Replace(release.TagName, "[A-Za-z ]", ""));
        }

        public static int CompareAssemblyVersion(Version version)
        {
            var appVersion = new Version(System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
            return version.CompareTo(appVersion);
        }

        /// <summary>
        /// String Contains method with StringComparison property.
        /// </summary>
        private static bool Contains(String str, String substring,
                                    StringComparison comp)
        {
            if (substring == null)
                throw new ArgumentNullException("substring",
                                             "substring cannot be null.");
            else if (!Enum.IsDefined(typeof(StringComparison), comp))
                throw new ArgumentException("comp is not a member of StringComparison",
                                         "comp");

            return str.IndexOf(substring, comp) >= 0;
        }
    }
}
