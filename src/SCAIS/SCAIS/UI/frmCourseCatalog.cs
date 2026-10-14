using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The administrator's screen for the course catalogue and the prerequisite rules.
    /// </summary>
    /// <remarks>
    /// This is where the curriculum that the eligibility rules enforce is maintained.
    /// Everything the rules later refuse a student for was configured here. Both tabs write
    /// through <see cref="IRepository{T}"/> implementations, and the course tab
    /// demonstrates domain validation: a credit hour value outside the permitted range is
    /// refused by <see cref="Course.CreditHours"/> before any SQL is issued.
    /// </remarks>
    public class frmCourseCatalog : Form
    {
        /// <summary>Reads and writes courses.</summary>
        private readonly CourseRepository courses;

        /// <summary>Reads and writes the curriculum rules.</summary>
        private readonly PrerequisiteRepository prerequisites;

        /// <summary>Reads the tracks for the selectors.</summary>
        private readonly SpecializationRepository specializations;

        /// <summary>The grid of courses.</summary>
        private DataGridView gridCourses;

        /// <summary>The grid of curriculum rules.</summary>
        private DataGridView gridRules;

        /// <summary>The course code editor.</summary>
        private TextBox txtCode;

        /// <summary>The course title editor.</summary>
        private TextBox txtTitle;

        /// <summary>The credit hours editor.</summary>
        private TextBox txtCredits;

        /// <summary>The core subject flag.</summary>
        private CheckBox chkCore;

        /// <summary>The track selector for a course.</summary>
        private ComboBox cmbSpecialization;

        /// <summary>The dependent course selector for a rule.</summary>
        private ComboBox cmbRuleCourse;

        /// <summary>The required course selector for a rule.</summary>
        private ComboBox cmbRuleRequired;

        /// <summary>The rule type selector.</summary>
        private ComboBox cmbRuleType;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>Creates the screen.</summary>
        public frmCourseCatalog()
        {
            this.courses = new CourseRepository();
            this.prerequisites = new PrerequisiteRepository();
            this.specializations = new SpecializationRepository();
            this.BuildInterface();
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Course catalogue and prerequisite rules");
            this.ClientSize = new Size(960, 640);

            this.Controls.Add(UiTheme.CreateHeader("Course catalogue and prerequisite rules"));

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = UiTheme.BodyFont;
            tabs.Padding = new Point(16, 6);
            tabs.TabPages.Add(this.BuildCoursesTab());
            tabs.TabPages.Add(this.BuildRulesTab());
            this.Controls.Add(tabs);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Dock = DockStyle.Bottom;
            this.lblStatus.Height = 26;
            this.lblStatus.Padding = new Padding(12, 6, 0, 0);
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Builds the tab that maintains the course catalogue.</summary>
        /// <returns>The configured tab page.</returns>
        private TabPage BuildCoursesTab()
        {
            TabPage page = new TabPage("Courses");
            page.BackColor = Color.WhiteSmoke;

            this.gridCourses = UiTheme.CreateGrid();
            this.gridCourses.Location = new Point(12, 12);
            this.gridCourses.Size = new Size(920, 330);
            this.gridCourses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.gridCourses.SelectionChanged += this.OnCourseSelected;
            UiTheme.AddColumn(this.gridCourses, "Code", "Code", 10);
            UiTheme.AddColumn(this.gridCourses, "Title", "Course", 38);
            UiTheme.AddColumn(this.gridCourses, "CreditHours", "Credits", 8);
            UiTheme.AddColumn(this.gridCourses, "IsCore", "Core", 7);
            UiTheme.AddColumn(this.gridCourses, "TrackName", "Track", 24);
            page.Controls.Add(this.gridCourses);

            Label editor = UiTheme.CreateLabel("Add or update a course");
            editor.Font = UiTheme.SubHeadingFont;
            editor.Location = new Point(12, 356);
            editor.AutoSize = true;
            page.Controls.Add(editor);

            this.txtCode = UiTheme.CreateTextBox(90);
            this.txtCode.Location = new Point(12, 384);
            page.Controls.Add(this.txtCode);

            this.txtTitle = UiTheme.CreateTextBox(280);
            this.txtTitle.Location = new Point(112, 384);
            page.Controls.Add(this.txtTitle);

            this.txtCredits = UiTheme.CreateTextBox(50);
            this.txtCredits.Location = new Point(402, 384);
            page.Controls.Add(this.txtCredits);

            this.chkCore = new CheckBox();
            this.chkCore.Text = "Core subject";
            this.chkCore.Font = UiTheme.BodyFont;
            this.chkCore.Location = new Point(462, 386);
            this.chkCore.AutoSize = true;
            page.Controls.Add(this.chkCore);

            this.cmbSpecialization = new ComboBox();
            this.cmbSpecialization.Font = UiTheme.BodyFont;
            this.cmbSpecialization.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSpecialization.Location = new Point(600, 384);
            this.cmbSpecialization.Width = 220;
            page.Controls.Add(this.cmbSpecialization);

            Label hint = UiTheme.CreateLabel("code | title | credits (1-6) | core | track");
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(12, 414);
            hint.AutoSize = true;
            page.Controls.Add(hint);

            Button add = UiTheme.CreateButton("Add course", this.OnAddCourse);
            add.Location = new Point(12, 444);
            page.Controls.Add(add);

            Button update = UiTheme.CreateButton("Update course", this.OnUpdateCourse);
            update.Location = new Point(164, 444);
            page.Controls.Add(update);

            Button delete = UiTheme.CreateButton("Delete course", this.OnDeleteCourse);
            delete.Location = new Point(316, 444);
            page.Controls.Add(delete);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefreshCourses);
            refresh.Location = new Point(468, 444);
            page.Controls.Add(refresh);

            return page;
        }

        /// <summary>Builds the tab that maintains the prerequisite rules.</summary>
        /// <returns>The configured tab page.</returns>
        private TabPage BuildRulesTab()
        {
            TabPage page = new TabPage("Prerequisite and corequisite rules");
            page.BackColor = Color.WhiteSmoke;

            Label note = UiTheme.CreateLabel(
                "A rule states that one course depends on another. Prerequisite means it must be " +
                "passed first; corequisite means it may be taken at the same time.");
            note.ForeColor = Color.DimGray;
            note.Location = new Point(12, 10);
            note.MaximumSize = new Size(900, 34);
            page.Controls.Add(note);

            this.gridRules = UiTheme.CreateGrid();
            this.gridRules.Location = new Point(12, 50);
            this.gridRules.Size = new Size(920, 300);
            this.gridRules.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            UiTheme.AddColumn(this.gridRules, "DependentCourse", "Course", 34);
            UiTheme.AddColumn(this.gridRules, "RequiredCourse", "Requires", 34);
            UiTheme.AddColumn(this.gridRules, "RuleType", "Rule type", 16);
            UiTheme.AddColumn(this.gridRules, "PrerequisiteId", "Rule", 8);
            page.Controls.Add(this.gridRules);

            Label editor = UiTheme.CreateLabel("Add a rule");
            editor.Font = UiTheme.SubHeadingFont;
            editor.Location = new Point(12, 366);
            editor.AutoSize = true;
            page.Controls.Add(editor);

            this.cmbRuleCourse = new ComboBox();
            this.cmbRuleCourse.Font = UiTheme.BodyFont;
            this.cmbRuleCourse.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRuleCourse.Location = new Point(12, 394);
            this.cmbRuleCourse.Width = 300;
            page.Controls.Add(this.cmbRuleCourse);

            this.cmbRuleRequired = new ComboBox();
            this.cmbRuleRequired.Font = UiTheme.BodyFont;
            this.cmbRuleRequired.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRuleRequired.Location = new Point(322, 394);
            this.cmbRuleRequired.Width = 300;
            page.Controls.Add(this.cmbRuleRequired);

            this.cmbRuleType = new ComboBox();
            this.cmbRuleType.Font = UiTheme.BodyFont;
            this.cmbRuleType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRuleType.Location = new Point(632, 394);
            this.cmbRuleType.Width = 180;
            this.cmbRuleType.Items.Add(RuleType.Prerequisite);
            this.cmbRuleType.Items.Add(RuleType.Corequisite);
            this.cmbRuleType.SelectedIndex = 0;
            page.Controls.Add(this.cmbRuleType);

            Label hint = UiTheme.CreateLabel("course | requires | rule type");
            hint.ForeColor = Color.DimGray;
            hint.Location = new Point(12, 424);
            hint.AutoSize = true;
            page.Controls.Add(hint);

            Button add = UiTheme.CreateButton("Add rule", this.OnAddRule);
            add.Location = new Point(12, 454);
            page.Controls.Add(add);

            Button delete = UiTheme.CreateButton("Delete rule", this.OnDeleteRule);
            delete.Location = new Point(164, 454);
            page.Controls.Add(delete);

            Button refresh = UiTheme.CreateButton("Refresh", this.OnRefreshRules);
            refresh.Location = new Point(316, 454);
            page.Controls.Add(refresh);

            return page;
        }

        /// <summary>
        /// A flattened view of a course for the grid. The track name lives on the related
        /// object, and a grid binds to a property, so the screen projects the course into
        /// this small row type rather than changing the domain model to suit a control.
        /// </summary>
        private class CourseRow
        {
            /// <summary>Gets or sets the course behind the row.</summary>
            public Course Course { get; set; }

            /// <summary>Gets the catalogue code.</summary>
            public string Code { get { return this.Course == null ? string.Empty : this.Course.Code; } }

            /// <summary>Gets the title.</summary>
            public string Title { get { return this.Course == null ? string.Empty : this.Course.Title; } }

            /// <summary>Gets the credit hours.</summary>
            public int CreditHours { get { return this.Course == null ? 0 : this.Course.CreditHours; } }

            /// <summary>Gets whether the course is a core subject.</summary>
            public bool IsCore { get { return this.Course != null && this.Course.IsCore; } }

            /// <summary>Gets the track name, or "core" for a core subject.</summary>
            public string TrackName
            {
                get
                {
                    if (this.Course == null)
                    {
                        return string.Empty;
                    }

                    return this.Course.Specialization == null ? "core" : this.Course.Specialization.Name;
                }
            }
        }

        /// <summary>A flattened view of a curriculum rule for the rules grid.</summary>
        private class RuleRow
        {
            /// <summary>Gets or sets the rule behind the row.</summary>
            public Prerequisite Rule { get; set; }

            /// <summary>Gets the dependent course.</summary>
            public string DependentCourse
            {
                get { return this.Rule == null ? string.Empty : this.Rule.Course.GetDisplayName(); }
            }

            /// <summary>Gets the required course.</summary>
            public string RequiredCourse
            {
                get { return this.Rule == null ? string.Empty : this.Rule.RequiredCourse.GetDisplayName(); }
            }

            /// <summary>Gets the rule type.</summary>
            public string RuleType
            {
                get { return this.Rule == null ? string.Empty : this.Rule.RuleType.ToString(); }
            }

            /// <summary>Gets the rule number.</summary>
            public int PrerequisiteId
            {
                get { return this.Rule == null ? 0 : this.Rule.PrerequisiteId; }
            }
        }

        /// <summary>Loads both tabs when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            this.LoadSelectors();
            this.ReloadCourses();
            this.ReloadRules();
        }

        /// <summary>Fills the specialization and course selectors.</summary>
        private void LoadSelectors()
        {
            UiTheme.Guard(this, delegate
            {
                this.cmbSpecialization.Items.Clear();
                this.cmbSpecialization.Items.Add("(core - every track)");

                foreach (Specialization track in this.specializations.GetAll())
                {
                    this.cmbSpecialization.Items.Add(track);
                }

                this.cmbSpecialization.SelectedIndex = 0;

                List<Course> all = this.courses.GetAll();

                this.cmbRuleCourse.Items.Clear();
                this.cmbRuleRequired.Items.Clear();

                foreach (Course course in all)
                {
                    this.cmbRuleCourse.Items.Add(course);
                    this.cmbRuleRequired.Items.Add(course);
                }

                if (this.cmbRuleCourse.Items.Count > 0)
                {
                    this.cmbRuleCourse.SelectedIndex = 0;
                    this.cmbRuleRequired.SelectedIndex = 0;
                }
            });
        }

        /// <summary>Reloads the course grid.</summary>
        private void ReloadCourses()
        {
            UiTheme.Guard(this, delegate
            {
                List<CourseRow> rows = new List<CourseRow>();

                foreach (Course course in this.courses.GetAll())
                {
                    CourseRow row = new CourseRow();
                    row.Course = course;
                    rows.Add(row);
                }

                this.gridCourses.DataSource = null;
                this.gridCourses.DataSource = rows;

                this.lblStatus.Text = rows.Count + " course(s) in the catalogue.";
            });
        }

        /// <summary>Reloads the rules grid.</summary>
        private void ReloadRules()
        {
            UiTheme.Guard(this, delegate
            {
                List<RuleRow> rows = new List<RuleRow>();

                foreach (Prerequisite rule in this.prerequisites.GetAll())
                {
                    RuleRow row = new RuleRow();
                    row.Rule = rule;
                    rows.Add(row);
                }

                this.gridRules.DataSource = null;
                this.gridRules.DataSource = rows;

                this.lblStatus.Text = rows.Count + " curriculum rule(s) defined.";
            });
        }

        /// <summary>Copies the selected course into the editor fields.</summary>
        private void OnCourseSelected(object sender, EventArgs e)
        {
            Course course = this.SelectedCourse();

            if (course == null)
            {
                return;
            }

            this.txtCode.Text = course.Code;
            this.txtTitle.Text = course.Title;
            this.txtCredits.Text = course.CreditHours.ToString();
            this.chkCore.Checked = course.IsCore;

            if (course.Specialization == null)
            {
                this.cmbSpecialization.SelectedIndex = 0;
            }
            else
            {
                this.cmbSpecialization.SelectedItem = course.Specialization;
            }
        }

        /// <summary>Adds a course from the editor fields.</summary>
        private void OnAddCourse(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Course course = this.ReadCourseFromEditor(0);

                if (course == null)
                {
                    return;
                }

                if (this.courses.CodeExists(course.Code, 0))
                {
                    UiTheme.ShowError(this, "The catalogue code " + course.Code + " is already used.");
                    return;
                }

                if (!this.courses.Add(course))
                {
                    UiTheme.ShowError(this, "The course could not be added.");
                    return;
                }

                this.ReloadCourses();
                this.LoadSelectors();
                UiTheme.ShowInfo(this, course.GetDisplayName() + " has been added to the catalogue.");
            });
        }

        /// <summary>Updates the selected course from the editor fields.</summary>
        private void OnUpdateCourse(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Course selected = this.SelectedCourse();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select a course in the grid first.");
                    return;
                }

                Course edited = this.ReadCourseFromEditor(selected.CourseId);

                if (edited == null)
                {
                    return;
                }

                if (this.courses.CodeExists(edited.Code, selected.CourseId))
                {
                    UiTheme.ShowError(this, "The catalogue code " + edited.Code + " is already used.");
                    return;
                }

                if (!this.courses.Update(edited))
                {
                    UiTheme.ShowError(this, "The course could not be updated.");
                    return;
                }

                this.ReloadCourses();
                this.LoadSelectors();
                UiTheme.ShowInfo(this, edited.GetDisplayName() + " has been updated.");
            });
        }

        /// <summary>Deletes the selected course.</summary>
        private void OnDeleteCourse(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Course selected = this.SelectedCourse();

                if (selected == null)
                {
                    UiTheme.ShowError(this, "Select a course in the grid first.");
                    return;
                }

                if (!UiTheme.Confirm(this, "Delete " + selected.GetDisplayName() +
                    "? Courses referenced by enrolments or rules cannot be deleted."))
                {
                    return;
                }

                if (!this.courses.Delete(selected.CourseId))
                {
                    UiTheme.ShowError(this, "The course could not be deleted.");
                    return;
                }

                this.ReloadCourses();
                this.LoadSelectors();
                UiTheme.ShowInfo(this, "The course has been deleted.");
            });
        }

        /// <summary>Reloads the course grid.</summary>
        private void OnRefreshCourses(object sender, EventArgs e)
        {
            this.ReloadCourses();
        }

        /// <summary>Adds a curriculum rule from the selectors.</summary>
        private void OnAddRule(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                Course dependent = this.cmbRuleCourse.SelectedItem as Course;
                Course required = this.cmbRuleRequired.SelectedItem as Course;

                if (dependent == null || required == null)
                {
                    UiTheme.ShowError(this, "Choose both a course and the course it requires.");
                    return;
                }

                if (dependent.CourseId == required.CourseId)
                {
                    UiTheme.ShowError(this, "A course cannot require itself.");
                    return;
                }

                RuleType ruleType = (RuleType)this.cmbRuleType.SelectedItem;

                // The domain object validates the rule, so an impossible rule is refused
                // before the database is asked to store it.
                Prerequisite rule = new Prerequisite(0, dependent, required, ruleType);

                if (!this.prerequisites.Add(rule))
                {
                    UiTheme.ShowError(this, "The rule could not be added. It may already exist.");
                    return;
                }

                this.ReloadRules();
                UiTheme.ShowInfo(this, rule.GetDescription());
            });
        }

        /// <summary>Deletes the selected curriculum rule.</summary>
        private void OnDeleteRule(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                if (this.gridRules.CurrentRow == null)
                {
                    UiTheme.ShowError(this, "Select a rule in the grid first.");
                    return;
                }

                RuleRow row = this.gridRules.CurrentRow.DataBoundItem as RuleRow;

                if (row == null || row.Rule == null)
                {
                    return;
                }

                if (!UiTheme.Confirm(this, "Delete the rule: " + row.Rule.GetDescription() + "?"))
                {
                    return;
                }

                if (!this.prerequisites.Delete(row.Rule.PrerequisiteId))
                {
                    UiTheme.ShowError(this, "The rule could not be deleted.");
                    return;
                }

                this.ReloadRules();
                UiTheme.ShowInfo(this, "The rule has been deleted.");
            });
        }

        /// <summary>Reloads the rules grid.</summary>
        private void OnRefreshRules(object sender, EventArgs e)
        {
            this.ReloadRules();
        }

        /// <summary>
        /// Reads the editor fields into a course. Returns null when the values are not
        /// valid, after telling the administrator what is wrong - which surfaces the domain
        /// validation rather than duplicating it.
        /// </summary>
        /// <param name="courseId">The key to assign, or zero for a new course.</param>
        /// <returns>The course, or null when the input is invalid.</returns>
        private Course ReadCourseFromEditor(int courseId)
        {
            int creditHours;

            if (!int.TryParse(this.txtCredits.Text.Trim(), out creditHours))
            {
                UiTheme.ShowError(this, "Credit hours must be a whole number between 1 and 6.");
                return null;
            }

            Course course;

            try
            {
                course = new Course(courseId, this.txtCode.Text, this.txtTitle.Text,
                    creditHours, this.chkCore.Checked, string.Empty);
            }
            catch (ArgumentException exception)
            {
                // ArgumentOutOfRangeException derives from ArgumentException, so one catch
                // covers both an invalid name and an invalid credit hour value.
                UiTheme.ShowError(this, exception.Message);
                return null;
            }

            course.Specialization = this.chkCore.Checked
                ? null
                : this.cmbSpecialization.SelectedItem as Specialization;

            return course;
        }

        /// <summary>Returns the course currently selected in the grid.</summary>
        private Course SelectedCourse()
        {
            if (this.gridCourses.CurrentRow == null)
            {
                return null;
            }

            CourseRow row = this.gridCourses.CurrentRow.DataBoundItem as CourseRow;

            return row == null ? null : row.Course;
        }
    }
}
