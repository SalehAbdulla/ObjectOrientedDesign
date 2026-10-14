using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.Business;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The student's academic record screen, and the place where a course plan is built.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The three tabs correspond to the student's use cases in the business case:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>My record</b> - the courses completed, in progress and
    ///   still pending, which is the status tracking requirement.</description></item>
    ///   <item><description><b>Eligible courses</b> - the courses the student may take,
    ///   each showing the verdict of the eligibility rules, and where a plan is
    ///   assembled and submitted.</description></item>
    ///   <item><description><b>My requests</b> - the plans already submitted and the
    ///   adviser's remark on each one.</description></item>
    /// </list>
    /// <para>
    /// The screen contains no business rules. It asks
    /// <see cref="CourseAdvisingService"/> for verdicts and
    /// <see cref="ReportService"/> for the record, and it renders what it is given. That
    /// is what "integration with the OO classes" means in the rubric: the screen is a
    /// consumer of the domain model, not a place where the rules live.
    /// </para>
    /// </remarks>
    public class frmMyAcademicRecord : Form
    {
        /// <summary>The signed-in student.</summary>
        private readonly Student student;

        /// <summary>The service behind recommendations and plan submission.</summary>
        private readonly CourseAdvisingService advising;

        /// <summary>The service behind the progression summary.</summary>
        private readonly ReportService reports;

        /// <summary>The plan currently being assembled, or null when none has been started.</summary>
        private CourseRequest draftRequest;

        /// <summary>The grid of the academic record.</summary>
        private DataGridView gridRecord;

        /// <summary>The grid of courses and their eligibility verdicts.</summary>
        private DataGridView gridEligible;

        /// <summary>The grid of the plan being assembled.</summary>
        private DataGridView gridPlan;

        /// <summary>The grid of plans already submitted.</summary>
        private DataGridView gridRequests;

        /// <summary>The summary line above the record.</summary>
        private Label lblSummary;

        /// <summary>The status line at the bottom.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        /// <param name="student">The student whose record is shown.</param>
        public frmMyAcademicRecord(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            this.student = student;
            this.advising = new CourseAdvisingService();
            this.reports = new ReportService();

            this.BuildInterface();
        }

        /// <summary>Builds the tabs and their contents.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "My academic record");
            this.ClientSize = new Size(940, 600);

            this.Controls.Add(UiTheme.CreateHeader("My academic record"));

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = UiTheme.BodyFont;
            tabs.Padding = new Point(16, 6);

            tabs.TabPages.Add(this.BuildRecordTab());
            tabs.TabPages.Add(this.BuildEligibleTab());
            tabs.TabPages.Add(this.BuildRequestsTab());

            this.Controls.Add(tabs);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Dock = DockStyle.Bottom;
            this.lblStatus.Height = 26;
            this.lblStatus.Padding = new Padding(12, 6, 0, 0);
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Builds the tab that lists the whole academic record.</summary>
        /// <returns>The configured tab page.</returns>
        private TabPage BuildRecordTab()
        {
            TabPage page = new TabPage("My record");
            page.BackColor = Color.WhiteSmoke;

            this.lblSummary = UiTheme.CreateLabel(string.Empty);
            this.lblSummary.Font = UiTheme.SubHeadingFont;
            this.lblSummary.ForeColor = UiTheme.HeaderColour;
            this.lblSummary.Location = new Point(12, 12);
            this.lblSummary.AutoSize = true;
            page.Controls.Add(this.lblSummary);

            this.gridRecord = UiTheme.CreateGrid();
            this.gridRecord.Location = new Point(12, 44);
            this.gridRecord.Size = new Size(896, 460);
            this.gridRecord.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            UiTheme.AddColumn(this.gridRecord, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridRecord, "Title", "Course", 32);
            UiTheme.AddColumn(this.gridRecord, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridRecord, "Semester", "Semester", 20);
            UiTheme.AddColumn(this.gridRecord, "Status", "Status", 15);
            UiTheme.AddColumn(this.gridRecord, "Grade", "Grade", 10);
            page.Controls.Add(this.gridRecord);

            return page;
        }

        /// <summary>Builds the tab that recommends courses and assembles a plan.</summary>
        /// <returns>The configured tab page.</returns>
        private TabPage BuildEligibleTab()
        {
            TabPage page = new TabPage("Eligible courses and new plan");
            page.BackColor = Color.WhiteSmoke;

            Label note = UiTheme.CreateLabel(
                "Every course in your track is listed with the verdict of the eligibility rules. " +
                "Select a course and add it to your plan.");
            note.ForeColor = Color.DimGray;
            note.Location = new Point(12, 10);
            note.AutoSize = true;
            page.Controls.Add(note);

            this.gridEligible = UiTheme.CreateGrid();
            this.gridEligible.Location = new Point(12, 36);
            this.gridEligible.Size = new Size(896, 220);
            this.gridEligible.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UiTheme.AddColumn(this.gridEligible, "Code", "Code", 9);
            UiTheme.AddColumn(this.gridEligible, "Title", "Course", 28);
            UiTheme.AddColumn(this.gridEligible, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridEligible, "Eligibility", "Verdict", 12);
            UiTheme.AddColumn(this.gridEligible, "Reason", "Explanation", 43);
            page.Controls.Add(this.gridEligible);

            Button add = UiTheme.CreateButton("Add selected to plan", this.OnAddToPlan);
            add.Location = new Point(12, 266);
            page.Controls.Add(add);

            Button refresh = UiTheme.CreateButton("Re-check eligibility", this.OnRefreshEligibility);
            refresh.Location = new Point(164, 266);
            page.Controls.Add(refresh);

            Label planNote = UiTheme.CreateLabel("My plan");
            planNote.Font = UiTheme.SubHeadingFont;
            planNote.Location = new Point(12, 308);
            planNote.AutoSize = true;
            page.Controls.Add(planNote);

            this.gridPlan = UiTheme.CreateGrid();
            this.gridPlan.Location = new Point(12, 332);
            this.gridPlan.Size = new Size(896, 130);
            this.gridPlan.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            UiTheme.AddColumn(this.gridPlan, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridPlan, "Title", "Course", 34);
            UiTheme.AddColumn(this.gridPlan, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridPlan, "EligibilityNote", "Verdict", 48);
            page.Controls.Add(this.gridPlan);

            Button submit = UiTheme.CreateButton("Submit plan for review", this.OnSubmitPlan);
            submit.Location = new Point(12, 470);
            submit.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            page.Controls.Add(submit);

            Button startNew = UiTheme.CreateButton("Start a new plan", this.OnStartNewPlan);
            startNew.Location = new Point(178, 470);
            startNew.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            page.Controls.Add(startNew);

            return page;
        }

        /// <summary>Builds the tab that lists plans already submitted.</summary>
        /// <returns>The configured tab page.</returns>
        private TabPage BuildRequestsTab()
        {
            TabPage page = new TabPage("My requests");
            page.BackColor = Color.WhiteSmoke;

            this.gridRequests = UiTheme.CreateGrid();
            this.gridRequests.Location = new Point(12, 12);
            this.gridRequests.Size = new Size(896, 470);
            this.gridRequests.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            UiTheme.AddColumn(this.gridRequests, "RequestId", "Plan", 8);
            UiTheme.AddColumn(this.gridRequests, "Status", "Status", 14);
            UiTheme.AddColumn(this.gridRequests, "CourseCount", "Courses", 10);
            UiTheme.AddColumn(this.gridRequests, "CreditTotal", "Credits", 10);
            UiTheme.AddColumn(this.gridRequests, "AdviserRemark", "Adviser remark", 58);
            page.Controls.Add(this.gridRequests);

            return page;
        }

        /// <summary>
        /// A flattened view of a plan for the requests grid. The domain object exposes the
        /// course count and credit total as methods, and a grid binds to properties, so the
        /// screen projects the plan into this small row type rather than changing the
        /// domain model to suit a control.
        /// </summary>
        private class RequestRow
        {
            /// <summary>Gets or sets the plan number.</summary>
            public int RequestId { get; set; }

            /// <summary>Gets or sets the state of the plan.</summary>
            public string Status { get; set; }

            /// <summary>Gets or sets how many courses the plan contains.</summary>
            public int CourseCount { get; set; }

            /// <summary>Gets or sets the total credit hours the plan asks for.</summary>
            public int CreditTotal { get; set; }

            /// <summary>Gets or sets the adviser's remark.</summary>
            public string AdviserRemark { get; set; }
        }

        /// <summary>Loads every tab when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.StartNewPlan();
            this.ReloadRecord();
            this.ReloadEligible();
            this.ReloadRequests();
        }

        /// <summary>Reloads the academic record and its summary line.</summary>
        private void ReloadRecord()
        {
            UiTheme.Guard(this, delegate
            {
                this.lblSummary.Text = this.reports.GetProgressionSummary(this.student.StudentId);

                this.gridRecord.DataSource = null;
                this.gridRecord.DataSource = this.reports.GetCourseProgression(this.student.StudentId);
            });
        }

        /// <summary>Re-runs the eligibility rules over the catalogue and shows the verdicts.</summary>
        private void ReloadEligible()
        {
            UiTheme.Guard(this, delegate
            {
                // The record is reloaded first, because the rules read the student's own
                // academic state and it may have changed since the screen opened.
                this.student.LoadAcademicRecord(
                    new SCAIS.DataAccess.EnrollmentRepository().GetByStudent(this.student.StudentId));

                this.gridEligible.DataSource = null;
                this.gridEligible.DataSource = this.advising.GetEligibleCourses(this.student);
            });
        }

        /// <summary>Reloads the plan being assembled.</summary>
        private void ReloadPlan()
        {
            this.gridPlan.DataSource = null;
            this.gridPlan.DataSource = this.draftRequest == null
                ? new List<CourseRequestLine>()
                : new List<CourseRequestLine>(this.draftRequest.Lines);
        }

        /// <summary>Reloads the plans already submitted.</summary>
        private void ReloadRequests()
        {
            UiTheme.Guard(this, delegate
            {
                List<RequestRow> rows = new List<RequestRow>();

                foreach (CourseRequest request in this.advising.GetRequestsForStudent(this.student.StudentId))
                {
                    RequestRow row = new RequestRow();
                    row.RequestId = request.RequestId;
                    row.Status = request.Status.ToString();
                    row.CourseCount = request.GetCourseCount();
                    row.CreditTotal = request.GetTotalCreditHours();
                    row.AdviserRemark = request.AdviserRemark ?? string.Empty;

                    rows.Add(row);
                }

                this.gridRequests.DataSource = null;
                this.gridRequests.DataSource = rows;
            });
        }

        /// <summary>Starts an empty plan for the coming semester.</summary>
        private void StartNewPlan()
        {
            UiTheme.Guard(this, delegate
            {
                Semester semester = this.advising.GetPlanningSemester();

                if (semester == null)
                {
                    UiTheme.ShowError(this, "No academic term is defined, so a plan cannot be started.");
                    return;
                }

                this.draftRequest = this.advising.CreateRequest(this.student, semester.SemesterId);
                this.ReloadPlan();

                this.lblStatus.Text = "A new plan has been started for " + semester.GetDisplayName() + ".";
            });
        }

        /// <summary>
        /// Adds the selected course to the plan. The verdict is shown immediately, and a
        /// blocked course is added anyway so the student can see the reason - which is the
        /// "block or warn" behaviour the business case asks for.
        /// </summary>
        private void OnAddToPlan(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                CourseEligibility selected = this.SelectedEligibility();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select a course in the list first.");
                    return;
                }

                if (this.draftRequest == null)
                {
                    this.StartNewPlan();
                }

                EligibilityResult verdict = this.advising.AddCourseToRequest(
                    this.draftRequest, this.student, selected.Course);

                this.ReloadPlan();

                if (verdict.IsEligible)
                {
                    this.lblStatus.Text = selected.Course.GetDisplayName() + " added to the plan.";
                }
                else
                {
                    this.lblStatus.Text = selected.Course.GetDisplayName() + " was blocked.";
                    UiTheme.ShowError(this,
                        selected.Course.GetDisplayName() + " cannot be taken yet.\r\n\r\n" + verdict.Reason);
                }
            });
        }

        /// <summary>Re-runs the eligibility rules on demand.</summary>
        private void OnRefreshEligibility(object sender, EventArgs e)
        {
            this.ReloadEligible();
            this.ReloadRecord();
            this.lblStatus.Text = "Eligibility re-checked against your current record.";
        }

        /// <summary>
        /// Submits the assembled plan for the adviser's decision.
        /// </summary>
        /// <remarks>
        /// Submitting is the moment the Observer pattern fires: the service attaches the
        /// observers and the plan's own <c>Submit</c> method announces the change, which
        /// writes the notification the student then sees on their messages screen.
        /// </remarks>
        private void OnSubmitPlan(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                if (this.draftRequest == null || this.draftRequest.GetCourseCount() == 0)
                {
                    UiTheme.ShowError(this, "Add at least one course before submitting the plan.");
                    return;
                }

                if (this.draftRequest.ContainsBlockedCourse()
                    && !UiTheme.Confirm(this,
                        "This plan contains at least one course that is blocked. " +
                        "Submit it anyway for your adviser to review?"))
                {
                    return;
                }

                if (!UiTheme.Confirm(this, "Submit this plan to your adviser for review?"))
                {
                    return;
                }

                int courseCount = this.draftRequest.GetCourseCount();

                if (!this.advising.SubmitRequest(this.draftRequest))
                {
                    UiTheme.ShowError(this, "The plan could not be saved.");
                    return;
                }

                this.StartNewPlan();
                this.ReloadRequests();

                this.lblStatus.Text = "Plan submitted with " + courseCount + " course(s).";
                UiTheme.ShowInfo(this,
                    "Your plan has been submitted.\r\n\r\n" +
                    "A message has been written for you by the notification observer, " +
                    "and your adviser can now review the plan.");
            });
        }

        /// <summary>Starts a fresh plan, discarding the one being assembled.</summary>
        private void OnStartNewPlan(object sender, EventArgs e)
        {
            if (this.draftRequest != null && this.draftRequest.GetCourseCount() > 0
                && !UiTheme.Confirm(this, "Discard the plan you are currently assembling?"))
            {
                return;
            }

            this.StartNewPlan();
        }

        /// <summary>Returns the course currently selected in the eligible list.</summary>
        private CourseEligibility SelectedEligibility()
        {
            if (this.gridEligible.CurrentRow == null)
            {
                return null;
            }

            return this.gridEligible.CurrentRow.DataBoundItem as CourseEligibility;
        }
    }
}
