using Lively.Models.Taskbar;
using Lively.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Services.Taskbar
{
    internal sealed class TaskbarUtilityProcessClient : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        };

        private static readonly TimeSpan FastCallTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ApplyTimeout = TimeSpan.FromSeconds(50);
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2);
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly object processLock = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TaskbarIpcResponse>> pendingRequests = new();
        private Process process;
        private bool disposedValue;

        public TaskbarUtilityProcessClient(string utilityPath = null)
        {
            UtilityPath = utilityPath ?? ResolveUtilityPath();
        }

        public string UtilityPath { get; }
        public bool IsRunning => process != null && !process.HasExited;
        public bool Exists => File.Exists(UtilityPath);

        public bool IsAvailable()
        {
            var response = Send(new TaskbarIpcRequest { Type = TaskbarIpcCommand.IsAvailable }, FastCallTimeout, startIfMissing: true);
            return response?.Success == true;
        }

        public bool Apply(TaskbarIpcRequest request)
        {
            request.Type = TaskbarIpcCommand.Apply;
            var response = Send(request, ApplyTimeout, startIfMissing: true);
            if (response?.Success == true)
                return true;

            LogFailure(response, "Taskbar utility apply failed.");
            return false;
        }

        public bool Refresh()
        {
            if (!IsRunning)
                return false;

            var response = Send(new TaskbarIpcRequest { Type = TaskbarIpcCommand.Refresh }, ApplyTimeout, startIfMissing: false);
            if (response?.Success == true || response?.Status == TaskbarIpcStatus.Unavailable)
                return response?.Success == true;

            LogFailure(response, "Taskbar utility refresh failed.");
            return false;
        }

        public bool Restore()
        {
            if (!IsRunning)
                return true;

            var response = Send(new TaskbarIpcRequest { Type = TaskbarIpcCommand.Restore }, FastCallTimeout, startIfMissing: false);
            if (response?.Success == true)
                return true;

            LogFailure(response, "Taskbar utility restore failed.");
            return false;
        }

        public void Shutdown()
        {
            try
            {
                if (IsRunning)
                {
                    Restore();
                    Send(new TaskbarIpcRequest { Type = TaskbarIpcCommand.Shutdown }, ShutdownTimeout, startIfMissing: false);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to shut down taskbar utility gracefully.");
            }
            finally
            {
                KillProcessIfNeeded();
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                Shutdown();
                disposedValue = true;
            }
        }

        private TaskbarIpcResponse Send(TaskbarIpcRequest request, TimeSpan timeout, bool startIfMissing)
        {
            if (disposedValue || !EnsureProcess(startIfMissing))
                return null;

            request.RequestId ??= Guid.NewGuid().ToString("N");
            var completion = new TaskCompletionSource<TaskbarIpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!pendingRequests.TryAdd(request.RequestId, completion))
                return null;

            try
            {
                var json = JsonSerializer.Serialize(request, JsonOptions);
                lock (processLock)
                {
                    process?.StandardInput.WriteLine(json);
                    process?.StandardInput.Flush();
                }

                if (!completion.Task.Wait(timeout))
                {
                    Logger.Warn("Taskbar utility request timed out: {0}.", request.Type);
                    KillProcessIfNeeded();
                    return null;
                }

                return completion.Task.Result;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to communicate with taskbar utility.");
                return null;
            }
            finally
            {
                pendingRequests.TryRemove(request.RequestId, out _);
            }
        }

        private bool EnsureProcess(bool startIfMissing)
        {
            lock (processLock)
            {
                if (IsRunning)
                    return true;

                if (!startIfMissing)
                    return false;

                if (!Exists)
                {
                    Logger.Info("Taskbar utility not found: {0}", UtilityPath);
                    return false;
                }

                var workingDirectory = Path.GetDirectoryName(UtilityPath);
                process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = UtilityPath,
                        Arguments = "--parent-pid " + Process.GetCurrentProcess().Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        WorkingDirectory = workingDirectory,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardInputEncoding = Encoding.UTF8,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                    },
                };
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.Exited += Process_Exited;

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    Logger.Info("Started taskbar utility: {0}", UtilityPath);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to start taskbar utility.");
                    process.Dispose();
                    process = null;
                    return false;
                }
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            try
            {
                var response = JsonSerializer.Deserialize<TaskbarIpcResponse>(e.Data, JsonOptions);
                if (response?.RequestId != null && pendingRequests.TryRemove(response.RequestId, out var completion))
                {
                    completion.TrySetResult(response);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to parse taskbar utility response: {0}", e.Data);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Logger.Info("Taskbar utility: {0}", e.Data);
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            foreach (var item in pendingRequests)
            {
                item.Value.TrySetResult(new TaskbarIpcResponse
                {
                    RequestId = item.Key,
                    Success = false,
                    Status = TaskbarIpcStatus.Error,
                    Error = "Taskbar utility exited.",
                });
            }
        }

        private void KillProcessIfNeeded()
        {
            lock (processLock)
            {
                if (process == null)
                    return;

                try
                {
                    if (!process.HasExited)
                    {
                        if (!process.WaitForExit((int)ShutdownTimeout.TotalMilliseconds))
                            process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to terminate taskbar utility.");
                }
                finally
                {
                    process.Dispose();
                    process = null;
                }
            }
        }

        private static void LogFailure(TaskbarIpcResponse response, string message)
        {
            if (response == null)
            {
                Logger.Warn(message);
                return;
            }

            Logger.Warn("{0} Status: {1}, HRESULT: 0x{2:X8}, Error: {3}", message, response.Status, response.HResult, response.Error);
        }

        private static string ResolveUtilityPath()
        {
            return Path.Combine(AppContext.BaseDirectory, Constants.PlayerPartialPaths.TaskbarPath);
        }
    }
}
