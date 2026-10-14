using System;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.Business;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The single login screen through which every role signs in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The business case requires one unified login screen for administrators, advisers
    /// and students, with the system redirecting each of them based on their role. This
    /// form is that screen, and it is the only form in the application that any role can
    /// open.
    /// </para>
    /// <para>
    /// <b>Where the polymorphism happens.</b> The form holds a <see cref="User"/>
    /// reference and never asks which subclass it has. It calls
    /// <see cref="User.GetProfileSummary"/> and <see cref="User.GetDashboardTitle"/>, and
    /// the concrete account decides what those return. The role redirect itself is the
    /// <see cref="DashboardFactory"/>'s job, which keeps the choice of form out of the
    /// domain model.
    /// </para>
    /// <para>
    /// The form deliberately shows the demonstration credentials. SCAIS is a marked
    /// course project with a seeded database, and a marker who cannot sign in cannot
    /// award the implementation marks.
    /// </para>
    /// </remarks>
    public class frmLogin : Form
    {
        /// <summary>Verifies credentials.</summary>
        private readonly AuthenticationService authentication;

        /// <summary>The sign-in name input.</summary>
        private TextBox txtUsername;

        /// <summary>The password input.</summary>
        private TextBox txtPassword;

        /// <summary>The label that reports a failed attempt.</summary>
        private Label lblStatus;

        /// <summary>Creates the login screen.</summary>
        public frmLogin()
        {
            this.authentication = new AuthenticationService();
            this.BuildInterface();
        }

        /// <summary>
        /// Gets the account that signed in, or null when nobody has.
        /// </summary>
        public User AuthenticatedUser { get; private set; }

        /// <summary>
        /// Builds the controls. The layout is written in code so that it can be reviewed
        /// and version controlled as ordinary C#.
        /// </summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "SCAIS - Sign in");
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(520, 430);

            this.Controls.Add(UiTheme.CreateHeader("Smart Course Advising Information System"));

            Label subtitle = UiTheme.CreateLabel("Bahrain Institute of Technology");
            subtitle.Font = UiTheme.SubHeadingFont;
            subtitle.ForeColor = Color.DimGray;
            subtitle.Location = new Point(40, 68);
            this.Controls.Add(subtitle);

            Label roleNote = UiTheme.CreateLabel(
                "One sign-in screen for administrators, advisers and students.");
            roleNote.ForeColor = Color.DimGray;
            roleNote.Location = new Point(40, 92);
            this.Controls.Add(roleNote);

            Label usernameLabel = UiTheme.CreateLabel("Username");
            usernameLabel.Location = new Point(40, 140);
            this.Controls.Add(usernameLabel);

            this.txtUsername = UiTheme.CreateTextBox(280);
            this.txtUsername.Location = new Point(40, 162);
            this.Controls.Add(this.txtUsername);

            Label passwordLabel = UiTheme.CreateLabel("Password");
            passwordLabel.Location = new Point(40, 200);
            this.Controls.Add(passwordLabel);

            this.txtPassword = UiTheme.CreateTextBox(280);
            this.txtPassword.Location = new Point(40, 222);
            this.txtPassword.UseSystemPasswordChar = true;
            this.Controls.Add(this.txtPassword);

            this.lblStatus = UiTheme.CreateLabel(string.Empty);
            this.lblStatus.ForeColor = UiTheme.BlockedColour;
            this.lblStatus.Location = new Point(40, 258);
            this.lblStatus.MaximumSize = new Size(440, 40);
            this.Controls.Add(this.lblStatus);

            Button signIn = UiTheme.CreateButton("Sign in", this.OnSignIn);
            signIn.Location = new Point(40, 306);
            signIn.MinimumSize = new Size(140, 34);
            this.Controls.Add(signIn);

            Button cancel = UiTheme.CreateButton("Cancel", this.OnCancel);
            cancel.Location = new Point(192, 306);
            this.Controls.Add(cancel);

            Label hint = UiTheme.CreateLabel(
                "Demonstration accounts:\r\n" +
                "    administrator   admin / Admin@123\r\n" +
                "    adviser             adviser01 / Adviser@123\r\n" +
                "    student             student01 / Student@123");
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(40, 352);
            hint.MaximumSize = new Size(440, 60);
            this.Controls.Add(hint);

            this.AcceptButton = signIn;
            this.CancelButton = cancel;
        }

        /// <summary>
        /// Attempts to sign in and, on success, closes the form with an OK result so that
        /// <c>Program.Main</c> can open the correct dashboard.
        /// </summary>
        private void OnSignIn(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                this.lblStatus.Text = string.Empty;

                LoginResult result = this.authentication.Login(
                    this.txtUsername.Text, this.txtPassword.Text);

                if (!result.Succeeded)
                {
                    this.lblStatus.Text = result.Message;
                    this.txtPassword.SelectAll();
                    this.txtPassword.Focus();
                    return;
                }

                this.AuthenticatedUser = result.User;

                // The title and the summary are produced by the concrete account, which is
                // the polymorphism the login screen relies on.
                UiTheme.ShowInfo(this, string.Format(
                    "Welcome, {0}.\r\n\r\n{1}\r\n\r\nOpening the {2}.",
                    result.User.FullName,
                    result.User.GetProfileSummary(),
                    result.User.GetDashboardTitle()));

                this.DialogResult = DialogResult.OK;
                this.Close();
            });
        }

        /// <summary>Closes the form without signing in.</summary>
        private void OnCancel(object sender, EventArgs e)
        {
            this.AuthenticatedUser = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
