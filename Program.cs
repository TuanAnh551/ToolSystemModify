using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SystemControlTool
{
    static class Program
    {
        private const string GITHUB_OWNER = "TuanAnh551";
        private const string GITHUB_REPO = "ToolSystemModify";
        private const string GITHUB_TOKEN = null;
        private const string EXPECTED_PASSWORD = "1304";
        private const string CLEANUP_ARG_PREFIX = "--cleanup=";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var launchedNew = CheckAndUpdateAsync().GetAwaiter().GetResult();
                if (launchedNew) return;
            }
            catch { }

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK ||
                    !string.Equals(login.EnteredPassword ?? string.Empty, EXPECTED_PASSWORD, StringComparison.Ordinal))
                    return;
            }

            Application.Run(new Form1());
        }

        private static async Task<bool> CheckAndUpdateAsync()
        {
            var apiUrl = $"https://api.github.com/repos/{GITHUB_OWNER}/{GITHUB_REPO}/releases/latest";

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ToolSystemModify-Updater");
                if (!string.IsNullOrEmpty(GITHUB_TOKEN))
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", GITHUB_TOKEN);

                string json;
                try { json = await client.GetStringAsync(apiUrl).ConfigureAwait(false); }
                catch { return false; }

                if (string.IsNullOrWhiteSpace(json)) return false;

                var tag = ExtractJsonString(json, "tag_name") ?? ExtractJsonString(json, "name");
                var downloadUrl = ExtractExeDownloadUrl(json);

                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(downloadUrl))
                    return false;

                if (!Version.TryParse(tag.TrimStart('v', 'V'), out Version remoteVersion))
                    return false;

                // Lấy version hiện tại
                Version currentVersion;
                try
                {
                    var fileVer = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion;
                    if (!Version.TryParse(fileVer, out currentVersion))
                        currentVersion = new Version(0, 0, 0, 0);
                }
                catch { currentVersion = new Version(0, 0, 0, 0); }

                if (remoteVersion <= currentVersion) return false;

                var answer = MessageBox.Show(
                    $"Có phiên bản mới ({remoteVersion}). Bạn đang dùng {currentVersion}.\n\nTải và cập nhật ngay?",
                    "Cập nhật mới", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes) return false;

                // Tải file exe
                byte[] data;
                try { data = await client.GetByteArrayAsync(downloadUrl).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tải thất bại: {ex.Message}", "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var currentExe = Assembly.GetEntryAssembly().Location;
                var currentDir = Path.GetDirectoryName(currentExe);

                // Lấy tên file gốc từ download URL
                var downloadFileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                var tempFile = Path.Combine(currentDir, downloadFileName);

                try { File.WriteAllBytes(tempFile, data); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không lưu được file: {ex.Message}", "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Tạo batch script: chờ app cũ tắt -> xóa file cũ -> chạy file mới
                var batPath = Path.Combine(Path.GetTempPath(), "tool_update.bat");
                var batContent = $@"@echo off
timeout /t 2 /nobreak >nul
del /f /q ""{currentExe}""
start """" ""{tempFile}""
del ""%~f0""
";
                try { File.WriteAllText(batPath, batContent, Encoding.Default); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không tạo được script: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{batPath}\"")
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không chạy được script cập nhật: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private static string ExtractJsonString(string json, string key)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string ExtractExeDownloadUrl(string json)
        {
            var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe[^\"]*)\"", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;

            m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
