using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The administrator's screen for editing and deactivating accounts.
    /// </summary>
    /// <remarks>
    /// The grid is populated from <see cref="UserRepository"/> and the rows are the domain
    /// objects themselves, so editing a row and pressing Save calls straight through to
    /// <see cref="UserRepository.Update"/>. Because the objects are
    /// <see cref="Student"/>, <see cref="Adviser"/> or <see cref="Administrator"/>, the
    /// repository's own polymorphic dispatch decides which role table is written - this
    /// screen does not branch on the role at all.
    /// </remarks>
    public class frmUserManagement : Form
    {
        /// <summary>The repository behind the screen.</summary>
        private readonly UserRepository users;

        /// <summary>The grid of accounts.</summary>
        private DataGridView grid;

        /// <summary>The full name editor.</summary>
        private TextBox txtFullName;

        /// <summary>The e-mail editor.</summary>
        private TextBox txtEmail;

        /// <summary>The active flag editor.</summary>
        private CheckBox chkActive;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        public frmUserManagement()
        {
            this.users = new UserRepository();
            this.BuildInterface();
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Manage user accounts");
            this.ClientSize = new Size(920, 600);

            this.Controls.Add(UiTheme.CreateHeader("Manage user accounts"));

            this.grid = UiTheme.CreateGrid();
            this.grid.Location = new Point(14, 60);
            this.grid.Size = new Size(880, 360);
            this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.grid.SelectionChanged += this.OnSelectionChanged;
            UiTheme.AddColumn(this.grid, "UserId", "Id", 6);
            UiTheme.AddColumn(this.grid, "Username", "Username", 16);
            UiTheme.AddColumn(this.grid, "FullName", "Full name", 28);
            UiTheme.AddColumn(this.grid, "Email", "E-mail", 26);
            UiTheme.AddColumn(this.grid, "Role", "Role", 12);
            UiTheme.AddColumn(this.grid, "IsActive", "Active", 8);
            this.Controls.Add(this.grid);

            Label editorLabel = UiTheme.CreateLabel("Edit the selected account");
            editorLabel.Font = UiTheme.SubHeadingFont;
            editorLabel.Location = new Point(14, 432);
            editorLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            editorLabel.AutoSize = true;
            this.Controls.Add(editorLabel);

            this.txtFullName = UiTheme.CreateTextBox(240);
            this.txtFullName.Location = new Point(14, 480);
            this.txtFullName.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(this.txtFullName);

            this.txtEmail = UiTheme.CreateTextBox(240);
            this.txtEmail.Location = new Point(276, 480);
            this.txtEmail.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(this.txtEmail);

            this.chkActive = new CheckBox();
            this.chkActive.Text = "Account may sign in";
            this.chkActive.Font = UiTheme.BodyFont;
            this.chkActive.Location = new Point(538, 482);
            this.chkActive.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.chkActive.AutoSize = true;
            this.Controls.Add(this.chkActive);

            Button save = UiTheme.CreateButton("Save changes", this.OnSave);
            save.Location = new Point(14, 520);
            save.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(save);

            Button resetPassword = UiTheme.CreateButton("Reset password", this.OnResetPassword);
            resetPassword.Location = new Point(166, 520);
            resetPassword.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(resetPassword);

            Button delete = UiTheme.CreateButton("Delete account", this.OnDelete);
            delete.Location = new Point(318, 520);
            delete.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(delete);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefresh);
            refresh.Location = new Point(470, 520);
            refresh.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(refresh);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(794, 520);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(14, 564);
            this.lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblStatus.AutoSize = true;
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the accounts when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.Reload();
        }

        /// <summary>Reloads the grid from the database.</summary>
        private void Reload()
        {
            UiTheme.Guard(this, delegate
            {
                List<User> accounts = this.users.GetAll();

                this.grid.DataSource = null;
                this.grid.DataSource = accounts;

                this.lblStatus.Text = accounts.Count + " account(s) loaded.";
            });
        }

        /// <summary>Copies the selected account into the editor fields.</summary>
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            User selected = this.SelectedAccount();

            if (selected == null)
            {
                return;
            }

            this.txtFullName.Text = selected.FullName;
            this.txtEmail.Text = selected.Email;
            this.chkActive.Checked = selected.IsActive;

            // The polymorphic summary is shown in the status line, so the administrator can
            // see the role specific detail without a second screen.
            this.lblStatus.Text = selected.GetProfileSummary();
        }

        /// <summary>Saves the editor fields back through the repository.</summary>
        private void OnSave(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                User selected = this.SelectedAccount();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select an account first.");
                    return;
                }

                // The domain object validates the values, so an invalid name or e-mail is
                // rejected before the database is touched.
                selected.FullName = this.txtFullName.Text;
                selected.Email = this.txtEmail.Text;
                selected.IsActive = this.chkActive.Checked;

                if (!this.users.Update(selected))
                {
                    UiTheme.ShowError(this, "The account could not be saved.");
                    return;
                }

                this.Reload();
                UiTheme.ShowInfo(this, "The account has been saved.");
            });
        }

        /// <summary>Replaces the selected account's password with a temporary one.</summary>
        private void OnResetPassword(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                User selected = this.SelectedAccount();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select an account first.");
                    return;
                }

                const string temporary = "Welcome@123";

                if (!UiTheme.Confirm(this, "Reset the password of " + selected.Username +
                    " to '" + temporary + "'?"))
                {
                    return;
                }

                // The plain text password is hashed inside the repository and is never
                // written to the database or to a log.
                if (!this.users.UpdatePassword(selected.UserId, temporary))
                {
                    UiTheme.ShowError(this, "The password could not be reset.");
                    return;
                }

                UiTheme.ShowInfo(this, "The password has been reset to '" + temporary + "'.");
            });
        }

        /// <summary>Deletes the selected account after confirmation.</summary>
        private void OnDelete(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                User selected = this.SelectedAccount();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select an account first.");
                    return;
                }

                if (!UiTheme.Confirm(this, "Permanently delete the account '" + selected.Username +
                    "'? This cannot be undone."))
                {
                    return;
                }

                if (!this.users.Delete(selected.UserId))
                {
                    UiTheme.ShowError(this,
                        "The account could not be deleted. It may be referenced by enrolments " +
                        "or course plans, in which case deactivating it is the correct action.");
                    return;
                }

                this.Reload();
                UiTheme.ShowInfo(this, "The account has been deleted.");
            });
        }

        /// <summary>Reloads the grid.</summary>
        private void OnRefresh(object sender, EventArgs e)
        {
            this.Reload();
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>Returns the account currently selected in the grid.</summary>
        private User SelectedAccount()
        {
            if (this.grid.CurrentRow == null)
            {
                return null;
            }

            return this.grid.CurrentRow.DataBoundItem as User;
        }
    }
}
