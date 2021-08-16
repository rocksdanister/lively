using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.Helpers
{
    class GithubUpdater : IAppUpdater
    {
        public async Task<(Uri, Version, string)> GetLatestRelease(bool isBeta)
        {
            var userName = "rocksdanister";
            var repositoryName = isBeta ? "lively-beta" : "lively";
            var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);
            Version version = GithubUtil.GetVersion(gitRelease);

            //download asset format: lively_setup_x86_full_vXXXX.exe, XXXX - 4 digit version no.
            var gitUrl = await GithubUtil.GetAssetUrl("lively_setup_x86_full",
                gitRelease, repositoryName, userName);
            Uri uri = new Uri(gitUrl);

            //changelog text and formatting
            var sb = new StringBuilder(gitRelease.Body);
            sb.Replace("#", "").Replace("\t", "  ");
            string changelog = sb.ToString();

            return (uri, version, changelog);
        }
    }
}
