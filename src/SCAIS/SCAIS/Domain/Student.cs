using System;
using System.Collections.Generic;

namespace SCAIS.Domain
{
    /// <summary>
    /// An IT student with a declared specialization.
    /// </summary>
    /// <remarks>
    /// A <c>Student</c> is a <see cref="User"/> (the is-a test holds: every student is
    /// a user of SCAIS and can sign in), so identity and authentication are inherited
    /// rather than duplicated. What is added here is the academic state that only a
    /// student has: the declared track, the assigned adviser, and the academic record
    /// of courses that are completed, in progress or still pending.
    ///
    /// The record is held as a collection of <see cref="Enrollment"/> objects so that
    /// a student answers questions about its own progress, instead of the user
    /// interface or a service reaching into the database for it.
    /// </remarks>
    public class Student : User
    {
        /// <summary>Primary key of the Students row.</summary>
        private readonly int studentId;

        /// <summary>Year the student first enrolled.</summary>
        private readonly int enrollmentYear;

        /// <summary>Grade point average, or null when the student has no grades yet.</summary>
        private readonly decimal? gpa;

        /// <summary>The academic record: every course this student has any status for.</summary>
        private readonly List<Enrollment> enrollments = new List<Enrollment>();

        /// <summary>The declared track.</summary>
        private Specialization specialization;

        /// <summary>The assigned adviser, or null when none has been assigned yet.</summary>
        private Adviser adviser;

        /// <summary>Creates a student account.</summary>
        public Student(int userId, string username, string passwordHash, string fullName, string email,
                       bool isActive, int studentId, int enrollmentYear, decimal? gpa)
            : base(userId, username, passwordHash, fullName, email, UserRole.Student, isActive)
        {
            this.studentId = studentId;
            this.enrollmentYear = enrollmentYear;
            this.gpa = gpa;
        }

        /// <summary>Gets the primary key of the student record.</summary>
        public int StudentId
        {
            get { return this.studentId; }
        }

        /// <summary>Gets the year the student first enrolled.</summary>
        public int EnrollmentYear
        {
            get { return this.enrollmentYear; }
        }

        /// <summary>Gets the grade point average, or null when no grades exist yet.</summary>
        public decimal? GPA
        {
            get { return this.gpa; }
        }

        /// <summary>
        /// Gets or sets the declared specialization. Set by the data layer when the
        /// student is loaded, because the track name lives in the Specializations table.
        /// </summary>
        public Specialization Specialization
        {
            get { return this.specialization; }
            set { this.specialization = value; }
        }

        /// <summary>Gets or sets the assigned adviser.</summary>
        public Adviser Adviser
        {
            get { return this.adviser; }
            set { this.adviser = value; }
        }

        /// <summary>
        /// Gets the academic record as a read-only view. Callers cannot add or remove
        /// entries directly, which keeps the record consistent with the database.
        /// </summary>
        public IList<Enrollment> Enrollments
        {
            get { return this.enrollments.AsReadOnly(); }
        }

        /// <summary>
        /// Replaces the in-memory academic record with the supplied entries.
        /// Called by the data layer after loading.
        /// </summary>
        /// <param name="record">The enrollments that make up the record.</param>
        public void LoadAcademicRecord(IEnumerable<Enrollment> record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            this.enrollments.Clear();
            this.enrollments.AddRange(record);
        }

        /// <summary>
        /// Returns true when the student has passed the supplied course.
        /// Used by the prerequisite rule to decide eligibility.
        /// </summary>
        /// <param name="courseId">The course to look for.</param>
        /// <returns>True when a completed enrolment exists for the course.</returns>
        public bool HasCompleted(int courseId)
        {
            foreach (Enrollment enrollment in this.enrollments)
            {
                if (enrollment.Course != null
                    && enrollment.Course.CourseId == courseId
                    && enrollment.Status == EnrollmentStatus.Completed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when the student has already passed or is currently taking the
        /// supplied course, which makes a second enrolment pointless.
        /// </summary>
        /// <param name="courseId">The course to look for.</param>
        /// <returns>True when the course is completed or in progress.</returns>
        public bool HasTakenOrIsTaking(int courseId)
        {
            foreach (Enrollment enrollment in this.enrollments)
            {
                if (enrollment.Course != null
                    && enrollment.Course.CourseId == courseId
                    && enrollment.Status != EnrollmentStatus.Pending)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns every course the student has passed.</summary>
        public IEnumerable<Enrollment> GetCompletedCourses()
        {
            return this.GetCoursesWithStatus(EnrollmentStatus.Completed);
        }

        /// <summary>Returns every course the student is currently taking.</summary>
        public IEnumerable<Enrollment> GetInProgressCourses()
        {
            return this.GetCoursesWithStatus(EnrollmentStatus.InProgress);
        }

        /// <summary>Returns every course the student has not taken yet.</summary>
        public IEnumerable<Enrollment> GetPendingCourses()
        {
            return this.GetCoursesWithStatus(EnrollmentStatus.Pending);
        }

        /// <summary>
        /// Returns the total credit hours the student is committed to in a semester.
        /// Used by the credit limit rule.
        /// </summary>
        /// <param name="semesterId">The semester to total.</param>
        /// <returns>Sum of the credit hours of every unfinished enrolment in that semester.</returns>
        public int GetCreditsForSemester(int semesterId)
        {
            int total = 0;

            foreach (Enrollment enrollment in this.enrollments)
            {
                if (enrollment.SemesterId == semesterId
                    && enrollment.Status != EnrollmentStatus.Completed
                    && enrollment.Course != null)
                {
                    total += enrollment.Course.CreditHours;
                }
            }

            return total;
        }

        /// <summary>
        /// Returns the total credit hours the student has already earned by passing
        /// courses. Used by the progress report.
        /// </summary>
        public int GetEarnedCredits()
        {
            int total = 0;

            foreach (Enrollment enrollment in this.enrollments)
            {
                if (enrollment.Status == EnrollmentStatus.Completed && enrollment.Course != null)
                {
                    total += enrollment.Course.CreditHours;
                }
            }

            return total;
        }

        /// <summary>
        /// Describes the account for the shell header. Produced by this subclass, which
        /// is the polymorphism the login screen relies on.
        /// </summary>
        public override string GetProfileSummary()
        {
            string track = this.specialization == null ? "no track declared" : this.specialization.Name;
            string average = this.gpa.HasValue ? this.gpa.Value.ToString("0.00") : "not yet graded";

            return string.Format("Student - {0} - GPA {1}", track, average);
        }

        /// <summary>Gets the dashboard title for a student.</summary>
        public override string GetDashboardTitle()
        {
            return "Student Dashboard";
        }

        /// <summary>Students may not approve course plans.</summary>
        public override bool CanApproveCourseRequests
        {
            get { return false; }
        }

        /// <summary>Returns the student name together with the declared track.</summary>
        public override string ToString()
        {
            string track = this.specialization == null ? "no track" : this.specialization.Name;
            return this.FullName + " - " + track;
        }

        /// <summary>
        /// Returns the entries of the academic record that have the supplied status.
        /// </summary>
        private IEnumerable<Enrollment> GetCoursesWithStatus(EnrollmentStatus status)
        {
            List<Enrollment> matches = new List<Enrollment>();

            foreach (Enrollment enrollment in this.enrollments)
            {
                if (enrollment.Status == status)
                {
                    matches.Add(enrollment);
                }
            }

            return matches;
        }
    }
}
