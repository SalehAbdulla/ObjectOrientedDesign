using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SCAIS.Business;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// The reports screen, carrying the two reports the business case requires.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two reports are the <b>student course progression report</b> (what a student
    /// has completed, is taking and has yet to take) and the <b>prerequisite compliance
    /// report</b> (which students cannot take a course, and the exact subject blocking
    /// them).
    /// </para>
    /// <para>
    /// The screen renders whatever <see cref="ReportService"/> returns. It performs no
    /// arithmetic and applies no eligibility rules of its own, so a report can never
    /// disagree with the screens the students and advisers see.
    /// </para>
    /// <para>
    /// Printing uses <see cref="PrintDocument"/> rather than an external reporting
    /// component, so the application has no third-party dependency and "Microsoft Print to
    /// PDF" can be used to produce the report as a file.
    /// </para>
    /// </remarks>
    public class frmReports : Form
    {
        /// <summary>Builds the report data.</summary>
        private readonly ReportService reports;

        /// <summary>Reads students for the selector.</summary>
        private readonly UserRepository users;

        /// <summary>The report selector.</summary>
        private ComboBox cmbReport;

        /// <summary>The student selector, used by the progression report.</summary>
        private ComboBox cmbStudent;

        /// <summary>The grid showing the report.</summary>
        private DataGridView grid;

        /// <summary>The heading shown above the report.</summary>
        private Label lblHeading;

        /// <summary>The status line.</summary>
        private Label lblStatus;

        /// <summary>The rows currently displayed, kept for printing and export.</summary>
        private List<string[]> currentRows = new List<string[]>();

        /// <summary>The column headers of the report currently displayed.</summary>
        private string[] currentHeaders = new string[0];

        /// <summary>The row being printed.</summary>
        private int printRowIndex;

        /// <summary>Creates the screen.</summary>
        public frmReports()
        {
            this.reports = new ReportService();
            this.users = new UserRepository();
            this.BuildInterface();
        }

        /// <summary>Builds the controls.</summary>
        private void BuildInterface()
        {
            UiTheme.ApplyForm(this, "Reports");
            this.ClientSize = new Size(960, 620);

            this.Controls.Add(UiTheme.CreateHeader("Reports"));

            Label reportLabel = UiTheme.CreateLabel("Report");
            reportLabel.Location = new Point(14, 62);
            reportLabel.AutoSize = true;
            this.Controls.Add(reportLabel);

            this.cmbReport = new ComboBox();
            this.cmbReport.Font = UiTheme.BodyFont;
            this.cmbReport.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbReport.Location = new Point(14, 84);
            this.cmbReport.Width = 320;
            this.cmbReport.Items.Add("Student course progression");
            this.cmbReport.Items.Add("Prerequisite compliance");
            this.cmbReport.SelectedIndex = 0;
            this.cmbReport.SelectedIndexChanged += this.OnReportChanged;
            this.Controls.Add(this.cmbReport);

            Label studentLabel = UiTheme.CreateLabel("Student");
            studentLabel.Location = new Point(352, 62);
            studentLabel.AutoSize = true;
            this.Controls.Add(studentLabel);

            this.cmbStudent = new ComboBox();
            this.cmbStudent.Font = UiTheme.BodyFont;
            this.cmbStudent.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbStudent.Location = new Point(352, 84);
            this.cmbStudent.Width = 320;
            this.Controls.Add(this.cmbStudent);

            Button run = UiTheme.CreateButton("Run report", this.OnRunReport);
            run.Location = new Point(690, 82);
            this.Controls.Add(run);

            this.lblHeading = UiTheme.CreateLabel(string.Empty);
            this.lblHeading.Font = UiTheme.SubHeadingFont;
            this.lblHeading.ForeColor = UiTheme.HeaderColour;
            this.lblHeading.Location = new Point(14, 120);
            this.lblHeading.AutoSize = true;
            this.Controls.Add(this.lblHeading);

            this.grid = UiTheme.CreateGrid();
            this.grid.Location = new Point(14, 146);
            this.grid.Size = new Size(930, 390);
            this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(this.grid);

            Button print = UiTheme.CreateButton("Print", this.OnPrint);
            print.Location = new Point(14, 548);
            print.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(print);

            Button export = UiTheme.CreateButton("Export to CSV", this.OnExport);
            export.Location = new Point(166, 548);
            export.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(export);

            Button close = UiTheme.CreateButton("Close", this.OnClose);
            close.Location = new Point(844, 548);
            close.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(close);

            this.lblStatus = UiTheme.CreateLabel("Ready.");
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(318, 554);
            this.lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblStatus.AutoSize = true;
            this.Controls.Add(this.lblStatus);

            this.Load += this.OnLoad;
        }

        /// <summary>Loads the student selector when the screen opens.</summary>
        private void OnLoad(object sender, EventArgs e)
        {
            UiTheme.Guard(this, delegate
            {
                this.cmbStudent.Items.Clear();

                foreach (Student student in this.users.GetStudents())
                {
                    this.cmbStudent.Items.Add(student);
                }

                if (this.cmbStudent.Items.Count > 0)
                {
                    this.cmbStudent.SelectedIndex = 0;
                }

                this.RunSelectedReport();
            });
        }

        /// <summary>Re-runs the report when a different report is chosen.</summary>
        private void OnReportChanged(object sender, EventArgs e)
        {
            // The student selector only applies to the progression report, so it is
            // disabled rather than hidden, which keeps the layout stable.
            this.cmbStudent.Enabled = this.cmbReport.SelectedIndex == 0;
        }

        /// <summary>Runs the selected report.</summary>
        private void OnRunReport(object sender, EventArgs e)
        {
            this.RunSelectedReport();
        }

        /// <summary>
        /// Builds and displays the selected report, and remembers its shape for printing
        /// and export.
        /// </summary>
        private void RunSelectedReport()
        {
            UiTheme.Guard(this, delegate
            {
                this.currentRows.Clear();

                if (this.cmbReport.SelectedIndex == 0)
                {
                    this.BuildProgressionReport();
                }
                else
                {
                    this.BuildComplianceReport();
                }

                this.grid.DataSource = null;
                this.grid.Columns.Clear();

                for (int index = 0; index < this.currentHeaders.Length; index++)
                {
                    UiTheme.AddColumn(this.grid, "Column" + index, this.currentHeaders[index], 20);
                }

                List<ReportGridRow> displayRows = new List<ReportGridRow>();

                foreach (string[] row in this.currentRows)
                {
                    ReportGridRow display = new ReportGridRow();

                    for (int index = 0; index < row.Length && index < 6; index++)
                    {
                        display.Set(index, row[index]);
                    }

                    displayRows.Add(display);
                }

                this.grid.DataSource = displayRows;

                this.lblStatus.Text = this.currentRows.Count + " row(s) in the report.";
            });
        }

        /// <summary>Builds the student course progression report.</summary>
        private void BuildProgressionReport()
        {
            Student student = this.cmbStudent.SelectedItem as Student;

            if (student == null)
            {
                UiTheme.ShowError(this, "Select a student for the progression report.");
                return;
            }

            this.currentHeaders = new string[]
            {
                "Code", "Course", "Credits", "Semester", "Status", "Grade"
            };

            foreach (CourseProgressionRow row in this.reports.GetCourseProgression(student.StudentId))
            {
                this.currentRows.Add(new string[]
                {
                    row.Code, row.Title, row.CreditHours.ToString(), row.Semester, row.Status, row.Grade
                });
            }

            this.lblHeading.Text = "Student course progression - " +
                this.reports.GetProgressionSummary(student.StudentId);
        }

        /// <summary>Builds the prerequisite compliance report.</summary>
        private void BuildComplianceReport()
        {
            this.currentHeaders = new string[]
            {
                "Student", "Track", "Blocked from", "Missing", "Rule type"
            };

            foreach (PrerequisiteComplianceRow row in this.reports.GetPrerequisiteCompliance())
            {
                this.currentRows.Add(new string[]
                {
                    row.StudentName, row.Track, row.BlockedCourse, row.MissingCourse, row.RuleType
                });
            }

            this.lblHeading.Text =
                "Prerequisite compliance - students who cannot take a course, and why";
        }

        /// <summary>
        /// A generic grid row. The two reports have different columns, so rather than
        /// binding to a domain type the screen projects each report into this fixed shape,
        /// which keeps one grid able to display both.
        /// </summary>
        private class ReportGridRow
        {
            /// <summary>Gets or sets the first column.</summary>
            public string Column0 { get; set; }

            /// <summary>Gets or sets the second column.</summary>
            public string Column1 { get; set; }

            /// <summary>Gets or sets the third column.</summary>
            public string Column2 { get; set; }

            /// <summary>Gets or sets the fourth column.</summary>
            public string Column3 { get; set; }

            /// <summary>Gets or sets the fifth column.</summary>
            public string Column4 { get; set; }

            /// <summary>Gets or sets the sixth column.</summary>
            public string Column5 { get; set; }

            /// <summary>Assigns a value to a column by index.</summary>
            /// <param name="index">The column index.</param>
            /// <param name="value">The value to store.</param>
            public void Set(int index, string value)
            {
                switch (index)
                {
                    case 0: this.Column0 = value; break;
                    case 1: this.Column1 = value; break;
                    case 2: this.Column2 = value; break;
                    case 3: this.Column3 = value; break;
                    case 4: this.Column4 = value; break;
                    default: this.Column5 = value; break;
                }
            }
        }

        /// <summary>Sends the current report to a printer, including a print-to-PDF driver.</summary>
        private void OnPrint(object sender, EventArgs e)
        {
            if (this.currentRows.Count == 0)
            {
                UiTheme.ShowError(this, "Run a report before printing it.");
                return;
            }

            UiTheme.Guard(this, delegate
            {
                PrintDocument document = new PrintDocument();
                document.DocumentName = this.lblHeading.Text;
                document.PrintPage += this.OnPrintPage;

                this.printRowIndex = 0;

                using (PrintDialog dialog = new PrintDialog())
                {
                    dialog.Document = document;

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        document.Print();
                    }
                }
            });
        }

        /// <summary>
        /// Renders one page of the report: the heading, the column headers and as many
        /// rows as fit before the page is full.
        /// </summary>
        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Font titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
            Font bodyFont = new Font("Segoe UI", 9F);
            Font headerFont = new Font("Segoe UI", 9F, FontStyle.Bold);

            float left = e.MarginBounds.Left;
            float top = e.MarginBounds.Top;
            float lineHeight = bodyFont.GetHeight(graphics) + 4;

            graphics.DrawString("SCAIS - " + this.lblHeading.Text, titleFont, Brushes.Black, left, top);
            top += titleFont.GetHeight(graphics) + 10;

            float columnWidth = e.MarginBounds.Width / this.currentHeaders.Length;

            for (int index = 0; index < this.currentHeaders.Length; index++)
            {
                graphics.DrawString(this.currentHeaders[index], headerFont, Brushes.Black,
                    left + (index * columnWidth), top);
            }

            top += lineHeight;
            graphics.DrawLine(Pens.Gray, left, top, left + e.MarginBounds.Width, top);
            top += 4;

            while (this.printRowIndex < this.currentRows.Count)
            {
                if (top + lineHeight > e.MarginBounds.Bottom)
                {
                    // The page is full. Ask for another page and carry on from here.
                    e.HasMorePages = true;
                    return;
                }

                string[] row = this.currentRows[this.printRowIndex];

                for (int index = 0; index < row.Length; index++)
                {
                    graphics.DrawString(row[index], bodyFont, Brushes.Black,
                        left + (index * columnWidth), top);
                }

                top += lineHeight;
                this.printRowIndex++;
            }

            graphics.DrawString(
                "Generated by SCAIS on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                bodyFont, Brushes.Gray, left, top + 12);

            e.HasMorePages = false;
        }

        /// <summary>Exports the current report as a CSV file.</summary>
        private void OnExport(object sender, EventArgs e)
        {
            if (this.currentRows.Count == 0)
            {
                UiTheme.ShowError(this, "Run a report before exporting it.");
                return;
            }

            UiTheme.Guard(this, delegate
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Comma separated values (*.csv)|*.csv|All files (*.*)|*.*";
                    dialog.FileName = "SCAIS-report.csv";

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    StringBuilder builder = new StringBuilder();

                    for (int index = 0; index < this.currentHeaders.Length; index++)
                    {
                        if (index > 0) { builder.Append(','); }
                        builder.Append(Escape(this.currentHeaders[index]));
                    }

                    builder.AppendLine();

                    foreach (string[] row in this.currentRows)
                    {
                        for (int index = 0; index < row.Length; index++)
                        {
                            if (index > 0) { builder.Append(','); }
                            builder.Append(Escape(row[index]));
                        }

                        builder.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, builder.ToString());

                    UiTheme.ShowInfo(this, "The report has been exported to:\r\n" + dialog.FileName);
                }
            });
        }

        /// <summary>
        /// Quotes a CSV value when it contains a comma, a quote or a line break, so the
        /// exported file opens correctly in a spreadsheet.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped value.</returns>
        private static string Escape(string value)
        {
            string text = value ?? string.Empty;

            if (text.IndexOf(',') < 0 && text.IndexOf('"') < 0
                && text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
            {
                return text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Closes the screen.</summary>
        private void OnClose(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
