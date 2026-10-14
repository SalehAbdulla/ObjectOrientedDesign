using System;
using System.Collections.Generic;

namespace SCAIS.Domain
{
    /// <summary>
    /// A faculty member who manages advisees and approves course selections.
    /// </summary>
    /// <remarks>
    /// A <c>Adviser</c> is a <see cref="User"/>: it signs in through the same screen
    /// and reuses the inherited authentication. The state added here is the faculty
    /// data (department and office) and the set of advisees assigned to the adviser.
    ///
    /// The advisees are held as an aggregation, not a composition: a student exists
    /// independently of the adviser and the assignment can be changed by an
    /// administrator at any time, so the student is not owned by the adviser.
    /// </remarks>
    public class Adviser : User
    {
        /// <summary>Primary key of the Advisers row.</summary>
        private readonly int adviserId;

        /// <summary>Backing field for <see cref="Department"/>.</summary>
        private string department;

        /// <summary>Backing field for <see cref="Office"/>.</summary>
        private string office;

        /// <summary>The advisees currently assigned to this adviser.</summary>
        private readonly List<Student> advisees = new List<Student>();

        /// <summary>Creates an adviser account.</summary>
        public Adviser(int userId, string username, string passwordHash, string fullName, string email,
                       bool isActive, int adviserId, string department, string office)
            : base(userId, username, passwordHash, fullName, email, UserRole.Adviser, isActive)
        {
            this.adviserId = adviserId;
            this.Department = department;
            this.Office = office;
        }

        /// <summary>Gets the primary key of the adviser record.</summary>
        public int AdviserId
        {
            get { return this.adviserId; }
        }

        /// <summary>Gets or sets the academic department.</summary>
        public string Department
        {
            get { return this.department; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("An adviser must belong to a department.", "value");
                }

                this.department = value.Trim();
            }
        }

        /// <summary>Gets or sets the office location.</summary>
        public string Office
        {
            get { return this.office; }
            set { this.office = value; }
        }

        /// <summary>
        /// Gets the assigned advisees as a read-only view. The list is changed only
        /// through <see cref="AssignAdvisee"/> and <see cref="RemoveAdvisee"/>.
        /// </summary>
        public IList<Student> Advisees
        {
            get { return this.advisees.AsReadOnly(); }
        }

        /// <summary>
        /// Assigns a student to this adviser, ignoring a student who is already assigned
        /// so the list can never contain duplicates.
        /// </summary>
        /// <param name="student">The student to assign.</param>
        public void AssignAdvisee(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException("student");
            }

            foreach (Student existing in this.advisees)
            {
                if (existing.StudentId == student.StudentId)
                {
                    return;
                }
            }

            this.advisees.Add(student);
            student.Adviser = this;
        }

        /// <summary>
        /// Removes a student from this adviser's list and clears the student's adviser
        /// reference, so the two objects never disagree about the assignment.
        /// </summary>
        /// <param name="student">The student to remove.</param>
        /// <returns>True when the student was assigned and has been removed.</returns>
        public bool RemoveAdvisee(Student student)
        {
            if (student == null)
            {
                return false;
            }

            for (int index = 0; index < this.advisees.Count; index++)
            {
                if (this.advisees[index].StudentId == student.StudentId)
                {
                    this.advisees.RemoveAt(index);
                    student.Adviser = null;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the number of students assigned to this adviser.</summary>
        public int GetAdviseeCount()
        {
            return this.advisees.Count;
        }

        /// <summary>
        /// Describes the account for the shell header. Produced by this subclass, so the
        /// same call on a <see cref="User"/> reference gives different text per role.
        /// </summary>
        public override string GetProfileSummary()
        {
            return string.Format("Adviser - {0} - {1} advisee(s)", this.department, this.advisees.Count);
        }

        /// <summary>Gets the dashboard title for an adviser.</summary>
        public override string GetDashboardTitle()
        {
            return "Adviser Dashboard";
        }

        /// <summary>Advisers are the only role allowed to approve course plans.</summary>
        public override bool CanApproveCourseRequests
        {
            get { return true; }
        }

        /// <summary>Returns the adviser name together with the department.</summary>
        public override string ToString()
        {
            return this.FullName + " - " + this.department;
        }
    }
}
