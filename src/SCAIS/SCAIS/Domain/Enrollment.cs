using System;
using System.Collections.Generic;

namespace SCAIS.Domain
{
    /// <summary>
    /// One entry in a student's academic record: the fact that a student has a given
    /// status for a given course in a given semester.
    /// </summary>
    /// <remarks>
    /// <para><b>This class is the strongest example of encapsulation in SCAIS.</b></para>
    /// <para>
    /// The two attributes that carry the meaning of the record - the course status and
    /// the grade - have <b>no public setter at all</b>. They are private fields exposed
    /// through get-only properties, and the only way to change them is through
    /// <see cref="MarkCompleted"/>, <see cref="MarkInProgress"/> or
    /// <see cref="MarkPending"/>. Because those methods validate their input, an
    /// <c>Enrollment</c> can never be put into an impossible state: it cannot be
    /// <i>Completed</i> without a grade, and it cannot hold a grade that is not a real
    /// BIT grade. This is the same rule the database enforces with the
    /// <c>CK_Enrollments_Grade</c> check constraint, enforced in the object so that
    /// invalid data never even reaches the database.
    /// </para>
    /// <para>
    /// <see cref="EnrollmentId"/>, <see cref="StudentId"/> and <see cref="SemesterId"/>
    /// are read-only after construction because they identify the record; changing them
    /// would silently move the enrolment to another student.
    /// </para>
    /// </remarks>
    public class Enrollment
    {
        /// <summary>The grades that BIT awards, used to validate the setter.</summary>
        private static readonly string[] ValidGrades =
        {
            "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "F"
        };

        /// <summary>Primary key of the Enrollments row.</summary>
        private readonly int enrollmentId;

        /// <summary>Owning student.</summary>
        private readonly int studentId;

        /// <summary>Semester in which the course was taken.</summary>
        private readonly int semesterId;

        /// <summary>The course. Private so that status and grade are the only mutators.</summary>
        private Course course;

        /// <summary>Current status. Private, with no public setter.</summary>
        private EnrollmentStatus status;

        /// <summary>Current grade. Private, with no public setter.</summary>
        private string grade;

        /// <summary>Creates an academic record entry.</summary>
        /// <param name="enrollmentId">Primary key, or zero when not yet saved.</param>
        /// <param name="studentId">The student this entry belongs to.</param>
        /// <param name="course">The course studied.</param>
        /// <param name="semesterId">The semester in which it was studied.</param>
        /// <param name="status">Initial status.</param>
        /// <param name="grade">Initial grade, or null when the course is not completed.</param>
        public Enrollment(int enrollmentId, int studentId, Course course, int semesterId,
                          EnrollmentStatus status, string grade)
        {
            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            this.enrollmentId = enrollmentId;
            this.studentId = studentId;
            this.course = course;
            this.semesterId = semesterId;

            // Routed through the behaviour methods so the invariants always hold,
            // even when the object is being rebuilt from a database row.
            switch (status)
            {
                case EnrollmentStatus.Completed:
                    this.MarkCompleted(grade);
                    break;

                case EnrollmentStatus.InProgress:
                    this.MarkInProgress();
                    break;

                default:
                    this.MarkPending();
                    break;
            }
        }

        /// <summary>Gets the primary key of the enrolment.</summary>
        public int EnrollmentId
        {
            get { return this.enrollmentId; }
        }

        /// <summary>Gets the student this entry belongs to.</summary>
        public int StudentId
        {
            get { return this.studentId; }
        }

        /// <summary>Gets the semester this entry belongs to.</summary>
        public int SemesterId
        {
            get { return this.semesterId; }
        }

        /// <summary>Gets the course studied.</summary>
        public Course Course
        {
            get { return this.course; }
        }

        /// <summary>
        /// Gets the current status. Read-only: use <see cref="MarkCompleted"/>,
        /// <see cref="MarkInProgress"/> or <see cref="MarkPending"/> to change it.
        /// </summary>
        public EnrollmentStatus Status
        {
            get { return this.status; }
        }

        /// <summary>
        /// Gets the grade, or null when the course has not been completed. Read-only:
        /// the grade is set by <see cref="MarkCompleted"/>.
        /// </summary>
        public string Grade
        {
            get { return this.grade; }
        }

        /// <summary>
        /// Marks the course as passed and records the grade. A blank or unknown grade is
        /// rejected, which is what guarantees the "Completed implies a grade" invariant.
        /// </summary>
        /// <param name="grade">The grade awarded, for example "B+".</param>
        public void MarkCompleted(string grade)
        {
            if (!IsValidGrade(grade))
            {
                throw new ArgumentException(
                    "A completed course must have a valid grade such as A, B+ or C.", "grade");
            }

            this.status = EnrollmentStatus.Completed;
            this.grade = grade.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Marks the course as currently being taken. The grade is cleared because a
        /// course in progress has no result yet.
        /// </summary>
        public void MarkInProgress()
        {
            this.status = EnrollmentStatus.InProgress;
            this.grade = null;
        }

        /// <summary>
        /// Marks the course as not yet taken. The grade is cleared for the same reason
        /// as <see cref="MarkInProgress"/>.
        /// </summary>
        public void MarkPending()
        {
            this.status = EnrollmentStatus.Pending;
            this.grade = null;
        }

        /// <summary>Returns true when the course has been passed.</summary>
        public bool IsCompleted()
        {
            return this.status == EnrollmentStatus.Completed;
        }

        /// <summary>Returns true when the course is currently being taken.</summary>
        public bool IsInProgress()
        {
            return this.status == EnrollmentStatus.InProgress;
        }

        /// <summary>Returns true when the course has not been taken yet.</summary>
        public bool IsPending()
        {
            return this.status == EnrollmentStatus.Pending;
        }

        /// <summary>
        /// Returns true when the recorded grade is a pass rather than a fail.
        /// </summary>
        public bool HasPassed()
        {
            return this.status == EnrollmentStatus.Completed
                && !string.Equals(this.grade, "F", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns the status and grade as display text.</summary>
        public string GetStatusText()
        {
            if (this.status == EnrollmentStatus.Completed)
            {
                return "Completed (" + this.grade + ")";
            }

            return this.status == EnrollmentStatus.InProgress ? "In progress" : "Pending";
        }

        /// <summary>Returns the course and the current status.</summary>
        public override string ToString()
        {
            string courseName = this.course == null ? "(course)" : this.course.GetDisplayName();
            return courseName + " - " + this.GetStatusText();
        }

        /// <summary>
        /// Returns true when the supplied value is one of the grades BIT awards.
        /// </summary>
        private static bool IsValidGrade(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string candidate = value.Trim().ToUpperInvariant();

            for (int index = 0; index < ValidGrades.Length; index++)
            {
                if (string.Equals(ValidGrades[index], candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
