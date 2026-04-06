using System;
using System.Drawing;
using System.Windows.Forms;

namespace SystemControlTool
{
    public class LoginForm : Form
    {
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblPassword;

        public string EnteredPassword => txtPassword.Text;

        public LoginForm()
        {
            InitUI();
        }

        void InitUI()
        {
            this.Text = "Đăng nhập";
            this.Size = new Size(320, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            lblTitle = new Label()
            {
                Text = "System Control Tool",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(60, 20)
            };

            lblPassword = new Label()
            {
                Text = "Mật khẩu:",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(20, 70)
            };

            txtPassword = new TextBox()
            {
                Location = new Point(100, 67),
                Size = new Size(180, 25),
                PasswordChar = '●',
                Font = new Font("Segoe UI", 10)
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) TryLogin();
            };

            btnLogin = new Button()
            {
                Text = "Đăng nhập",
                Location = new Point(100, 110),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => TryLogin();

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
        }

        void TryLogin()
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
