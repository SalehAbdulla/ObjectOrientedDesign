using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The administrator's screen for the timetable: which course runs in which academic term,
    /// and how many places each offering has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This screen is the visible form of use case UC-07. The catalogue says what may be
    /// taught and the prerequisite rules say what may follow what, but neither of them says
    /// what actually runs this term - and a student cannot be advised to take a course that is
    /// not running. The screen maintains the <c>CourseOfferings</c> rows that answer that
    /// question, and it shows the consequence of them: how many of each offering's places are
    /// already taken, counted from the real enrolment data.
    /// </para>
    /// <para>
    /// <b>Domain rules, not screen rules.</b> The capacity range and the "an offering with
    /// students enrolled cannot be withdrawn" rule both live on
    /// <see cref="CourseOffering"/>. The screen calls <see cref="CourseOffering.HasEnrollments"/>
    /// and lets the <see cref="CourseOffering.Capacity"/> setter refuse an impossible value,
    /// so the same rules apply to any other client of the model.
    /// </para>
    /// <para>
    /// The layout is built in code, like every other screen in SCAIS, and all of its controls
    /// come from <see cref="UiTheme"/> so that it is visually indistinguishable from the
    /// catalogue screen an administrator uses immediately before it.
    /// </para>
    /// </remarks>
    public class frmCourseOfferings : Form
    {
        /// <summary>Reads and writes the timetable.</summary>
        private readonly CourseOfferingRepository offerings;

        /// <summary>Reads the catalogue for the course selector.</summary>
        private readonly CourseRepository courses;

        /// <summary>Reads the academic terms for the term selector.</summary>
        private readonly SemesterRepository semesters;

        /// <summary>The term selector.</summary>
        private ComboBox cmbSemester;

        /// <summary>The grid of offerings for the selected term.</summary>
        private DataGridView gridOfferings;

        /// <summary>The course selector of the editor.</summary>
        private ComboBox cmbCourse;

        /// <summary>The capacity editor.</summary>
        private TextBox txtCapacity;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>
        /// True while the selectors are being filled. Changing a combo box's selection raises
        /// its event, so without this the screen would reload the grid half way through
        /// building itself.
        /// </summary>
        private bool suppressSelectionEvents;

        /// <summary>Creates the screen.</summary>
        public frmCourseOfferings()
        {
            this.offerings = new CourseOfferingRepository();
            this.courses = new CourseRepository();
            this.semesters = new SemesterRepository();
            this.BuildInterface();
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Course offerings per semester");
            this.ClientSize = new Size(980, 660);

            this.Controls.Add(UiTheme.CreateHeader("Course offerings per semester"));

            // Everything else lives inside one filling panel, which keeps the docking order
            // trivial: the status line takes the bottom strip and the panel takes the rest.
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.WhiteSmoke;
            this.Controls.Add(content);

            Label semesterCaption = UiTheme.CreateLabel("Semester");
            semesterCaption.Font = UiTheme.SubHeadingFont;
            semesterCaption.Location = new Point(12, 16);
            semesterCaption.AutoSize = true;
            content.Controls.Add(semesterCaption);

            this.cmbSemester = new ComboBox();
            this.cmbSemester.Font = UiTheme.BodyFont;
            this.cmbSemester.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSemester.Location = new Point(96, 12);
            this.cmbSemester.Width = 250;
            this.cmbSemester.SelectedIndexChanged += this.OnSemesterChanged;
            content.Controls.Add(this.cmbSemester);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefresh);
            refresh.Location = new Point(358, 9);
            content.Controls.Add(refresh);

            Button copy = UiTheme.CreateButton("Copy the current term into this term", this.OnCopyCurrentTerm);
            copy.Location = new Point(496, 9);
            content.Controls.Add(copy);

            Label hint = UiTheme.CreateLabel(
                "The timetable the advising screens read. An offering cannot be removed while students are enrolled in it.");
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(12, 46);
            hint.AutoSize = true;
            content.Controls.Add(hint);

            content.Controls.Add(this.BuildGrid());
            content.Controls.Add(this.BuildEditor());

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Dock = DockStyle.Bottom;
            this.lblStatus.Height = 26;
            this.lblStatus.Padding = new Padding(12, 6, 0, 0);
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Builds the grid of offerings for the selected term.</summary>
        /// <returns>The configured grid.</returns>
        private DataGridView BuildGrid()
        {
            this.gridOfferings = UiTheme.CreateGrid();
            this.gridOfferings.Location = new Point(12, 72);
            this.gridOfferings.Size = new Size(956, 350);
            this.gridOfferings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.gridOfferings.SelectionChanged += this.OnOfferingSelected;
            UiTheme.AddColumn(this.gridOfferings, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridOfferings, "Title", "Course", 32);
            UiTheme.AddColumn(this.gridOfferings, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridOfferings, "TrackName", "Track", 18);
            UiTheme.AddColumn(this.gridOfferings, "Capacity", "Places", 9);
            UiTheme.AddColumn(this.gridOfferings, "Enrolled", "Taken", 9);
            UiTheme.AddColumn(this.gridOfferings, "SeatsFree", "Free", 9);

            return this.gridOfferings;
        }

        /// <summary>Builds the editor that adds an offering or changes its places.</summary>
        /// <returns>The configured panel.</returns>
        private Panel BuildEditor()
        {
            Panel editor = new Panel();
            editor.Location = new Point(12, 432);
            editor.Size = new Size(956, 150);
            editor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label caption = UiTheme.CreateLabel("Add an offering, or change the places of the selected one");
            caption.Font = UiTheme.SubHeadingFont;
            caption.Location = new Point(0, 0);
            caption.AutoSize = true;
            editor.Controls.Add(caption);

            this.cmbCourse = new ComboBox();
            this.cmbCourse.Font = UiTheme.BodyFont;
            this.cmbCourse.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbCourse.Location = new Point(0, 30);
            this.cmbCourse.Width = 420;
            editor.Controls.Add(this.cmbCourse);

            Label capacityCaption = UiTheme.CreateLabel("Places");
            capacityCaption.Location = new Point(436, 34);
            capacityCaption.AutoSize = true;
            editor.Controls.Add(capacityCaption);

            this.txtCapacity = UiTheme.CreateTextBox(70);
            this.txtCapacity.Location = new Point(486, 30);
            this.txtCapacity.Text = CourseOffering.DefaultCapacity.ToString();
            editor.Controls.Add(this.txtCapacity);

            Label hint = UiTheme.CreateLabel(
                "course | places (1-" + CourseOffering.MaximumCapacity + "). Leave blank to use " +
                CourseOffering.DefaultCapacity + ".");
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(0, 60);
            hint.AutoSize = true;
            editor.Controls.Add(hint);

            Button add = UiTheme.CreateButton("Add offering", this.OnAddOffering);
            add.Location = new Point(0, 90);
            editor.Controls.Add(add);

            Button update = UiTheme.CreateButton("Update places", this.OnUpdateOffering);
            update.Location = new Point(152, 90);
            editor.Controls.Add(update);

            Button delete = UiTheme.CreateButton("Remove offering", this.OnDeleteOffering);
            delete.Location = new Point(304, 90);
            editor.Controls.Add(delete);

            return editor;
        }

        /// <summary>A flattened view of one offering for the grid.</summary>
        private class OfferingRow
        {
            /// <summary>Gets or sets the offering behind the row.</summary>
            public CourseOffering Offering { get; set; }

            /// <summary>Gets the catalogue code.</summary>
            public string Code
            {
                get { return this.Offering == null ? string.Empty : this.Offering.Course.Code; }
            }

            /// <summary>Gets the title.</summary>
            public string Title
            {
                get { return this.Offering == null ? string.Empty : this.Offering.Course.Title; }
            }

            /// <summary>Gets the credit hours.</summary>
            public int CreditHours
            {
                get { return this.Offering == null ? 0 : this.Offering.Course.CreditHours; }
            }

            /// <summary>Gets the track name, or "core" for a core subject.</summary>
            public string TrackName
            {
                get
                {
                    if (this.Offering == null)
                    {
                        return string.Empty;
                    }

                    return this.Offering.Course.Specialization == null
                        ? "core"
                        : this.Offering.Course.Specialization.Name;
                }
            }

            /// <summary>Gets the number of places the offering has.</summary>
            public int Capacity
            {
                get { return this.Offering == null ? 0 : this.Offering.Capacity; }
            }

            /// <summary>Gets the number of places already taken.</summary>
            public int Enrolled
            {
                get { return this.Offering == null ? 0 : this.Offering.Enrolled; }
            }

            /// <summary>Gets the number of places still free.</summary>
            public int SeatsFree
            {
                get { return this.Offering == null ? 0 : this.Offering.SeatsRemaining; }
            }
        }

        /// <summary>Fills the selectors and shows the first term when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.LoadSelectors();
            this.ReloadOfferings();
        }

        /// <summary>Fills the term and course selectors.</summary>
        private void LoadSelectors()
        {
            UiTheme.Guard(this, delegate
            {
                this.suppressSelectionEvents = true;

                this.cmbSemester.Items.Clear();

                foreach (Semester semester in this.semesters.GetAll())
                {
                    this.cmbSemester.Items.Add(semester);
                }

                // Open on the term a student would be planning for, so that the administrator
                // sees the timetable that matters rather than the oldest row in the table.
                this.SelectSemester(this.semesters.GetNext());

                this.cmbCourse.Items.Clear();

                foreach (Course course in this.courses.GetAll())
                {
                    this.cmbCourse.Items.Add(course);
                }

                if (this.cmbCourse.Items.Count > 0)
                {
                    this.cmbCourse.SelectedIndex = 0;
                }

                this.suppressSelectionEvents = false;
            });
        }

        /// <summary>Reloads the grid for the selected term.</summary>
        private void ReloadOfferings()
        {
            UiTheme.Guard(this, delegate
            {
                Semester semester = this.SelectedSemester();

                if (semester == null)
                {
                    this.gridOfferings.DataSource = null;
                    this.lblStatus.Text = "Choose a semester.";
                    return;
                }

                List<OfferingRow> rows = new List<OfferingRow>();
                int freePlaces = 0;

                foreach (CourseOffering offering in this.offerings.GetBySemester(semester.SemesterId))
                {
                    OfferingRow row = new OfferingRow();
                    row.Offering = offering;
                    rows.Add(row);

                    freePlaces += offering.SeatsRemaining;
                }

                this.gridOfferings.DataSource = null;
                this.gridOfferings.DataSource = rows;

                this.lblStatus.Text = string.Format(
                    "{0} offering(s) in {1}; {2} place(s) still free.",
                    rows.Count, semester.GetDisplayName(), freePlaces);
            });
        }

        /// <summary>Reloads the grid when the administrator chooses another term.</summary>
        private void OnSemesterChanged(object sender, EventArgs e)
        {
            if (this.suppressSelectionEvents)
            {
                return;
            }

            this.ReloadOfferings();
        }

        /// <summary>Reloads the grid on demand.</summary>
        private void OnRefresh(object sender, EventArgs e)
        {
            this.ReloadOfferings();
        }

        /// <summary>Copies the selected offering into the editor.</summary>
        private void OnOfferingSelected(object sender, EventArgs e)
        {
            CourseOffering offering = this.SelectedOffering();

            if (offering == null)
            {
                return;
            }

            this.SelectCourse(offering.Course);
            this.txtCapacity.Text = offering.Capacity.ToString();
        }

        /// <summary>Returns the offering selected in the grid.</summary>
        /// <returns>The selected offering, or null when no row is selected.</returns>
        private CourseOffering SelectedOffering()
        {
            if (this.gridOfferings.CurrentRow == null)
            {
                return null;
            }

            OfferingRow row = this.gridOfferings.CurrentRow.DataBoundItem as OfferingRow;

            return row == null ? null : row.Offering;
        }

        /// <summary>Returns the term chosen in the selector.</summary>
        /// <returns>The selected semester, or null.</returns>
        private Semester SelectedSemester()
        {
            return this.cmbSemester.SelectedItem as Semester;
        }

        /// <summary>
        /// Selects a term by its key. The comparison is by key rather than by object because the
        /// selector holds its own <see cref="Semester"/> instances, loaded separately from the
        /// database, so a reference comparison would never match.
        /// </summary>
        /// <param name="semester">The term to select, or null for the first one.</param>
        private void SelectSemester(Semester semester)
        {
            for (int index = 0; index < this.cmbSemester.Items.Count; index++)
            {
                Semester candidate = this.cmbSemester.Items[index] as Semester;

                if (semester != null && candidate != null && candidate.SemesterId == semester.SemesterId)
                {
                    this.cmbSemester.SelectedIndex = index;
                    return;
                }
            }

            if (this.cmbSemester.Items.Count > 0)
            {
                this.cmbSemester.SelectedIndex = 0;
            }
        }

        /// <summary>Selects a course in the editor by its key.</summary>
        /// <param name="course">The course to select.</param>
        private void SelectCourse(Course course)
        {
            if (course == null)
            {
                return;
            }

            for (int index = 0; index < this.cmbCourse.Items.Count; index++)
            {
                Course candidate = this.cmbCourse.Items[index] as Course;

                if (candidate != null && candidate.CourseId == course.CourseId)
                {
                    this.cmbCourse.SelectedIndex = index;
                    return;
                }
            }
        }

        /// <summary>
        /// Reads the number of places from the editor. A blank box means the default capacity
        /// rather than an error, because "the usual size" is what an administrator almost always
        /// wants; the range check is still applied so the domain never sees an impossible value.
        /// </summary>
        /// <param name="capacity">The capacity that was read.</param>
        /// <returns>True when the editor holds a usable value.</returns>
        private bool TryReadCapacity(out int capacity)
        {
            capacity = CourseOffering.DefaultCapacity;
            string text = this.txtCapacity.Text.Trim();

            if (text.Length == 0)
            {
                return true;
            }

            if (!int.TryParse(text, out capacity) ||
                capacity < CourseOffering.MinimumCapacity ||
                capacity > CourseOffering.MaximumCapacity)
            {
                UiTheme.ShowError(this, "Places must be a whole number between " +
                    CourseOffering.MinimumCapacity + " and " + CourseOffering.MaximumCapacity + ".");

                capacity = CourseOffering.DefaultCapacity;
                return false;
            }

            return true;
        }

        /// <summary>Adds an offering of the chosen course in the chosen term.</summary>
        private void OnAddOffering(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Semester semester = this.SelectedSemester();
                Course course = this.cmbCourse.SelectedItem as Course;

                if (semester == null || course == null)
                {
                    UiTheme.ShowError(this, "Choose a semester and a course first.");
                    return;
                }

                int capacity;

                if (!this.TryReadCapacity(out capacity))
                {
                    return;
                }

                // The database enforces this too, through UQ_CourseOfferings. Checking first
                // means the administrator is told what is wrong instead of being shown a
                // constraint violation raised by SQL Server.
                if (this.offerings.Exists(course.CourseId, semester.SemesterId, 0))
                {
                    UiTheme.ShowError(this, course.GetDisplayName() +
                        " is already offered in " + semester.GetDisplayName() + ".");
                    return;
                }

                CourseOffering offering = new CourseOffering(0, course, semester, capacity);

                if (!this.offerings.Add(offering))
                {
                    UiTheme.ShowError(this, "The offering could not be added.");
                    return;
                }

                this.ReloadOfferings();
                UiTheme.ShowInfo(this, offering.GetDisplayName() + " has been added with " +
                    capacity + " place(s).");
            });
        }

        /// <summary>Changes the number of places of the selected offering.</summary>
        private void OnUpdateOffering(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                CourseOffering offering = this.SelectedOffering();

                if (offering == null)
                {
                    UiTheme.ShowError(this, "Select an offering in the grid first.");
                    return;
                }

                int capacity;

                if (!this.TryReadCapacity(out capacity))
                {
                    return;
                }

                // Shrinking an offering below the number of students already enrolled would
                // withdraw places that have already been promised.
                if (capacity < offering.Enrolled)
                {
                    UiTheme.ShowError(this, capacity + " place(s) cannot cover the " +
                        offering.Enrolled + " student(s) already enrolled in this course this term.");
                    return;
                }

                offering.Capacity = capacity;

                if (!this.offerings.Update(offering))
                {
                    UiTheme.ShowError(this, "The offering could not be updated.");
                    return;
                }

                this.ReloadOfferings();
                UiTheme.ShowInfo(this, offering.GetDisplayName() + " now has " +
                    offering.GetAvailabilityText() + ".");
            });
        }

        /// <summary>Removes the selected offering from the timetable.</summary>
        private void OnDeleteOffering(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                CourseOffering offering = this.SelectedOffering();

                if (offering == null)
                {
                    UiTheme.ShowError(this, "Select an offering in the grid first.");
                    return;
                }

                // The rule is asked of the object rather than decided here: withdrawing an
                // offering that students are already enrolled in would leave their records
                // pointing at a course that is no longer running.
                if (offering.HasEnrollments())
                {
                    UiTheme.ShowError(this, offering.GetDisplayName() + " cannot be removed: " +
                        offering.Enrolled + " student(s) are already enrolled in it this term.");
                    return;
                }

                if (!UiTheme.Confirm(this, "Remove " + offering.GetDisplayName() +
                    " from the timetable?"))
                {
                    return;
                }

                if (!this.offerings.Delete(offering.OfferingId))
                {
                    UiTheme.ShowError(this, "The offering could not be removed.");
                    return;
                }

                this.ReloadOfferings();
                UiTheme.ShowInfo(this, "The offering has been removed.");
            });
        }

        /// <summary>
        /// Copies the current term's offerings into the selected term, so that a timetable does
        /// not have to be entered from nothing every term.
        /// </summary>
        private void OnCopyCurrentTerm(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Semester target = this.SelectedSemester();

                if (target == null)
                {
                    UiTheme.ShowError(this, "Choose the semester to copy into first.");
                    return;
                }

                Semester source = this.semesters.GetCurrent();

                if (source == null)
                {
                    UiTheme.ShowError(this, "No term in the calendar covers today, so there is " +
                        "nothing to copy from.");
                    return;
                }

                if (source.SemesterId == target.SemesterId)
                {
                    UiTheme.ShowError(this, source.GetDisplayName() +
                        " is already the term shown, so there is nothing to copy.");
                    return;
                }

                if (!UiTheme.Confirm(this, "Copy every offering of " + source.GetDisplayName() +
                    " that is not already in " + target.GetDisplayName() + "?"))
                {
                    return;
                }

                int added = this.offerings.CopyFrom(source.SemesterId, target.SemesterId);

                this.ReloadOfferings();

                UiTheme.ShowInfo(this, added == 0
                    ? "Nothing was copied: " + target.GetDisplayName() +
                      " already has every offering of " + source.GetDisplayName() + "."
                    : added + " offering(s) copied into " + target.GetDisplayName() + ".");
            });
        }
    }
}
