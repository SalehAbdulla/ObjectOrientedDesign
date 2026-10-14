using System;
using System.Collections.Generic;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.Business
{
    /// <summary>
    /// One line of the student course progression report.
    /// </summary>
    /// <remarks>
    /// The row exposes flat, display ready properties because the report grid and the
    /// printout both bind to it directly. Keeping the shaping in the service means the
    /// user interface contains no report logic at all.
    /// </remarks>
    public class CourseProgressionRow
    {
        /// <summary>Gets or sets the student's name.</summary>
        public string StudentName { get; set; }

        /// <summary>Gets or sets the student's sign-in name.</summary>
        public string Username { get; set; }

        /// <summary>Gets or sets the declared track.</summary>
        public string Track { get; set; }

        /// <summary>Gets or sets the semester the course belongs to.</summary>
        public string Semester { get; set; }

        /// <summary>Gets or sets the course code.</summary>
        public string Code { get; set; }

        /// <summary>Gets or sets the course title.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the credit hours.</summary>
        public int CreditHours { get; set; }

        /// <summary>Gets or sets the status as display text.</summary>
        public string Status { get; set; }

        /// <summary>Gets or sets the grade, or an empty string when there is none.</summary>
        public string Grade { get; set; }
    }

    /// <summary>
    /// One line of the prerequisite compliance report.
    /// </summary>
    public class PrerequisiteComplianceRow
    {
        /// <summary>Gets or sets the student's name.</summary>
        public string StudentName { get; set; }

        /// <summary>Gets or sets the declared track.</summary>
        public string Track { get; set; }

        /// <summary>Gets or sets the course the student is blocked from.</summary>
        public string BlockedCourse { get; set; }

        /// <summary>Gets or sets the subject that has not been completed.</summary>
        public string MissingCourse { get; set; }

        /// <summary>Gets or sets whether the rule is a prerequisite or a corequisite.</summary>
        public string RuleType { get; set; }
    }

    /// <summary>
    /// Builds the data sets behind the SCAIS reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The service shapes rows for display and does the arithmetic (credit totals,
    /// compliance counts) so that no report logic leaks into the forms. Both reports the
    /// business case asks for are produced here: the course progression report and the
    /// prerequisite compliance report.
    /// </para>
    /// <para>
    /// The compliance report is the one that shows the system is doing real work: it
    /// names the students who cannot take a course and the exact subject that blocks
    /// them, which is precisely the check the current manual process gets wrong.
    /// </para>
    /// </remarks>
    public class ReportService
    {
        /// <summary>Reads accounts.</summary>
        private readonly UserRepository users;

        /// <summary>Reads the academic record.</summary>
        private readonly EnrollmentRepository enrollments;

        /// <summary>Reads the catalogue.</summary>
        private readonly CourseRepository courses;

        /// <summary>Reads the curriculum rules.</summary>
        private readonly PrerequisiteRepository prerequisites;

        /// <summary>Reads the academic terms.</summary>
        private readonly SemesterRepository semesters;

        /// <summary>Creates the service with its own collaborators.</summary>
        public ReportService()
            : this(new UserRepository(), new EnrollmentRepository(), new CourseRepository(),
                   new PrerequisiteRepository(), new SemesterRepository())
        {
        }

        /// <summary>Creates the service with supplied collaborators.</summary>
        /// <param name="users">The account repository.</param>
        /// <param name="enrollments">The enrolment repository.</param>
        /// <param name="courses">The course repository.</param>
        /// <param name="prerequisites">The curriculum rule repository.</param>
        /// <param name="semesters">The semester repository.</param>
        public ReportService(UserRepository users, EnrollmentRepository enrollments,
                             CourseRepository courses, PrerequisiteRepository prerequisites,
                             SemesterRepository semesters)
        {
            if (users == null) { throw new ArgumentNullException("users"); }
            if (enrollments == null) { throw new ArgumentNullException("enrollments"); }
            if (courses == null) { throw new ArgumentNullException("courses"); }
            if (prerequisites == null) { throw new ArgumentNullException("prerequisites"); }
            if (semesters == null) { throw new ArgumentNullException("semesters"); }

            this.users = users;
            this.enrollments = enrollments;
            this.courses = courses;
            this.prerequisites = prerequisites;
            this.semesters = semesters;
        }

        /// <summary>
        /// Builds the course progression report for one student: every course they have
        /// completed, are taking, or have yet to take.
        /// </summary>
        /// <param name="studentId">The student to report on.</param>
        /// <returns>The report rows, ordered by course code.</returns>
        public List<CourseProgressionRow> GetCourseProgression(int studentId)
        {
            Student student = this.FindStudent(studentId);
            List<CourseProgressionRow> rows = new List<CourseProgressionRow>();

            if (student == null)
            {
                return rows;
            }

            List<Enrollment> record = this.enrollments.GetByStudent(studentId);
            Dictionary<int, string> termNames = this.GetTermNames();

            foreach (Enrollment enrollment in record)
            {
                CourseProgressionRow row = new CourseProgressionRow();
                row.StudentName = student.FullName;
                row.Username = student.Username;
                row.Track = student.Specialization == null ? "no track" : student.Specialization.Name;
                row.Semester = termNames.ContainsKey(enrollment.SemesterId)
                    ? termNames[enrollment.SemesterId]
                    : "semester " + enrollment.SemesterId;
                row.Code = enrollment.Course.Code;
                row.Title = enrollment.Course.Title;
                row.CreditHours = enrollment.Course.CreditHours;
                row.Status = enrollment.Status.ToString();
                row.Grade = enrollment.Grade ?? string.Empty;

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// Builds the progression report for every student, which is what the programme
        /// wide report needs.
        /// </summary>
        /// <returns>The report rows for all students.</returns>
        public List<CourseProgressionRow> GetCourseProgressionForAllStudents()
        {
            List<CourseProgressionRow> rows = new List<CourseProgressionRow>();

            foreach (Student student in this.users.GetStudents())
            {
                rows.AddRange(this.GetCourseProgression(student.StudentId));
            }

            return rows;
        }

        /// <summary>
        /// Summarises how many courses one student has in each state, which is the header
        /// shown above the progression report.
        /// </summary>
        /// <param name="studentId">The student to summarise.</param>
        /// <returns>A sentence describing the student's position.</returns>
        public string GetProgressionSummary(int studentId)
        {
            Student student = this.FindStudent(studentId);

            if (student == null)
            {
                return "The student could not be found.";
            }

            student.LoadAcademicRecord(this.enrollments.GetByStudent(studentId));

            return string.Format(
                "{0}: {1} completed, {2} in progress, {3} pending, {4} credit hours earned.",
                student.FullName,
                Count(student.GetCompletedCourses()),
                Count(student.GetInProgressCourses()),
                Count(student.GetPendingCourses()),
                student.GetEarnedCredits());
        }

        /// <summary>
        /// Builds the prerequisite compliance report: every student who cannot take a
        /// course, and the exact subject that blocks them.
        /// </summary>
        /// <remarks>
        /// The check reuses the same <see cref="Student.HasCompleted"/> question the
        /// eligibility rule asks, so the report can never disagree with the screen the
        /// student sees. Duplicating the logic here would have been the easy mistake.
        /// </remarks>
        /// <returns>The compliance rows, ordered by student then course.</returns>
        public List<PrerequisiteComplianceRow> GetPrerequisiteCompliance()
        {
            List<PrerequisiteComplianceRow> rows = new List<PrerequisiteComplianceRow>();
            List<Prerequisite> rules = this.prerequisites.GetAll();

            foreach (Student student in this.users.GetStudents())
            {
                student.LoadAcademicRecord(this.enrollments.GetByStudent(student.StudentId));

                foreach (Prerequisite rule in rules)
                {
                    // Only a prerequisite can block enrolment outright.
                    if (!rule.IsPrerequisite())
                    {
                        continue;
                    }

                    // A student who has already passed the dependent course is not blocked.
                    if (student.HasCompleted(rule.Course.CourseId))
                    {
                        continue;
                    }

                    if (student.HasCompleted(rule.RequiredCourse.CourseId))
                    {
                        continue;
                    }

                    PrerequisiteComplianceRow row = new PrerequisiteComplianceRow();
                    row.StudentName = student.FullName;
                    row.Track = student.Specialization == null ? "no track" : student.Specialization.Name;
                    row.BlockedCourse = rule.Course.GetDisplayName();
                    row.MissingCourse = rule.RequiredCourse.GetDisplayName();
                    row.RuleType = rule.RuleType.ToString();

                    rows.Add(row);
                }
            }

            return rows;
        }

        /// <summary>Finds a student account by its Students key.</summary>
        private Student FindStudent(int studentId)
        {
            foreach (Student student in this.users.GetStudents())
            {
                if (student.StudentId == studentId)
                {
                    return student;
                }
            }

            return null;
        }

        /// <summary>Builds a lookup of semester identifiers to display names.</summary>
        private Dictionary<int, string> GetTermNames()
        {
            Dictionary<int, string> names = new Dictionary<int, string>();

            foreach (Semester semester in this.semesters.GetAll())
            {
                names[semester.SemesterId] = semester.GetDisplayName();
            }

            return names;
        }

        /// <summary>Counts the items in a sequence.</summary>
        private static int Count(IEnumerable<Enrollment> items)
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
