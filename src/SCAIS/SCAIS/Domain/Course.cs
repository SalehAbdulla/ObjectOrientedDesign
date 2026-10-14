using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// A subject in the BIT course catalogue.
    /// </summary>
    /// <remarks>
    /// <para><b>Encapsulation.</b> Every attribute is stored in a private field. The
    /// attributes that have a rule attached to them enforce that rule inside the setter,
    /// so an invalid <c>Course</c> cannot exist at all:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CreditHours"/> must be between 1 and 6, which
    ///   mirrors the CHECK constraint in the database.</description></item>
    ///   <item><description><see cref="Code"/> must be non-blank and is normalised to
    ///   upper case so lookups are case insensitive.</description></item>
    /// </list>
    /// <para><see cref="CourseId"/> is read-only because it is assigned by the database
    /// and must never be changed by a caller.</para>
    ///
    /// <para><b>Abstraction.</b> Callers ask a course for
    /// <see cref="GetDisplayName"/> or <see cref="IsCoreSubject"/> rather than
    /// rebuilding the code and title themselves or re-deriving what "core" means.</para>
    /// </remarks>
    public class Course
    {
        /// <summary>Smallest permitted value for <see cref="CreditHours"/>.</summary>
        public const int MinimumCreditHours = 1;

        /// <summary>Largest permitted value for <see cref="CreditHours"/>.</summary>
        public const int MaximumCreditHours = 6;

        /// <summary>Primary key of the Courses row.</summary>
        private readonly int courseId;

        /// <summary>Backing field for <see cref="Code"/>.</summary>
        private string code;

        /// <summary>Backing field for <see cref="Title"/>.</summary>
        private string title;

        /// <summary>Backing field for <see cref="CreditHours"/>.</summary>
        private int creditHours;

        /// <summary>Backing field for <see cref="IsCore"/>.</summary>
        private bool isCore;

        /// <summary>Backing field for <see cref="Description"/>.</summary>
        private string description;

        /// <summary>
        /// The track this course belongs to, or null when the course is a core subject
        /// shared by every track.
        /// </summary>
        private Specialization specialization;

        /// <summary>Creates a course.</summary>
        public Course(int courseId, string code, string title, int creditHours, bool isCore, string description)
        {
            this.courseId = courseId;
            this.Code = code;
            this.Title = title;
            this.CreditHours = creditHours;
            this.isCore = isCore;
            this.description = description;
        }

        /// <summary>Gets the primary key of the course.</summary>
        public int CourseId
        {
            get { return this.courseId; }
        }

        /// <summary>
        /// Gets or sets the catalogue code, for example IT210. The value is trimmed and
        /// upper cased so that "it210" and "IT210" are the same course.
        /// </summary>
        public string Code
        {
            get { return this.code; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A course must have a catalogue code.", "value");
                }

                this.code = value.Trim().ToUpperInvariant();
            }
        }

        /// <summary>Gets or sets the course title.</summary>
        public string Title
        {
            get { return this.title; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A course must have a title.", "value");
                }

                this.title = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the credit hours. Values outside the permitted range are
        /// rejected, which stops the credit limit rule from ever seeing an impossible
        /// course.
        /// </summary>
        public int CreditHours
        {
            get { return this.creditHours; }

            set
            {
                if (value < MinimumCreditHours || value > MaximumCreditHours)
                {
                    throw new ArgumentOutOfRangeException("value",
                        string.Format("Credit hours must be between {0} and {1}.",
                            MinimumCreditHours, MaximumCreditHours));
                }

                this.creditHours = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a core subject taken by every
        /// student regardless of track.
        /// </summary>
        public bool IsCore
        {
            get { return this.isCore; }
            set { this.isCore = value; }
        }

        /// <summary>Gets or sets the description of the course.</summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        /// <summary>
        /// Gets or sets the owning track. Null means the course is a core subject.
        /// </summary>
        public Specialization Specialization
        {
            get { return this.specialization; }
            set { this.specialization = value; }
        }

        /// <summary>Returns the catalogue code and title as one string for display.</summary>
        public string GetDisplayName()
        {
            return this.code + " - " + this.title;
        }

        /// <summary>
        /// Returns true when this course is a core subject shared by every track.
        /// </summary>
        public bool IsCoreSubject()
        {
            return this.isCore;
        }

        /// <summary>Returns the catalogue code and title.</summary>
        public override string ToString()
        {
            return this.GetDisplayName();
        }
    }
}
