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
    /// The adviser's list of advisees, their academic records and their eligibility.
    /// </summary>
    /// <remarks>
    /// The three grids on this screen cover three adviser use cases in the business case:
    /// view assigned advisees, access an advisee's academic history and specialization,
    /// and recommend courses based on eligibility. The recommendation is not a separate
    /// screen because an adviser makes the recommendation while looking at the record,
    /// and splitting the two would have meant losing that context.
    /// </remarks>
    public class frmAdviseeList : Form
    {
        /// <summary>The signed-in adviser.</summary>
        private readonly Adviser adviser;

        /// <summary>The service behind the record and the recommendations.</summary>
        private readonly CourseAdvisingService advising;

        /// <summary>The grid of assigned advisees.</summary>
        private DataGridView gridAdvisees;

        /// <summary>The grid of the selected advisee's record.</summary>
        private DataGridView gridRecord;

        /// <summary>The grid of the selected advisee's eligibility.</summary>
        private DataGridView gridEligible;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        /// <param name="adviser">The adviser whose advisees are shown.</param>
        public frmAdviseeList(Adviser adviser)
        {
            if (adviser == null)
            {
                throw new ArgumentNullException("adviser");
            }

            this.adviser = adviser;
            this.advising = new CourseAdvisingService();
            this.BuildInterface();
        }

        /// <summary>
        /// A flattened view of an advisee for the list grid, so the counts and the track
        /// name can be shown without changing the domain object.
        /// </summary>
        private class AdviseeRow
        {
            /// <summary>Gets or sets the student behind the row.</summary>
            public Student Student { get; set; }

            /// <summary>Gets the student's name.</summary>
            public string Name
            {
                get { return this.Student == null ? string.Empty : this.Student.FullName; }
            }

            /// <summary>Gets or sets the declared track.</summary>
            public string Track { get; set; }

            /// <summary>Gets the year the student enrolled.</summary>
            public int Enrolled
            {
                get { return this.Student == null ? 0 : this.Student.EnrollmentYear; }
            }

            /// <summary>Gets or sets the grade point average as text.</summary>
            public string Gpa { get; set; }

            /// <summary>Gets or sets how many courses the student has passed.</summary>
            public int Completed { get; set; }

            /// <summary>Gets or sets how many courses the student is taking.</summary>
            public int InProgress { get; set; }

            /// <summary>Gets or sets how many courses the student has yet to take.</summary>
            public int Pending { get; set; }
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "My advisees");
            this.ClientSize = new Size(940, 660);

            this.Controls.Add(UiTheme.CreateHeader("My advisees"));

            Label adviseesLabel = UiTheme.CreateLabel("Assigned advisees");
            adviseesLabel.Font = UiTheme.SubHeadingFont;
            adviseesLabel.Location = new Point(14, 60);
            adviseesLabel.AutoSize = true;
            this.Controls.Add(adviseesLabel);

            this.gridAdvisees = UiTheme.CreateGrid();
            this.gridAdvisees.Location = new Point(14, 84);
            this.gridAdvisees.Size = new Size(900, 170);
            this.gridAdvisees.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.gridAdvisees.SelectionChanged += this.OnAdviseeSelected;
            UiTheme.AddColumn(this.gridAdvisees, "Name", "Student", 26);
            UiTheme.AddColumn(this.gridAdvisees, "Track", "Track", 18);
            UiTheme.AddColumn(this.gridAdvisees, "Enrolled", "Enrolled", 10);
            UiTheme.AddColumn(this.gridAdvisees, "Gpa", "GPA", 8);
            UiTheme.AddColumn(this.gridAdvisees, "Completed", "Completed", 12);
            UiTheme.AddColumn(this.gridAdvisees, "InProgress", "In progress", 12);
            UiTheme.AddColumn(this.gridAdvisees, "Pending", "Pending", 12);
            this.Controls.Add(this.gridAdvisees);

            Label recordLabel = UiTheme.CreateLabel("Academic record of the selected advisee");
            recordLabel.Font = UiTheme.SubHeadingFont;
            recordLabel.Location = new Point(14, 264);
            recordLabel.AutoSize = true;
            this.Controls.Add(recordLabel);

            this.gridRecord = UiTheme.CreateGrid();
            this.gridRecord.Location = new Point(14, 288);
            this.gridRecord.Size = new Size(900, 140);
            this.gridRecord.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UiTheme.AddColumn(this.gridRecord, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridRecord, "Title", "Course", 32);
            UiTheme.AddColumn(this.gridRecord, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridRecord, "Semester", "Semester", 20);
            UiTheme.AddColumn(this.gridRecord, "Status", "Status", 15);
            UiTheme.AddColumn(this.gridRecord, "Grade", "Grade", 10);
            this.Controls.Add(this.gridRecord);

            Label eligibleLabel = UiTheme.CreateLabel(
                "Courses the selected advisee is eligible for (the recommendation)");
            eligibleLabel.Font = UiTheme.SubHeadingFont;
            eligibleLabel.Location = new Point(14, 438);
            eligibleLabel.AutoSize = true;
            this.Controls.Add(eligibleLabel);

            this.gridEligible = UiTheme.CreateGrid();
            this.gridEligible.Location = new Point(14, 462);
            this.gridEligible.Size = new Size(900, 140);
            this.gridEligible.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UiTheme.AddColumn(this.gridEligible, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridEligible, "Title", "Course", 30);
            UiTheme.AddColumn(this.gridEligible, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridEligible, "Eligibility", "Verdict", 12);
            UiTheme.AddColumn(this.gridEligible, "Reason", "Explanation", 40);
            this.Controls.Add(this.gridEligible);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefresh);
            refresh.Location = new Point(14, 614);
            refresh.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(refresh);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(814, 614);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(140, 620);
            this.lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblStatus.AutoSize = true;
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the advisee list when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.ReloadAdvisees();
        }

        /// <summary>Reloads the advisee list, the record and the recommendations.</summary>
        private void ReloadAdvisees()
        {
            UiTheme.Guard(this, delegate
            {
                List<AdviseeRow> rows = new List<AdviseeRow>();

                foreach (Student student in new UserRepository().GetStudentsByAdviser(this.adviser.AdviserId))
                {
                    // The record is loaded here so the counts on this screen are real
                    // rather than a second query per column.
                    student.LoadAcademicRecord(
                        new EnrollmentRepository().GetByStudent(student.StudentId));

                    AdviseeRow row = new AdviseeRow();
                    row.Student = student;
                    row.Track = student.Specialization == null ? "no track" : student.Specialization.Name;
                    row.Gpa = student.GPA.HasValue ? student.GPA.Value.ToString("0.00") : "n/a";
                    row.Completed = CountOf(student.GetCompletedCourses());
                    row.InProgress = CountOf(student.GetInProgressCourses());
                    row.Pending = CountOf(student.GetPendingCourses());

                    rows.Add(row);
                }

                this.gridAdvisees.DataSource = null;
                this.gridAdvisees.DataSource = rows;

                this.lblStatus.Text = rows.Count == 0
                    ? "No students are assigned to you yet."
                    : rows.Count + " student(s) assigned to you.";

                // Binding a list does not raise SelectionChanged reliably, so the first advisee
                // is selected and shown explicitly. Without this the screen opened with the
                // advisee list filled in and the record and the recommendations beside it empty
                // until the adviser clicked a row.
                if (rows.Count > 0)
                {
                    this.gridAdvisees.CurrentCell = this.gridAdvisees.Rows[0].Cells[0];
                    this.ShowDetailsFor(rows[0].Student);
                }
            });
        }

        /// <summary>Shows the record and the recommendations for the selected advisee.</summary>
        private void OnAdviseeSelected(object sender, EventArgs e)
        {
            this.ShowDetailsFor(this.SelectedStudent());
        }

        /// <summary>
        /// Shows one advisee's academic record and the courses that advisee is eligible for.
        /// Called both when the adviser selects a row and when the screen opens, so the two
        /// paths cannot drift apart.
        /// </summary>
        /// <param name="student">The advisee to show, or null to clear the two grids.</param>
        private void ShowDetailsFor(Student student)
        {
            this.gridRecord.DataSource = null;
            this.gridEligible.DataSource = null;

            if (student == null)
            {
                return;
            }

            UiTheme.Guard(this, delegate
            {
                student.LoadAcademicRecord(
                    new EnrollmentRepository().GetByStudent(student.StudentId));

                this.gridRecord.DataSource = new ReportService().GetCourseProgression(student.StudentId);
                this.gridEligible.DataSource = this.advising.GetEligibleCourses(student);

                this.lblStatus.Text = new ReportService().GetProgressionSummary(student.StudentId);
            });
        }

        /// <summary>Reloads everything.</summary>
        private void OnRefresh(object sender, EventArgs e)
        {
            this.ReloadAdvisees();
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>Returns the advisee currently selected in the list.</summary>
        private Student SelectedStudent()
        {
            if (this.gridAdvisees.CurrentRow == null)
            {
                return null;
            }

            AdviseeRow row = this.gridAdvisees.CurrentRow.DataBoundItem as AdviseeRow;

            return row == null ? null : row.Student;
        }

        /// <summary>Counts a sequence, used for the status totals on this screen.</summary>
        private static int CountOf(IEnumerable<Enrollment> items)
        {
            int total = 0;

            foreach (Enrollment item in items)
            {
                total++;
            }

            return total;
        }
    }
}
