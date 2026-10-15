using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// One row of the timetable: a course that runs in a particular academic term, and the
    /// number of places it has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this class exists.</b> The catalogue says what may be taught; a semester says
    /// when teaching happens; neither of them says which course actually runs in which term.
    /// Without that third fact the administrator cannot control the timetable, and the system
    /// cannot tell a student that a course is not running this term. The
    /// <c>CourseOfferings</c> table holds the fact, and this class is its object.
    /// </para>
    /// <para>
    /// <b>Encapsulation.</b> <see cref="OfferingId"/> is read-only because the database
    /// assigns it. <see cref="Capacity"/> validates itself against
    /// <see cref="MinimumCapacity"/> and <see cref="MaximumCapacity"/> so an offering with
    /// zero or absurd capacity cannot exist, and <see cref="Course"/> and
    /// <see cref="Semester"/> reject null because an offering without both is meaningless.
    /// </para>
    /// <para>
    /// <b>Why two objects rather than two keys.</b> An offering holds a
    /// <see cref="Domain.Course"/> and a <see cref="Domain.Semester"/> rather than a
    /// <c>CourseId</c> and a <c>SemesterId</c>. That is what lets a screen display
    /// "IT221 - Networking and Data Communications 2 in Semester 2, 2026" without issuing a
    /// second query, and it is the same decision the enrolment record already makes.
    /// </para>
    /// <para>
    /// <see cref="Enrolled"/> is deliberately <b>not</b> a column of the table. It is read
    /// from <c>Enrollments</c> by the repository, because the number of students taking a
    /// course is a fact about enrolments and storing it twice would let the two copies
    /// disagree.
    /// </para>
    /// </remarks>
    public class CourseOffering
    {
        /// <summary>Smallest permitted value for <see cref="Capacity"/>.</summary>
        public const int MinimumCapacity = 1;

        /// <summary>Largest permitted value for <see cref="Capacity"/>.</summary>
        public const int MaximumCapacity = 500;

        /// <summary>The capacity assumed when an administrator does not supply one.</summary>
        public const int DefaultCapacity = 30;

        /// <summary>Primary key of the CourseOfferings row.</summary>
        private readonly int offeringId;

        /// <summary>Backing field for <see cref="Course"/>.</summary>
        private Course course;

        /// <summary>Backing field for <see cref="Semester"/>.</summary>
        private Semester semester;

        /// <summary>Backing field for <see cref="Capacity"/>.</summary>
        private int capacity;

        /// <summary>Backing field for <see cref="Enrolled"/>.</summary>
        private int enrolled;

        /// <summary>Creates an offering.</summary>
        /// <param name="offeringId">Primary key, or zero when not yet saved.</param>
        /// <param name="course">The course that runs. Must not be null.</param>
        /// <param name="semester">The term it runs in. Must not be null.</param>
        /// <param name="capacity">The number of places, between 1 and 500.</param>
        /// <exception cref="ArgumentNullException">When the course or the term is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the capacity is out of range.</exception>
        public CourseOffering(int offeringId, Course course, Semester semester, int capacity)
        {
            if (course == null)
            {
                throw new ArgumentNullException("course");
            }

            if (semester == null)
            {
                throw new ArgumentNullException("semester");
            }

            this.offeringId = offeringId;
            this.course = course;
            this.semester = semester;
            this.Capacity = capacity;
        }

        /// <summary>Gets the primary key of the offering.</summary>
        public int OfferingId
        {
            get { return this.offeringId; }
        }

        /// <summary>Gets or sets the course that runs.</summary>
        public Course Course
        {
            get { return this.course; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "An offering must name a course.");
                }

                this.course = value;
            }
        }

        /// <summary>Gets or sets the term the course runs in.</summary>
        public Semester Semester
        {
            get { return this.semester; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", "An offering must name a semester.");
                }

                this.semester = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of places. A value outside the permitted range is
        /// rejected, so the system can never publish an offering that no student could take
        /// or that claims more seats than a room could hold.
        /// </summary>
        public int Capacity
        {
            get { return this.capacity; }

            set
            {
                if (value < MinimumCapacity || value > MaximumCapacity)
                {
                    throw new ArgumentOutOfRangeException("value",
                        string.Format("Capacity must be between {0} and {1} places.",
                            MinimumCapacity, MaximumCapacity));
                }

                this.capacity = value;
            }
        }

        /// <summary>
        /// Gets or sets how many students are already enrolled in this course in this term.
        /// </summary>
        /// <remarks>
        /// The repository reads this from the Enrollments table; it is not stored on the
        /// offering itself. Setting it is the repository's job, which is why the setter
        /// exists at all.
        /// </remarks>
        public int Enrolled
        {
            get { return this.enrolled; }
            set { this.enrolled = value < 0 ? 0 : value; }
        }

        /// <summary>Gets the number of places still free, never a negative number.</summary>
        public int SeatsRemaining
        {
            get
            {
                int remaining = this.capacity - this.enrolled;
                return remaining < 0 ? 0 : remaining;
            }
        }

        /// <summary>
        /// Returns true when the offering still has a place for another student.
        /// </summary>
        /// <returns>True when fewer students are enrolled than the capacity allows.</returns>
        public bool HasSpace()
        {
            return this.enrolled < this.capacity;
        }

        /// <summary>
        /// Returns true when at least one student has enrolled, which is the fact that stops
        /// the offering from being withdrawn from the timetable.
        /// </summary>
        /// <returns>True when the enrolment count is greater than zero.</returns>
        public bool HasEnrollments()
        {
            return this.enrolled > 0;
        }

        /// <summary>
        /// Returns the course and the term as one string, for a heading or a confirmation.
        /// </summary>
        /// <returns>For example "IT221 - Networking and Data Communications 2 in Semester 2, 2026".</returns>
        public string GetDisplayName()
        {
            return this.course.GetDisplayName() + " in " + this.semester.GetDisplayName();
        }

        /// <summary>
        /// Returns how full the offering is, in words, for the status line of the screen that
        /// maintains it.
        /// </summary>
        /// <returns>For example "12 of 30 place(s) taken, 18 free".</returns>
        public string GetAvailabilityText()
        {
            return string.Format("{0} of {1} place(s) taken, {2} free",
                this.enrolled, this.capacity, this.SeatsRemaining);
        }

        /// <summary>Returns the course and the term.</summary>
        public override string ToString()
        {
            return this.GetDisplayName();
        }
    }
}
