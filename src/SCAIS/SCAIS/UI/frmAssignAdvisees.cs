using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The administrator's screen for assigning students to advisers.
    /// </summary>
    /// <remarks>
    /// The assignment is the aggregation shown on the class diagram:
    /// <c>Adviser 1 -- 0..* Student</c>. It is an aggregation rather than a composition
    /// because a student exists independently of their adviser and the assignment can be
    /// changed or cleared at any time, which is exactly what this screen does.
    /// </remarks>
    public class frmAssignAdvisees : Form
    {
        /// <summary>Reads accounts and performs the assignment.</summary>
        private readonly UserRepository users;

        /// <summary>The grid of students.</summary>
        private DataGridView gridStudents;

        /// <summary>The adviser selector.</summary>
        private ComboBox cmbAdvisers;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        public frmAssignAdvisees()
        {
            this.users = new UserRepository();
            this.BuildInterface();
        }

        /// <summary>
        /// A flattened view of a student for the grid, so the adviser's name can be shown
        /// without changing the domain object.
        /// </summary>
        private class StudentRow
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

            /// <summary>Gets or sets the assigned adviser's name, or a dash when unassigned.</summary>
            public string Adviser { get; set; }
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Assign advisees to advisers");
            this.ClientSize = new Size(900, 560);

            this.Controls.Add(UiTheme.CreateHeader("Assign advisees to advisers"));

            this.gridStudents = UiTheme.CreateGrid();
            this.gridStudents.Location = new Point(14, 60);
            this.gridStudents.Size = new Size(860, 380);
            this.gridStudents.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            UiTheme.AddColumn(this.gridStudents, "Name", "Student", 36);
            UiTheme.AddColumn(this.gridStudents, "Track", "Track", 26);
            UiTheme.AddColumn(this.gridStudents, "Adviser", "Adviser", 38);
            this.Controls.Add(this.gridStudents);

            Label adviserLabel = UiTheme.CreateLabel("Assign the selected student to");
            adviserLabel.Location = new Point(14, 454);
            adviserLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            adviserLabel.AutoSize = true;
            this.Controls.Add(adviserLabel);

            this.cmbAdvisers = new ComboBox();
            this.cmbAdvisers.Font = UiTheme.BodyFont;
            this.cmbAdvisers.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbAdvisers.Location = new Point(14, 476);
            this.cmbAdvisers.Width = 340;
            this.cmbAdvisers.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(this.cmbAdvisers);

            Button assign = UiTheme.CreateButton("Assign", this.OnAssign);
            assign.Location = new Point(368, 474);
            assign.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(assign);

            Button clear = UiTheme.CreateButton("Clear assignment", this.OnClear);
            clear.Location = new Point(520, 474);
            clear.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(clear);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefresh);
            refresh.Location = new Point(672, 474);
            refresh.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(refresh);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(774, 514);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(14, 518);
            this.lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblStatus.AutoSize = true;
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the students and the advisers when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.Reload();
        }

        /// <summary>Reloads the student list and the adviser selector.</summary>
        private void Reload()
        {
            UiTheme.Guard(this, delegate
            {
                List<Adviser> advisers = this.users.GetAdvisers();
                Dictionary<int, string> adviserNames = new Dictionary<int, string>();

                this.cmbAdvisers.Items.Clear();
                this.cmbAdvisers.Items.Add("(no adviser)");

                foreach (Adviser adviser in advisers)
                {
                    adviserNames[adviser.AdviserId] = adviser.FullName;
                    this.cmbAdvisers.Items.Add(adviser);
                }

                this.cmbAdvisers.SelectedIndex = 0;

                List<StudentRow> rows = new List<StudentRow>();
                int unassigned = 0;

                foreach (Student student in this.users.GetStudents())
                {
                    StudentRow row = new StudentRow();
                    row.Student = student;
                    row.Track = student.Specialization == null ? "no track" : student.Specialization.Name;

                    // The assignment is read from the database, because the student object
                    // holds a name only when an adviser was joined in.
                    int? adviserId = this.users.GetAdviserIdForStudent(student.StudentId);

                    if (adviserId.HasValue && adviserNames.ContainsKey(adviserId.Value))
                    {
                        row.Adviser = adviserNames[adviserId.Value];
                    }
                    else
                    {
                        row.Adviser = "(none)";
                        unassigned++;
                    }

                    rows.Add(row);
                }

                this.gridStudents.DataSource = null;
                this.gridStudents.DataSource = rows;

                this.lblStatus.Text = string.Format(
                    "{0} student(s), {1} adviser(s), {2} student(s) without an adviser.",
                    rows.Count, advisers.Count, unassigned);
            });
        }

        /// <summary>Assigns the selected student to the chosen adviser.</summary>
        private void OnAssign(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Student student = this.SelectedStudent();
                Adviser adviser = this.cmbAdvisers.SelectedItem as Adviser;

                if (student == null)
                {
                    UiTheme.ShowError(this, "Select a student in the list first.");
                    return;
                }

                if (adviser == null)
                {
                    UiTheme.ShowError(this,
                        "Choose an adviser. Use Clear assignment to remove an adviser instead.");
                    return;
                }

                if (!this.users.SetStudentAdviser(student.StudentId, adviser.AdviserId))
                {
                    UiTheme.ShowError(this, "The assignment could not be saved.");
                    return;
                }

                this.Reload();
                UiTheme.ShowInfo(this, student.FullName + " has been assigned to " + adviser.FullName + ".");
            });
        }

        /// <summary>Clears the selected student's adviser.</summary>
        private void OnClear(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Student student = this.SelectedStudent();

                if (student == null)
                {
                    UiTheme.ShowError(this, "Select a student in the list first.");
                    return;
                }

                if (!UiTheme.Confirm(this, "Remove the adviser assignment for " + student.FullName + "?"))
                {
                    return;
                }

                if (!this.users.SetStudentAdviser(student.StudentId, null))
                {
                    UiTheme.ShowError(this, "The assignment could not be cleared.");
                    return;
                }

                this.Reload();
                UiTheme.ShowInfo(this, student.FullName + " now has no adviser.");
            });
        }

        /// <summary>Reloads the list.</summary>
        private void OnRefresh(object sender, EventArgs e)
        {
            this.Reload();
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>Returns the student currently selected in the list.</summary>
        private Student SelectedStudent()
        {
            if (this.gridStudents.CurrentRow == null)
            {
                return null;
            }

            StudentRow row = this.gridStudents.CurrentRow.DataBoundItem as StudentRow;

            return row == null ? null : row.Student;
        }
    }
}
