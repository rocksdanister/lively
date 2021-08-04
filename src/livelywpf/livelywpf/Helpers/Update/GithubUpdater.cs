using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace livelywpf.Helpers
{
    class GithubUpdater : IAppUpdater
    {
        private Uri gitUpdateUri;
        private Version gitLatestVersion;
        private string gitUpdatChangelog;
        private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public async Task<AppUpdateStatus> CheckUpdate()
        {
            AppUpdateStatus status = AppUpdateStatus.error;
            try
            {
                var userName = "rocksdanister";
                var repositoryName = Program.IsTestBuild ? "lively-beta" : "lively";

                var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);
                var gitVerCompare = GithubUtil.CompareAssemblyVersion(gitRelease);
                gitLatestVersion = GithubUtil.GetVersion(gitRelease);
                if (gitVerCompare > 0)
                {
                    try
                    {
                        status = AppUpdateStatus.available;
                        //download asset format: lively_setup_x86_full_vXXXX.exe, XXXX - 4 digit version no.
                        var gitUrl = await GithubUtil.GetAssetUrl("lively_setup_x86_full",
                            gitRelease, repositoryName, userName);

                        //changelog text
                        var sb = new StringBuilder(gitRelease.Body);
                        //formatting git text.
                        sb.Replace("#", "").Replace("\t", "  ");
                        gitUpdatChangelog = sb.ToString();
                        gitUpdateUri = new Uri(gitUrl);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error retriving asseturl for update: " + e.Message);
                        status = AppUpdateStatus.error;
                    }
                }
                else if (gitVerCompare < 0)
                {
                    //beta release.
                    status = AppUpdateStatus.invalid;
                }
                else
                {
                    //up-to-date
                    status = AppUpdateStatus.uptodate;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Update check fail:" + e.Message);
            }

            return status;
        }

        public string GetChangelog()
        {
            return gitUpdatChangelog;
        }

        public Uri GetUri()
        {
            return gitUpdateUri;
        }

        public Version GetVersion()
        {
            return gitLatestVersion;
        }
    }
}
