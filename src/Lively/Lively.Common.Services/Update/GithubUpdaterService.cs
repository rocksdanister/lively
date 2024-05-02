using Lively.Common.Helpers;
using Lively.Common.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Lively.Common.Services.Update
{
    public sealed class GithubUpdaterService : IAppUpdaterService
    {
        //in milliseconds
        private readonly int fetchDelayError = 30 * 60 * 1000; //30min
        private readonly int fetchDelayRepeat = 12 * 60 * 60 * 1000; //12hr
        private readonly Timer retryTimer = new Timer();

        //public
        public AppUpdateStatus Status { get; private set; } = AppUpdateStatus.notchecked;
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public Version LastCheckVersion { get; private set; } = new Version(0, 0, 0, 0);
        public string LastCheckChangelog { get; private set; }
        public Uri LastCheckUri { get; private set; }

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
                (Uri, Version) data = await GetLatestRelease(Constants.ApplicationType.IsTestBuild);
                int verCompare = GithubUtil.CompareAssemblyVersion(data.Item2);
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
                LastCheckUri = data.Item1;
                LastCheckVersion = data.Item2;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Update fetch error:" + e.ToString());
                Status = AppUpdateStatus.error;
            }
            LastCheckTime = DateTime.Now;

            UpdateChecked?.Invoke(this, new AppUpdaterEventArgs(Status, LastCheckVersion, LastCheckTime, LastCheckUri));
            return Status;
        }

        public async Task<(Uri, Version)> GetLatestRelease(bool isBeta)
        {
            var userName = "rocksdanister";
            var repositoryName = isBeta ? "lively-beta" : "lively";
            var gitRelease = await GithubUtil.GetLatestRelease(repositoryName, userName, 0);
            Version version = GithubUtil.GetVersion(gitRelease);

            // Download latest installer file
            // Format: lively_setup_ARCH_full_vXXXX.exe, XXXX - 4 digit version no and ARCH - x86, arm64
            var arch = GetArchSetupString();
            var gitUrl = await GithubUtil.GetAssetUrl($"lively_setup_{arch}_full",
                gitRelease, repositoryName, userName);
            var uri = new Uri(gitUrl);

            return (uri, version);
        }

        private static string GetArchSetupString()
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm => throw new NotImplementedException(),
                Architecture.Arm64 => "arm64",
                Architecture.Wasm => throw new NotImplementedException(),
                Architecture.S390x => throw new NotImplementedException(),
                Architecture.LoongArch64 => throw new NotImplementedException(),
                Architecture.Armv6 => throw new NotImplementedException(),
                Architecture.Ppc64le => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
