using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SystemControlTool
{
    public partial class Form1 : Form
    {
        ManagementEventWatcher registryWatcher;

        [DllImport("user32.dll")]
        public static extern int SendMessageTimeout(
    IntPtr hWnd, int Msg, IntPtr wParam, string lParam,
    SendMessageTimeoutFlags flags, int timeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_ABORTIFHUNG = 0x2
        }

        private readonly Dictionary<string, Label> statusLabels = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Button> enableButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Button> disableButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);

        public Form1()
        {
            InitializeComponent();
            LoadStatus();
            StartRegistryWatcher();
            InitUI();
        }

        void InitUI()
        {
            this.Text = "System Control Tool";
            this.Size = new Size(650, 580);
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            // ===== HEADER =====
            Panel header = new Panel()
            {
                BackColor = Color.FromArgb(33, 150, 243),
                Dock = DockStyle.Top,
                Height = 60
            };

            Label lblTitle = new Label()
            {
                Text = "System Control Tool",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            header.Controls.Add(lblTitle);
            this.Controls.Add(header);

            // ===== FOOTER =====
            Panel footer = new Panel()
            {
                BackColor = Color.FromArgb(240, 240, 240),
                Dock = DockStyle.Bottom,
                Height = 30
            };

            Label lblFooter = new Label()
            {
                Text = "© 2026 - System Control Tool by Anh Tuấn",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(10, 7)
            };

            footer.Controls.Add(lblFooter);
            this.Controls.Add(footer);

            // ===== CONTENT =====
            int y = 80;

            CreateRow("Registry", "Registry", ref y, btnDisableReg_Click, btnEnableReg_Click);
            CreateRow("TaskManager", "Task Manager", ref y, btnDisableTask_Click, btnEnableTask_Click);
            CreateRow("CMD", "CMD", ref y, btnDisableCMD_Click, btnEnableCMD_Click);
            CreateRow("Wallpaper", "Wallpaper Lock", ref y, btnLockWallpaper_Click, btnUnlockWallpaper_Click);
            CreateRow("Programs", "Programs & Features", ref y, btnHidePrograms_Click, btnShowPrograms_Click);
            CreateRow("StartMenu", "Start Menu Uninstall", ref y, btnDisableStartUninstall_Click, btnEnableStartUninstall_Click);
            CreateRow("Run", "Run (Win + R)", ref y, btnDisableRun_Click, btnEnableRun_Click);
            CreateRow("System", "Control Panel + Settings", ref y, btnDisableSystem_Click, btnEnableSystem_Click);
            CreateRow("Display", "Display Settings", ref y, btnDisableDisplay_Click, btnEnableDisplay_Click);

            LoadStatus();
        }

        void CreateRow(string key, string labelText, ref int y, EventHandler disableEvent, EventHandler enableEvent)
        {
            Label lblName = new Label()
            {
                Text = labelText + ":",
                Location = new Point(20, y + 5),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            Label lblStatus = new Label()
            {
                Text = "Enabled",
                Location = new Point(220, y + 5),
                Size = new Size(100, 25),
                ForeColor = Color.Green,
                Name = "lblStatus_" + key
            };

            Button btnDisable = new Button()
            {
                Text = "Disable",
                Location = new Point(330, y),
                Size = new Size(100, 30),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Name = "btnDisable_" + key
            };

            Button btnEnable = new Button()
            {
                Text = "Enable",
                Location = new Point(440, y),
                Size = new Size(100, 30),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Name = "btnEnable_" + key
            };

            btnDisable.FlatAppearance.BorderColor = Color.Black;
            btnDisable.FlatAppearance.BorderSize = 1;
            btnEnable.FlatAppearance.BorderColor = Color.Black;
            btnEnable.FlatAppearance.BorderSize = 1;

            btnDisable.MouseEnter += (s, e) =>
            {
                btnDisable.BackColor = Color.FromArgb(220, 53, 69);
                btnDisable.ForeColor = Color.White;
                btnDisable.FlatAppearance.BorderSize = 0;
            };
            btnDisable.MouseLeave += (s, e) =>
            {
                btnDisable.BackColor = Color.White;
                btnDisable.ForeColor = Color.Black;
                btnDisable.FlatAppearance.BorderColor = Color.Black;
                btnDisable.FlatAppearance.BorderSize = 1;
            };

            btnEnable.MouseEnter += (s, e) =>
            {
                btnEnable.BackColor = Color.FromArgb(40, 167, 69);
                btnEnable.ForeColor = Color.White;
                btnEnable.FlatAppearance.BorderSize = 0;
            };
            btnEnable.MouseLeave += (s, e) =>
            {
                btnEnable.BackColor = Color.White;
                btnEnable.ForeColor = Color.Black;
                btnEnable.FlatAppearance.BorderColor = Color.Black;
                btnEnable.FlatAppearance.BorderSize = 1;
            };

            btnDisable.Click += disableEvent;
            btnEnable.Click += enableEvent;

            statusLabels[key] = lblStatus;
            enableButtons[key] = btnEnable;
            disableButtons[key] = btnDisable;

            this.Controls.Add(lblName);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnDisable);
            this.Controls.Add(btnEnable);

            y += 45;
        }

        void RefreshPolicy()
        {
            IntPtr result;
            SendMessageTimeout(
                new IntPtr(0xffff),
                0x1A,
                IntPtr.Zero,
                "Policy",
                SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
                100,
                out result);
        }

        void StartRegistryWatcher()
        {
            try
            {
                WqlEventQuery query = new WqlEventQuery(
                    "SELECT * FROM RegistryValueChangeEvent " +
                    "WHERE Hive='HKEY_CURRENT_USER' " +
                    "AND KeyPath='Software\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Policies\\\\System'"
                );

                registryWatcher = new ManagementEventWatcher(query);
                registryWatcher.EventArrived += (s, e) =>
                {
                    this.Invoke(new Action(() => LoadStatus()));
                };
                registryWatcher.Start();
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (registryWatcher != null)
                registryWatcher.Stop();
            base.OnFormClosing(e);
        }

        void Notify(string msg)
        {
            MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void SetPolicy(string path, string key, int value)
        {
            RegistryKey reg = Registry.CurrentUser.CreateSubKey(path);
            reg.SetValue(key, value, RegistryValueKind.DWord);
            reg.Close();
        }

        int GetPolicy(string path, string key)
        {
            RegistryKey reg = Registry.CurrentUser.OpenSubKey(path);
            if (reg == null) return 0;
            object val = reg.GetValue(key);
            if (val == null) return 0;
            return (int)val;
        }
        int GetPolicyLM(string path, string key)
        {
            RegistryKey reg = Registry.LocalMachine.OpenSubKey(path);
            if (reg == null) return 0;
            object val = reg.GetValue(key);
            if (val == null) return 0;
            return (int)val;
        }

        bool GetPolicyAny(string path, string key)
        {
            return GetPolicy(path, key) == 1 || GetPolicyLM(path, key) == 1;
        }
        void LoadStatus()
        {
            bool regDisabled = IsRegistryDisabled();
            bool taskDisabled = GetPolicyAny(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr");
            bool cmdDisabled = IsCMDDisabled();
            bool wallpaperLocked = GetPolicyAny(@"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop", "NoChangingWallPaper");
            bool programsDisabled = IsProgramsAndFeaturesDisabled();
            bool startMenuDisabled = GetPolicyAny(@"Software\Policies\Microsoft\Windows\Explorer", "NoUninstallFromStart");
            bool runDisabled = IsWinRDisabled();
            bool sysDisabled = IsSystemSettingsDisabled();
            bool displayDisabled = IsDisplaySettingsDisabled();

            SetStatusLabel("Registry", regDisabled ? "Disabled" : "Enabled", regDisabled ? Color.Red : Color.Green);
            SetStatusLabel("TaskManager", taskDisabled ? "Disabled" : "Enabled", taskDisabled ? Color.Red : Color.Green);
            SetStatusLabel("CMD", cmdDisabled ? "Disabled" : "Enabled", cmdDisabled ? Color.Red : Color.Green);
            SetStatusLabel("Wallpaper", wallpaperLocked ? "Locked" : "Unlocked", wallpaperLocked ? Color.Red : Color.Green);
            SetStatusLabel("Programs", programsDisabled ? "Hidden" : "Visible", programsDisabled ? Color.Red : Color.Green);
            SetStatusLabel("StartMenu", startMenuDisabled ? "Disabled" : "Enabled", startMenuDisabled ? Color.Red : Color.Green);
            SetStatusLabel("Run", runDisabled ? "Disabled" : "Enabled", runDisabled ? Color.Red : Color.Green);
            SetStatusLabel("System", sysDisabled ? "Disabled" : "Enabled", sysDisabled ? Color.Red : Color.Green);
            SetStatusLabel("Display", displayDisabled ? "Disabled" : "Enabled", displayDisabled ? Color.Red : Color.Green);

            SetButtonEnabled("TaskManager", !regDisabled);
            SetButtonEnabled("CMD", !regDisabled);
            SetButtonEnabled("Wallpaper", !regDisabled);
            SetButtonEnabled("Programs", !regDisabled);
            SetButtonEnabled("StartMenu", !regDisabled);
            SetButtonEnabled("Run", !regDisabled);
            SetButtonEnabled("System", !regDisabled);
            SetButtonEnabled("Display", !regDisabled);
        }

        private void SetStatusLabel(string key, string text, Color color)
        {
            if (statusLabels.TryGetValue(key, out var lbl))
            {
                lbl.Text = text;
                lbl.ForeColor = color;
            }
        }

        private void SetButtonEnabled(string key, bool enabled)
        {
            if (enableButtons.TryGetValue(key, out var btnE))  btnE.Enabled = enabled;
            if (disableButtons.TryGetValue(key, out var btnD)) btnD.Enabled = enabled;
        }

        // ── Registry ────────────────────────────────────────────────
        private void btnEnableReg_Click(object sender, EventArgs e)
        {
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", 0);
            Notify("Registry has been enabled");
            LoadStatus();
        }

        private void btnDisableReg_Click(object sender, EventArgs e)
        {
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", 1);
            Notify("Registry has been disabled");
            LoadStatus();
        }

        bool IsRegistryDisabled()
        {
            // Path thông thường HKCU
            if (GetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools") == 1) return true;
            // Path thông thường HKLM
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools") == 1) return true;
            // Path Group Policy HKCU (gpedit ghi vào đây)
            if (GetPolicy(@"Software\Policies\Microsoft\Windows\System", "DisableRegistryTools") == 1) return true;
            // Path Group Policy HKLM
            if (GetPolicyLM(@"Software\Policies\Microsoft\Windows\System", "DisableRegistryTools") == 1) return true;
            return false;
        }

        // ── Task Manager ─────────────────────────────────────────────
        private void btnEnableTask_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", 0);
            Notify("Task Manager has been enabled");
            LoadStatus();
        }

        private void btnDisableTask_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", 1);
            Notify("Task Manager has been disabled");
            LoadStatus();
        }

        // ── CMD ──────────────────────────────────────────────────────
        private void btnEnableCMD_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            SetPolicy(@"Software\Policies\Microsoft\Windows\System", "DisableCMD", 0);
            Notify("CMD has been enabled");
            LoadStatus();
        }

        private void btnDisableCMD_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            SetPolicy(@"Software\Policies\Microsoft\Windows\System", "DisableCMD", 1);
            Notify("CMD has been disabled");
            LoadStatus();
        }

        bool IsCMDDisabled()
        {
            // Path thông thường HKCU
            if (GetPolicy(@"Software\Policies\Microsoft\Windows\System", "DisableCMD") == 1) return true;
            // Path thông thường HKLM
            if (GetPolicyLM(@"Software\Policies\Microsoft\Windows\System", "DisableCMD") == 1) return true;
            // Path Group Policy HKCU (gpedit ghi vào đây)
            if (GetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCMD") == 1) return true;
            // Path Group Policy HKLM
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCMD") == 1) return true;
            return false;
        }

        // ── Wallpaper ────────────────────────────────────────────────
        private void btnLockWallpaper_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa."); return; }

            // HKCU
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop", "NoChangingWallPaper", 1);
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSetWallpaper", 1);
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispBackgroundPage", 1);

            // HKLM - cần cho Win 10 LTSB
            SetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop", "NoChangingWallPaper", 1);
            SetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispBackgroundPage", 1);

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            RefreshPolicy();

            Notify("Change wallpaper has been locked");
            LoadStatus();
        }

        private void btnUnlockWallpaper_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa."); return; }

            // HKCU
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop", "NoChangingWallPaper", 0);
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSetWallpaper", 0);
            SetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispBackgroundPage", 0);

            // HKLM - cần cho Win 10 LTSB
            SetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop", "NoChangingWallPaper", 0);
            SetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispBackgroundPage", 0);

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, null, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            RefreshPolicy();

            Notify("Change wallpaper has been unlocked");
            LoadStatus();
        }

        // Thêm hàm set HKLM
        void SetPolicyLM(string path, string key, int value)
        {
            try
            {
                RegistryKey reg = Registry.LocalMachine.CreateSubKey(path);
                if (reg != null)
                {
                    reg.SetValue(key, value, RegistryValueKind.DWord);
                    reg.Close();
                }
            }
            catch { }
        }



        // ── Programs and Features ────────────────────────────────────
        void DisableProgramsAndFeatures()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Programs");
            key.SetValue("NoProgramsAndFeatures", 1, RegistryValueKind.DWord);
        }

        void EnableProgramsAndFeatures()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Programs");
            key.SetValue("NoProgramsAndFeatures", 0, RegistryValueKind.DWord);
        }

        bool IsProgramsAndFeaturesDisabled()
        {
            return GetPolicyAny(@"Software\Microsoft\Windows\CurrentVersion\Policies\Programs", "NoProgramsAndFeatures");
        }

        private void btnHidePrograms_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableProgramsAndFeatures();
            RefreshPolicy();
            Notify("Programs and Features has been disabled");
            LoadStatus();
        }

        private void btnShowPrograms_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            EnableProgramsAndFeatures();
            RefreshPolicy();
            Notify("Programs and Features is enabled");
            LoadStatus();
        }

        // ── Start Menu Uninstall ─────────────────────────────────────
        void DisableStartMenuUninstall(bool disable)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\Explorer");
            if (disable)
                key.SetValue("NoUninstallFromStart", 1, RegistryValueKind.DWord);
            else
                key.DeleteValue("NoUninstallFromStart", false);
        }

        private void btnDisableStartUninstall_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableStartMenuUninstall(true);
            RefreshPolicy();
            Notify("Start Menu uninstall has been disabled");
            LoadStatus();
        }

        private void btnEnableStartUninstall_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableStartMenuUninstall(false);
            RefreshPolicy();
            Notify("Start Menu uninstall has been enabled");
            LoadStatus();
        }

        // ── Win + R ──────────────────────────────────────────────────
        void DisableWinR(bool disable)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer");
            if (disable)
                key.SetValue("NoRun", 1, RegistryValueKind.DWord);
            else
                key.DeleteValue("NoRun", false);
        }

        bool IsWinRDisabled()
        {
            return GetPolicyAny(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRun");
        }

        private void btnDisableRun_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableWinR(true);
            RefreshPolicy();
            Notify("Win + R has been disabled");
            LoadStatus();
        }

        private void btnEnableRun_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableWinR(false);
            RefreshPolicy();
            Notify("Win + R has been enabled");
            LoadStatus();
        }

        // ── Control Panel & Settings ─────────────────────────────────
        void DisableSystemSettings(bool disable)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer");
            if (disable)
                key.SetValue("NoControlPanel", 1, RegistryValueKind.DWord);
            else
                key.DeleteValue("NoControlPanel", false);
        }

        bool IsSystemSettingsDisabled()
        {
            if (GetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel") == 1) return true;
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel") == 1) return true;
            // Check thêm key RestrictCpl
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "RestrictCpl") == 1) return true;
            if (GetPolicyLM(@"Software\Policies\Microsoft\Windows\System", "NoControlPanel") == 1) return true;
            return false;
        }

        private void btnDisableSystem_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableSystemSettings(true);
            RefreshPolicy();
            Notify("Control Panel & Settings have been disabled");
            LoadStatus();
        }

        private void btnEnableSystem_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableSystemSettings(false);
            RefreshPolicy();
            Notify("Control Panel & Settings have been enabled");
            LoadStatus();
        }

        // ── Display Settings ─────────────────────────────────────────
        void DisableDisplaySettings(bool disable)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (disable)
                key.SetValue("NoDispCPL", 1, RegistryValueKind.DWord);
            else
                key.DeleteValue("NoDispCPL", false);
        }

        bool IsDisplaySettingsDisabled()
        {
            if (GetPolicy(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL") == 1) return true;
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL") == 1) return true;
            // Check thêm path khác
            if (GetPolicyLM(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDispCPL") == 1) return true;
            return false;
        }

        private void btnDisableDisplay_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableDisplaySettings(true);
            RefreshPolicy();
            Notify("Display Settings has been disabled");
            LoadStatus();
        }

        private void btnEnableDisplay_Click(object sender, EventArgs e)
        {
            if (IsRegistryDisabled()) { Notify("Registry đang bị khóa. Không thể thay đổi chức năng khác."); return; }
            DisableDisplaySettings(false);
            RefreshPolicy();
            Notify("Display Settings has been enabled");
            LoadStatus();
        }
    }
}
