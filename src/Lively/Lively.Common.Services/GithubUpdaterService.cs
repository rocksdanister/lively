using Lively.Common.Helpers;
using Lively.Models.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Lively.Common.Services
{
    public sealed class GithubUpdaterService : IAppUpdaterService
    {
        //in milliseconds
        private readonly int fetchDelayError = 30 * 60 * 1000; //30min
        private readonly int fetchDelayRepeat = 12 * 60 * 60 * 1000; //12hr
        private readonly Timer retryTimer = new Timer();
        private static Architecture ProcessArch => RuntimeInformation.ProcessArchitecture;

        //public
        public AppUpdateStatus Status { get; private set; } = AppUpdateStatus.notchecked;
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public Version LastCheckVersion { get; private set; } = new Version(0, 0, 0, 0);
        public string LastCheckChangelog { get; private set; }
        public Uri LastCheckUri { get; private set; }
        public string LastCheckFileName { get; private set; }

        public event EventHandler<AppUpdaterEventArgs> UpdateChecked;

        public GithubUpdaterService()
        {
            retryTimer.Elapsed += RetryTimer_Elapsed;
            //giving the retry delay is not reliable since it will reset if system sleeps/suspends.
            retryTimer.Interval = 5 * 60 * 1000;
        }

        /// <summary>
        /// Check for updates periodically.
        /// </summary>
        public void Start()
        {
            retryTimer.Start();
        }

        /// <summary>
        /// Stops periodic updates check.
        /// </summary>
        public void Stop()
        {
            if (retryTimer.Enabled)
            {
                retryTimer.Stop();
            }
        }

        private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now - LastCheckTime).TotalMilliseconds > (Status != AppUpdateStatus.error ? fetchDelayRepeat : fetchDelayError))
            {
                _ = CheckUpdate(0);
            }
        }

        public async Task<AppUpdateStatus> CheckUpdate(int fetchDelay)
        {
            if (Constants.ApplicationType.IsMSIX)
            {
                //msix already has built-in updater.
                return AppUpdateStatus.notchecked;
            }

            try
            {
                await Task.Delay(fetchDelay);
                var (SetupUri, SetupFileName, SetupVersion) = await GetLatestRelease(Constants.ApplicationType.IsTestBuild);
                int verCompare = GithubUtil.CompareAssemblyVersion(SetupVersion);
                if (verCompare > 0)
                {
                    //update available.
                    Status = AppUpdateStatus.available;
                }
                else if (verCompare < 0)
                {
                    //beta release.
                    Status = AppUpdateStatus.invalid;
                }
                else
                {
                    //up-to-date.
                    Status = AppUpdateStatus.uptodate;
                }
                LastCheckUri = SetupUri;
                LastCheckVersion = SetupVersion;
                LastCheckFileName = SetupFileName;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Update fetch error:" + e.ToString());
                Status = AppUpdateStatus.error;
            }
            LastCheckTime = DateTime.Now;

            UpdateChecked?.Invoke(this, new AppUpdaterEventArgs(Status, LastCheckVersion, LastCheckTime, LastCheckUri, LastCheckFileName));
            return Status;
        }

        public static async Task<(Uri, string, Version)> GetLatestRelease(bool isBeta)
        {
            var userName = "rocksdanister";
            var repositoryName = isBeta ? "lively-beta" : "lively";
            var gitRelease = await GithubUtil.GetLatestRelease(userName, repositoryName);
            Version version = GithubUtil.GetVersion(gitRelease);

            // Get latest installer file
            var arch = GetArchSetupString(ProcessArch);
            // Format: lively_setup_ARCH_full_vXXXX.exe, XXXX - 4 digit version no and ARCH - x86, arm64
            var (fileName, url) = GithubUtil.GetAssetUrl(gitRelease, $"lively_setup_{arch}_full");
            if (fileName == null && ProcessArch == Architecture.X64)
            {
                // Fallback, old updater has hardcoded arch value so for backward compatibility initially x64 setup will be named x86.
                // In the future make an x86 installer that downloads x64 installer and installs.
                // Lively v2.1 onwards only x64 version is available.
                (fileName, url) = GithubUtil.GetAssetUrl(gitRelease, $"lively_setup_x86_full");
            }

            return (url is null ? null : new Uri(url), fileName, version);
        }

        private static string GetArchSetupString(Architecture arch)
        {
            return arch switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
