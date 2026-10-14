namespace SCAIS.Domain
{
    /// <summary>
    /// The three kinds of account that can sign in through the single login screen.
    /// </summary>
    public enum UserRole
    {
        /// <summary>Maintains the course catalogue, user accounts and system rules.</summary>
        Administrator,

        /// <summary>Faculty member who manages advisees and approves course selections.</summary>
        Adviser,

        /// <summary>IT student with a declared specialization.</summary>
        Student
    }

    /// <summary>
    /// The three states a course can be in for a student.
    /// </summary>
    public enum EnrollmentStatus
    {
        /// <summary>The student has passed the course and has a grade.</summary>
        Completed,

        /// <summary>The student is currently taking the course.</summary>
        InProgress,

        /// <summary>The student has not taken the course yet.</summary>
        Pending
    }

    /// <summary>
    /// The lifecycle of a course plan submitted by a student.
    /// </summary>
    public enum RequestStatus
    {
        /// <summary>Being prepared by the student and not yet sent to the adviser.</summary>
        Draft,

        /// <summary>Sent to the adviser and awaiting a decision.</summary>
        Submitted,

        /// <summary>Accepted by the adviser.</summary>
        Approved,

        /// <summary>Declined by the adviser.</summary>
        Rejected
    }

    /// <summary>
    /// Distinguishes a rule that must be satisfied before enrolment from one that
    /// may be satisfied at the same time.
    /// </summary>
    public enum RuleType
    {
        /// <summary>The required course must already be completed.</summary>
        Prerequisite,

        /// <summary>The required course may be taken in the same semester.</summary>
        Corequisite
    }
}
