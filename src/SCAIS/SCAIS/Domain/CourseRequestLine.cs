using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// One requested course inside a course plan.
    /// </summary>
    /// <remarks>
    /// <para><b>Composition.</b> A line is created by its owning
    /// <see cref="CourseRequest"/> and has no identity or lifetime of its own: it cannot
    /// be moved to another request, and deleting the request deletes its lines. That is
    /// why <see cref="CourseRequest.Lines"/> is a composition rather than an
    /// aggregation, and why the database foreign key cascades on delete.</para>
    ///
    /// <para>The line stores the verdict of the eligibility check as text. The prefix
    /// "Blocked" is the marker written by the eligibility rules, so
    /// <see cref="IsEligible"/> can report the verdict without a second database
    /// column. The note is what the student sees next to the course, which is the
    /// "block or warn" behaviour the business case requires.</para>
    /// </remarks>
    public class CourseRequestLine
    {
        /// <summary>Prefix written by the eligibility rules when a course is refused.</summary>
        public const string BlockedPrefix = "Blocked";

        /// <summary>Primary key of the CourseRequestLines row.</summary>
        private readonly int lineId;

        /// <summary>The owning request.</summary>
        private readonly int requestId;

        /// <summary>The requested course.</summary>
        private readonly Course course;

        /// <summary>Verdict text produced by the eligibility check.</summary>
        private string eligibilityNote;

        /// <summary>Creates a requested course line.</summary>
        /// <param name="lineId">Primary key, or zero when not yet saved.</param>
        /// <param name="requestId">The owning request.</param>
        /// <param name="course">The requested course.</param>
        /// <param name="eligibilityNote">Verdict text, or null when not yet evaluated.</param>
        public CourseRequestLine(int lineId, int requestId, Course course, string eligibilityNote)
        {
            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            this.lineId = lineId;
            this.requestId = requestId;
            this.course = course;
            this.eligibilityNote = eligibilityNote;
        }

        /// <summary>Gets the primary key of the line.</summary>
        public int LineId
        {
            get { return this.lineId; }
        }

        /// <summary>Gets the owning request.</summary>
        public int RequestId
        {
            get { return this.requestId; }
        }

        /// <summary>Gets the requested course.</summary>
        public Course Course
        {
            get { return this.course; }
        }

        /// <summary>
        /// Gets the verdict text produced by the eligibility check. Null means the
        /// check has not run yet.
        /// </summary>
        public string EligibilityNote
        {
            get { return this.eligibilityNote; }
        }

        /// <summary>
        /// Gets a value indicating whether this course was found eligible. A line that
        /// has not been evaluated yet is treated as eligible.
        /// </summary>
        public bool IsEligible
        {
            get
            {
                return string.IsNullOrEmpty(this.eligibilityNote)
                    || !this.eligibilityNote.StartsWith(BlockedPrefix, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Records the verdict of the eligibility check for this course.
        /// </summary>
        /// <param name="note">The verdict text, or null to clear it.</param>
        public void SetEligibilityNote(string note)
        {
            this.eligibilityNote = note;
        }

        /// <summary>Returns the course and the verdict.</summary>
        public override string ToString()
        {
            string courseName = this.course == null ? "(course)" : this.course.GetDisplayName();
            return this.IsEligible ? courseName : courseName + " - " + this.eligibilityNote;
        }
    }
}
