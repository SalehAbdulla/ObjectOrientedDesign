using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.Business;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The screen where an adviser approves, modifies or rejects a student's course plan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the screen that demonstrates the Observer pattern end to end. Pressing
    /// Approve does not write a notification here: it calls
    /// <see cref="CourseAdvisingService.ApproveRequest"/>, which attaches the observers and
    /// lets the plan's own <c>Approve</c> method announce the change. The message the
    /// student later sees was written by <c>StudentNotificationService</c>, and the audit
    /// line was written by <c>AuditLogService</c>, without this screen knowing either of
    /// them exists.
    /// </para>
    /// <para>
    /// The queue is filtered by <c>AdviserId</c> at the database, so an adviser only ever
    /// sees plans that are actually theirs.
    /// </para>
    /// </remarks>
    public class frmCourseRequestReview : Form
    {
        /// <summary>The signed-in adviser.</summary>
        private readonly Adviser adviser;

        /// <summary>The service that performs the decision.</summary>
        private readonly CourseAdvisingService advising;

        /// <summary>Maps a student key to a display name, loaded once.</summary>
        private readonly Dictionary<int, string> studentNames = new Dictionary<int, string>();

        /// <summary>The grid of plans awaiting a decision.</summary>
        private DataGridView gridPlans;

        /// <summary>The grid of courses inside the selected plan.</summary>
        private DataGridView gridLines;

        /// <summary>The remark box, whose contents become the adviser's comment.</summary>
        private TextBox txtRemark;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        /// <param name="adviser">The adviser whose queue is shown.</param>
        public frmCourseRequestReview(Adviser adviser)
        {
            if (adviser == null)
            {
                throw new ArgumentNullException("adviser");
            }

            this.adviser = adviser;
            this.advising = new CourseAdvisingService();
            this.LoadStudentNames();
            this.BuildInterface();
        }

        /// <summary>Loads the names used to label each plan.</summary>
        private void LoadStudentNames()
        {
            foreach (Student student in new UserRepository().GetStudents())
            {
                this.studentNames[student.StudentId] = student.FullName;
            }
        }

        /// <summary>
        /// A flattened view of a plan for the queue grid, so the grid can show the student
        /// name and a formatted submission date without the domain object being changed.
        /// </summary>
        private class PlanRow
        {
            /// <summary>Gets or sets the plan.</summary>
            public CourseRequest Request { get; set; }

            /// <summary>Gets the plan number.</summary>
            public int PlanNumber
            {
                get { return this.Request == null ? 0 : this.Request.RequestId; }
            }

            /// <summary>Gets the student's name.</summary>
            public string StudentName { get; set; }

            /// <summary>Gets how many courses the plan asks for.</summary>
            public int CourseCount
            {
                get { return this.Request == null ? 0 : this.Request.GetCourseCount(); }
            }

            /// <summary>Gets the total credit hours the plan asks for.</summary>
            public int CreditTotal
            {
                get { return this.Request == null ? 0 : this.Request.GetTotalCreditHours(); }
            }

            /// <summary>Gets when the plan was submitted.</summary>
            public string Submitted { get; set; }
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Review submitted course plans");
            this.ClientSize = new Size(920, 620);

            this.Controls.Add(UiTheme.CreateHeader("Course plans awaiting your decision"));

            Label plansLabel = UiTheme.CreateLabel("Submitted plans");
            plansLabel.Font = UiTheme.SubHeadingFont;
            plansLabel.Location = new Point(14, 60);
            plansLabel.AutoSize = true;
            this.Controls.Add(plansLabel);

            this.gridPlans = UiTheme.CreateGrid();
            this.gridPlans.Location = new Point(14, 84);
            this.gridPlans.Size = new Size(880, 180);
            this.gridPlans.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.gridPlans.SelectionChanged += this.OnPlanSelected;
            UiTheme.AddColumn(this.gridPlans, "PlanNumber", "Plan", 10);
            UiTheme.AddColumn(this.gridPlans, "StudentName", "Student", 34);
            UiTheme.AddColumn(this.gridPlans, "CourseCount", "Courses", 10);
            UiTheme.AddColumn(this.gridPlans, "CreditTotal", "Credits", 10);
            UiTheme.AddColumn(this.gridPlans, "Submitted", "Submitted", 22);
            this.Controls.Add(this.gridPlans);

            Label linesLabel = UiTheme.CreateLabel("Courses requested");
            linesLabel.Font = UiTheme.SubHeadingFont;
            linesLabel.Location = new Point(14, 274);
            linesLabel.AutoSize = true;
            this.Controls.Add(linesLabel);

            this.gridLines = UiTheme.CreateGrid();
            this.gridLines.Location = new Point(14, 298);
            this.gridLines.Size = new Size(880, 160);
            this.gridLines.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UiTheme.AddColumn(this.gridLines, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridLines, "Title", "Course", 32);
            UiTheme.AddColumn(this.gridLines, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridLines, "Eligibility", "Verdict", 12);
            UiTheme.AddColumn(this.gridLines, "Reason", "Explanation", 38);
            this.Controls.Add(this.gridLines);

            Label remarkLabel = UiTheme.CreateLabel("Remark for the student (required for either decision)");
            remarkLabel.Location = new Point(14, 470);
            remarkLabel.AutoSize = true;
            this.Controls.Add(remarkLabel);

            this.txtRemark = UiTheme.CreateTextBox(600);
            this.txtRemark.Location = new Point(14, 492);
            this.txtRemark.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.Controls.Add(this.txtRemark);

            Button approve = UiTheme.CreateButton("Approve plan", this.OnApprove);
            approve.Location = new Point(14, 534);
            approve.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(approve);

            Button reject = UiTheme.CreateButton("Reject plan", this.OnReject);
            reject.Location = new Point(166, 534);
            reject.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(reject);

            Button refresh = UiTheme.CreateButton("Refresh queue", this.OnRefresh);
            refresh.Location = new Point(318, 534);
            refresh.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(refresh);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(794, 534);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(14, 578);
            this.lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblStatus.AutoSize = true;
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the queue when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.ReloadQueue();
        }

        /// <summary>Reloads the queue of plans awaiting a decision.</summary>
        private void ReloadQueue()
        {
            UiTheme.Guard(this, delegate
            {
                List<PlanRow> rows = new List<PlanRow>();

                foreach (CourseRequest request in
                         this.advising.GetSubmittedRequestsForAdviser(this.adviser.AdviserId))
                {
                    PlanRow row = new PlanRow();
                    row.Request = request;
                    row.StudentName = this.studentNames.ContainsKey(request.StudentId)
                        ? this.studentNames[request.StudentId]
                        : "student " + request.StudentId;
                    row.Submitted = request.SubmittedDate.HasValue
                        ? request.SubmittedDate.Value.ToString("yyyy-MM-dd HH:mm")
                        : string.Empty;

                    rows.Add(row);
                }

                this.gridPlans.DataSource = null;
                this.gridPlans.DataSource = rows;
                this.gridLines.DataSource = null;
                this.txtRemark.Text = string.Empty;

                this.lblStatus.Text = rows.Count == 0
                    ? "Nothing is awaiting your decision."
                    : rows.Count + " plan(s) awaiting your decision.";
            });
        }

        /// <summary>Shows the courses of the plan the adviser just selected.</summary>
        private void OnPlanSelected(object sender, EventArgs e)
        {
            CourseRequest request = this.SelectedRequest();

            this.gridLines.DataSource = null;

            if (request != null)
            {
                this.gridLines.DataSource = new List<CourseRequestLine>(request.Lines);
            }
        }

        /// <summary>
        /// Approves the selected plan. The decision, the notification and the audit entry
        /// are all produced by the service and the observers, not by this screen.
        /// </summary>
        private void OnApprove(object sender, EventArgs e)
        {
            this.Decide(true);
        }

        /// <summary>Rejects the selected plan.</summary>
        private void OnReject(object sender, EventArgs e)
        {
            this.Decide(false);
        }

        /// <summary>Reloads the queue.</summary>
        private void OnRefresh(object sender, EventArgs e)
        {
            this.ReloadQueue();
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Records a decision on the selected plan after asking for confirmation.
        /// </summary>
        /// <param name="approve">True to approve, false to reject.</param>
        private void Decide(bool approve)
        {
            UiTheme.Guard(this, delegate
            {
                CourseRequest request = this.SelectedRequest();

                if (request == null)
                {
                    UiTheme.ShowError(this, "Select a plan in the queue first.");
                    return;
                }

                if (string.IsNullOrEmpty(this.txtRemark.Text.Trim()))
                {
                    UiTheme.ShowError(this,
                        "A remark is required. The student sees it, and the domain model refuses " +
                        "a decision without one.");
                    return;
                }

                string action = approve ? "approve" : "reject";

                if (!UiTheme.Confirm(this, "Are you sure you want to " + action + " plan #" +
                    request.RequestId + "?"))
                {
                    return;
                }

                bool saved = approve
                    ? this.advising.ApproveRequest(request, this.txtRemark.Text)
                    : this.advising.RejectRequest(request, this.txtRemark.Text);

                if (!saved)
                {
                    UiTheme.ShowError(this, "The decision could not be saved.");
                    return;
                }

                this.ReloadQueue();

                UiTheme.ShowInfo(this,
                    "Plan #" + request.RequestId + " has been " + (approve ? "approved" : "rejected") + ".\r\n\r\n" +
                    "The notification observer has written a message for the student, " +
                    "and the audit observer has appended a line to the audit log.");
            });
        }

        /// <summary>Returns the plan currently selected in the queue.</summary>
        private CourseRequest SelectedRequest()
        {
            if (this.gridPlans.CurrentRow == null)
            {
                return null;
            }

            PlanRow row = this.gridPlans.CurrentRow.DataBoundItem as PlanRow;

            return row == null ? null : row.Request;
        }
    }
}
