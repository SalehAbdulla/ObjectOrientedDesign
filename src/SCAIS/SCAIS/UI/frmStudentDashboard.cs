using System;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The dashboard a student sees after signing in.
    /// </summary>
    /// <remarks>
    /// The student's four functions map directly onto the four student use cases in the
    /// business case: view the academic record, view the courses they are eligible for,
    /// submit preferred courses for review, and view the approved courses with the
    /// adviser's remarks. All four live on one tabbed screen, because a student works
    /// through them in one sitting.
    /// </remarks>
    public class frmStudentDashboard : DashboardForm
    {
        /// <summary>The signed-in student.</summary>
        private readonly Student student;

        /// <summary>Creates the dashboard.</summary>
        /// <param name="account">The student who signed in.</param>
        public frmStudentDashboard(Student account)
            : base(account, account.GetDashboardTitle())
        {
            this.student = account;
            this.BuildMenu();
        }

        /// <summary>Builds the menu.</summary>
        private void BuildMenu()
        {
            this.AddMenuCaption("My studies");
            this.AddMenuItem("My academic record and course plans", this.OnAcademicRecord);

            this.AddMenuCaption("Messages");
            this.AddMenuItem("View my messages", this.OnMessages);

            string track = this.student.Specialization == null
                ? "no track declared"
                : this.student.Specialization.Name;

            this.SetStatus(string.Format(
                "Declared track: {0}. Adviser: {1}.",
                track,
                this.student.Adviser == null ? "not yet assigned" : this.student.Adviser.FullName));
        }

        /// <summary>Opens the tabbed academic record screen.</summary>
        private void OnAcademicRecord(object sender, EventArgs e)
        {
            this.OpenChild(new frmMyAcademicRecord(this.student));
            this.SetStatus("Academic record refreshed.");
        }

        /// <summary>Opens the messages screen.</summary>
        private void OnMessages(object sender, EventArgs e)
        {
            this.OpenChild(new frmMessages(this.Account));
        }
    }
}
