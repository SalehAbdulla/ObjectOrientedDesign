using System;
using SCAIS.Business;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The dashboard an adviser sees after signing in.
    /// </summary>
    /// <remarks>
    /// The review function is offered here and nowhere else because
    /// <see cref="User.CanApproveCourseRequests"/> is true only for an adviser. The
    /// dashboard reads that capability rather than testing the role, so adding a fourth
    /// role that may also approve plans would need no change to this screen.
    /// </remarks>
    public class frmAdviserDashboard : DashboardForm
    {
        /// <summary>The signed-in adviser.</summary>
        private readonly Adviser adviser;

        /// <summary>Creates the dashboard.</summary>
        /// <param name="account">The adviser who signed in.</param>
        public frmAdviserDashboard(Adviser account)
            : base(account, account.GetDashboardTitle())
        {
            this.adviser = account;
            this.BuildMenu();
        }

        /// <summary>Builds the menu and reports the size of the review queue.</summary>
        private void BuildMenu()
        {
            this.AddMenuCaption("My advisees");
            this.AddMenuItem("View my advisees and their records", this.OnAdvisees);

            this.AddMenuCaption("Course plans");

            // The capability, not the role, decides whether this function exists.
            if (this.adviser.CanApproveCourseRequests)
            {
                this.AddMenuItem("Review submitted course plans", this.OnReviewPlans);
            }

            this.AddMenuCaption("Reporting");
            this.AddMenuItem("Reports", this.OnReports);

            this.ReportQueueSize();
        }

        /// <summary>Reports how many plans are waiting, so the adviser knows before opening the screen.</summary>
        private void ReportQueueSize()
        {
            int waiting = new CourseAdvisingService()
                .GetSubmittedRequestsForAdviser(this.adviser.AdviserId)
                .Count;

            this.SetStatus(waiting == 0
                ? "No course plans are awaiting your decision."
                : waiting + " course plan(s) are awaiting your decision.");
        }

        /// <summary>Opens the advisee list.</summary>
        private void OnAdvisees(object sender, EventArgs e)
        {
            this.OpenChild(new frmAdviseeList(this.adviser));
        }

        /// <summary>Opens the review screen and refreshes the queue size afterwards.</summary>
        private void OnReviewPlans(object sender, EventArgs e)
        {
            this.OpenChild(new frmCourseRequestReview(this.adviser));
            this.ReportQueueSize();
        }

        /// <summary>Opens the reports screen.</summary>
        private void OnReports(object sender, EventArgs e)
        {
            this.OpenChild(new frmReports());
        }
    }
}
