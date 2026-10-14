using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// An academic term, for example "Semester 1, 2026".
    /// </summary>
    /// <remarks>
    /// The class is deliberately small. It exists so that a semester is a value the
    /// rest of the system can pass around and display, instead of an integer key whose
    /// meaning each screen would have to look up for itself.
    /// </remarks>
    public class Semester
    {
        /// <summary>Primary key of the Semesters row.</summary>
        private readonly int semesterId;

        /// <summary>Backing field for <see cref="TermName"/>.</summary>
        private string termName;

        /// <summary>Backing field for <see cref="AcademicYear"/>.</summary>
        private int academicYear;

        /// <summary>When the term begins.</summary>
        private readonly DateTime? startDate;

        /// <summary>When the term ends.</summary>
        private readonly DateTime? endDate;

        /// <summary>Creates a semester.</summary>
        /// <param name="semesterId">Primary key, or zero when not yet saved.</param>
        /// <param name="termName">The term name, for example "Semester 1".</param>
        /// <param name="academicYear">The academic year.</param>
        /// <param name="startDate">When the term begins, or null.</param>
        /// <param name="endDate">When the term ends, or null.</param>
        public Semester(int semesterId, string termName, int academicYear, DateTime? startDate, DateTime? endDate)
        {
            this.semesterId = semesterId;
            this.TermName = termName;
            this.academicYear = academicYear;
            this.startDate = startDate;
            this.endDate = endDate;
        }

        /// <summary>Gets the primary key of the semester.</summary>
        public int SemesterId
        {
            get { return this.semesterId; }
        }

        /// <summary>
        /// Gets or sets the term name. A blank name is rejected because the name is what
        /// every screen displays.
        /// </summary>
        public string TermName
        {
            get { return this.termName; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A semester must have a term name.", "value");
                }

                this.termName = value.Trim();
            }
        }

        /// <summary>Gets or sets the academic year.</summary>
        public int AcademicYear
        {
            get { return this.academicYear; }
            set { this.academicYear = value; }
        }

        /// <summary>Gets when the term begins.</summary>
        public DateTime? StartDate
        {
            get { return this.startDate; }
        }

        /// <summary>Gets when the term ends.</summary>
        public DateTime? EndDate
        {
            get { return this.endDate; }
        }

        /// <summary>Returns the term and year as one string for display.</summary>
        public string GetDisplayName()
        {
            return this.termName + ", " + this.academicYear;
        }

        /// <summary>Returns the term and year.</summary>
        public override string ToString()
        {
            return this.GetDisplayName();
        }
    }
}
