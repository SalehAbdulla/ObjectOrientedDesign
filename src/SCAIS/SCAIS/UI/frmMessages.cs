using System;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// Shows the messages that have been written for the signed-in account.
    /// </summary>
    /// <remarks>
    /// This screen is the visible end of the Observer pattern. Nothing on it writes a
    /// message: the rows were produced by
    /// <see cref="SCAIS.Observers.StudentNotificationService"/> reacting to a plan changing
    /// status. The screen only reads what the observer already stored, which is why it
    /// needs to know nothing about approvals or rejections.
    /// </remarks>
    public class frmMessages : Form
    {
        /// <summary>Reads and writes messages.</summary>
        private readonly NotificationRepository repository;

        /// <summary>The account whose messages are shown.</summary>
        private readonly User account;

        /// <summary>The grid of messages.</summary>
        private DataGridView grid;

        /// <summary>Creates the screen.</summary>
        /// <param name="account">The account whose messages are shown.</param>
        public frmMessages(User account)
        {
            if (account == null)
            {
                throw new ArgumentNullException("account");
            }

            this.account = account;
            this.repository = new NotificationRepository();
            this.BuildInterface();
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "My messages");
            this.ClientSize = new Size(820, 460);

            this.Controls.Add(UiTheme.CreateHeader("My messages"));

            Label note = UiTheme.CreateLabel(
                "Messages are written automatically when one of your course plans changes state.");
            note.ForeColor = Color.DimGray;
            note.Location = new Point(16, 60);
            this.Controls.Add(note);

            this.grid = UiTheme.CreateGrid();
            this.grid.Location = new Point(16, 90);
            this.grid.Size = new Size(788, 300);
            this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            UiTheme.AddColumn(this.grid, "CreatedDate", "Received", 22);
            UiTheme.AddColumn(this.grid, "Message", "Message", 63);
            UiTheme.AddColumn(this.grid, "IsRead", "Read", 15);
            this.Controls.Add(this.grid);

            Button markAll = UiTheme.CreateButton("Mark all as read", this.OnMarkAllRead);
            markAll.Location = new Point(16, 404);
            markAll.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(markAll);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(704, 404);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the messages when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.Reload();
        }

        /// <summary>Reloads the message list.</summary>
        private void Reload()
        {
            UiTheme.Guard(this, delegate
            {
                this.grid.DataSource = null;
                this.grid.DataSource = this.repository.GetForUser(this.account.UserId);
            });
        }

        /// <summary>Marks every message as read.</summary>
        private void OnMarkAllRead(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                int updated = this.repository.MarkAllAsRead(this.account.UserId);

                UiTheme.ShowInfo(this, updated == 0
                    ? "There was nothing left to mark as read."
                    : updated + " message(s) marked as read.");

                this.Reload();
            });
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
