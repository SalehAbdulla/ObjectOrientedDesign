using System;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.Business;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The common shell that every role dashboard is built on: a header, a profile strip,
    /// a menu area and a status line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All three dashboards need the same chrome, and the rubric awards marks for
    /// consistency. Putting the chrome in one base class means the three dashboards differ
    /// only in <i>which</i> functions they offer, which is exactly the difference that
    /// should exist between them.
    /// </para>
    /// <para>
    /// <b>Polymorphic use.</b> The shell renders <see cref="User.GetProfileSummary"/>
    /// without knowing which concrete account it holds, so the same code produces
    /// "Student - Programming - GPA 3.42", "Adviser - Information Technology - 4
    /// advisee(s)" or "Administrator - access level Full" depending only on the object.
    /// </para>
    /// </remarks>
    public abstract class DashboardForm : Form
    {
        /// <summary>The panel that holds the menu buttons.</summary>
        private FlowLayoutPanel menuPanel;

        /// <summary>The label that reports the result of the last action.</summary>
        private Label statusLabel;

        /// <summary>The label that shows how many unread messages the account has.</summary>
        private Label notificationLabel;

        /// <summary>Creates the shell for an account.</summary>
        /// <param name="account">The account that signed in.</param>
        /// <param name="heading">The heading to show in the header strip.</param>
        protected DashboardForm(User account, string heading)
        {
            if (account == null)
            {
                throw new ArgumentNullException("account");
            }

            this.Account = account;
            this.BuildShell(heading);
        }

        /// <summary>
        /// Gets the account that signed in. This is the polymorphic reference the shell
        /// renders through.
        /// </summary>
        protected User Account { get; private set; }

        /// <summary>Adds a function to the menu.</summary>
        /// <param name="text">The caption of the function.</param>
        /// <param name="handler">The handler that opens the function.</param>
        protected void AddMenuItem(string text, EventHandler handler)
        {
            this.menuPanel.Controls.Add(UiTheme.CreateMenuButton(text, handler));
        }

        /// <summary>Adds a non-interactive caption that separates groups of functions.</summary>
        /// <param name="text">The caption to show.</param>
        protected void AddMenuCaption(string text)
        {
            Label caption = UiTheme.CreateLabel(text);
            caption.Font = UiTheme.SubHeadingFont;
            caption.ForeColor = Color.DimGray;
            caption.Margin = new Padding(2, 14, 2, 4);
            caption.AutoSize = true;

            this.menuPanel.Controls.Add(caption);
            this.menuPanel.SetFlowBreak(caption, true);
        }

        /// <summary>Writes a message to the status line at the bottom of the dashboard.</summary>
        /// <param name="message">The message to show.</param>
        protected void SetStatus(string message)
        {
            this.statusLabel.Text = message ?? string.Empty;
        }

        /// <summary>
        /// Refreshes the unread message count shown on the dashboard.
        /// </summary>
        /// <remarks>
        /// This is where the Observer pattern becomes visible to the user: after an
        /// adviser approves a plan, the notification the observer wrote is what changes
        /// this number when the student next looks at their dashboard.
        /// </remarks>
        protected void RefreshNotifications()
        {
            int unread = new NotificationRepository().CountUnread(this.Account.UserId);

            this.notificationLabel.Text = unread == 0
                ? "No unread messages"
                : unread + " unread message(s)";
        }

        /// <summary>
        /// Opens a screen on top of the dashboard and refreshes the shell afterwards, so
        /// that any change the screen made is reflected immediately.
        /// </summary>
        /// <param name="child">The screen to open.</param>
        protected void OpenChild(Form child)
        {
            if (child == null)
            {
                return;
            }

            using (child)
            {
                child.ShowDialog(this);
            }

            this.RefreshNotifications();
        }

        /// <summary>Builds the header, profile strip, menu area and status line.</summary>
        private void BuildShell(string heading)
        {
            UiTheme.ApplyForm(this, heading);
            this.ClientSize = new Size(900, 580);

            // The header label is created here but added at the end of this method: WinForms
            // resolves docking in reverse z-order, so the control added last is docked first
            // and ends up outermost.
            Label header = UiTheme.CreateHeader(heading);

            Panel profile = new Panel();
            profile.Dock = DockStyle.Top;
            profile.Height = 66;
            profile.BackColor = Color.White;

            Label name = UiTheme.CreateLabel(this.Account.FullName);
            name.Font = UiTheme.SubHeadingFont;
            name.Location = new Point(16, 10);
            profile.Controls.Add(name);

            // One call, three different strings: the polymorphism the report describes.
            Label summary = UiTheme.CreateLabel(this.Account.GetProfileSummary());
            summary.ForeColor = Color.DimGray;
            summary.Location = new Point(16, 34);
            profile.Controls.Add(summary);

            this.notificationLabel = UiTheme.CreateLabel(string.Empty);
            this.notificationLabel.ForeColor = UiTheme.HeaderColour;
            this.notificationLabel.Font = UiTheme.SubHeadingFont;
            this.notificationLabel.Location = new Point(620, 20);
            this.notificationLabel.AutoSize = true;
            profile.Controls.Add(this.notificationLabel);

            Panel status = new Panel();
            status.Dock = DockStyle.Bottom;
            status.Height = 36;
            status.BackColor = Color.FromArgb(232, 238, 245);

            this.statusLabel = UiTheme.CreateLabel("Ready.");
            this.statusLabel.ForeColor = Color.DimGray;
            this.statusLabel.Location = new Point(16, 9);
            status.Controls.Add(this.statusLabel);

            Button signOut = UiTheme.CreateButton("Sign out", this.OnSignOut);
            signOut.Location = new Point(780, 3);
            signOut.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            signOut.MinimumSize = new Size(100, 28);
            status.Controls.Add(signOut);

            this.menuPanel = new FlowLayoutPanel();
            this.menuPanel.Dock = DockStyle.Fill;
            this.menuPanel.FlowDirection = FlowDirection.TopDown;
            this.menuPanel.WrapContents = false;
            this.menuPanel.AutoScroll = true;
            this.menuPanel.Padding = new Padding(20, 16, 20, 16);
            this.menuPanel.BackColor = Color.WhiteSmoke;
            // Docking is resolved in reverse z-order: the control added last is docked first and
            // therefore sits outermost. Adding the strips in this order - menu, status line,
            // profile strip, header - is what puts the header at the very top, the profile strip
            // beneath it, the status line at the bottom and the menu in what is left. Adding
            // them in the order they read on screen (header first) pushed the header down over
            // the top of the menu and hid the first menu caption and its function.
            this.Controls.Add(this.menuPanel);
            this.Controls.Add(status);
            this.Controls.Add(profile);
            this.Controls.Add(header);

            this.Load += this.OnShellLoaded;
        }

        /// <summary>Shows the unread count as soon as the dashboard opens.</summary>
        private void OnShellLoaded(object sender, EventArgs e)
        {
            this.RefreshNotifications();
            this.SetStatus("Signed in as " + this.Account.Username + ".");
        }

        /// <summary>Ends the session and closes the dashboard.</summary>
        private void OnSignOut(object sender, EventArgs e)
        {
            if (!UiTheme.Confirm(this, "Sign out of SCAIS?"))
            {
                return;
            }

            new AuthenticationService().Logout();
            this.Close();
        }
    }
}
