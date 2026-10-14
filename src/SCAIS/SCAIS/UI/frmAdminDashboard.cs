using System;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The dashboard an administrator sees after signing in.
    /// </summary>
    /// <remarks>
    /// The functions offered here are filtered by the account's capability flags rather
    /// than by a role test. A registrar level administrator sees the account functions but
    /// not the catalogue, because <see cref="Administrator.CanManageCatalogue"/> returns
    /// false for that access level - and this screen never asks what the role is.
    /// </remarks>
    public class frmAdminDashboard : DashboardForm
    {
        /// <summary>The signed-in administrator.</summary>
        private readonly Administrator administrator;

        /// <summary>Creates the dashboard.</summary>
        /// <param name="account">The administrator who signed in.</param>
        public frmAdminDashboard(Administrator account)
            : base(account, account.GetDashboardTitle())
        {
            this.administrator = account;
            this.BuildMenu();
        }

        /// <summary>Builds the menu, omitting functions the access level does not allow.</summary>
        private void BuildMenu()
        {
            this.AddMenuCaption("Accounts and advising");

            if (this.administrator.CanManageUsers)
            {
                this.AddMenuItem("Manage user accounts", this.OnManageUsers);
            }

            this.AddMenuItem("Assign advisees to advisers", this.OnAssignAdvisees);

            this.AddMenuCaption("Curriculum");

            if (this.administrator.CanManageCatalogue)
            {
                this.AddMenuItem("Maintain course catalogue", this.OnCourseCatalogue);
                this.AddMenuItem("Maintain prerequisite rules", this.OnCourseCatalogue);
            }

            this.AddMenuCaption("Reporting");
            this.AddMenuItem("Reports", this.OnReports);

            this.SetStatus(string.Format(
                "Signed in as {0} with access level {1}.",
                this.administrator.FullName, this.administrator.AccessLevel));
        }

        /// <summary>Opens the user account screen.</summary>
        private void OnManageUsers(object sender, EventArgs e)
        {
            this.OpenChild(new frmUserManagement());
        }

        /// <summary>Opens the assign-advisees screen.</summary>
        private void OnAssignAdvisees(object sender, EventArgs e)
        {
            this.OpenChild(new frmAssignAdvisees());
        }

        /// <summary>Opens the course catalogue and prerequisite rule screen.</summary>
        private void OnCourseCatalogue(object sender, EventArgs e)
        {
            this.OpenChild(new frmCourseCatalog());
        }

        /// <summary>Opens the reports screen.</summary>
        private void OnReports(object sender, EventArgs e)
        {
            this.OpenChild(new frmReports());
        }
    }
}
