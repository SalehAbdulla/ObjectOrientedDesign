using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// An administrator who maintains the course catalogue, user accounts and the
    /// prerequisite rules of SCAIS.
    /// </summary>
    /// <remarks>
    /// An <c>Administrator</c> is a <see cref="User"/>. The state added here is the
    /// access level and the capability flags that describe what the account is allowed
    /// to maintain.
    ///
    /// An administrator deliberately <b>cannot</b> approve course plans
    /// (<see cref="CanApproveCourseRequests"/> returns false). Approval is an academic
    /// judgement that belongs to the adviser, so separating the two is a deliberate
    /// assignment of responsibility rather than an omission. Because the capability is
    /// exposed through the abstract base member, the user interface disables the
    /// approval action without containing a single test for the role.
    /// </remarks>
    public class Administrator : User
    {
        /// <summary>Primary key of the Administrators row.</summary>
        private readonly int adminId;

        /// <summary>Backing field for <see cref="AccessLevel"/>.</summary>
        private string accessLevel;

        /// <summary>Creates an administrator account.</summary>
        public Administrator(int userId, string username, string passwordHash, string fullName, string email,
                             bool isActive, int adminId, string accessLevel)
            : base(userId, username, passwordHash, fullName, email, UserRole.Administrator, isActive)
        {
            this.adminId = adminId;
            this.AccessLevel = accessLevel;
        }

        /// <summary>Gets the primary key of the administrator record.</summary>
        public int AdminId
        {
            get { return this.adminId; }
        }

        /// <summary>
        /// Gets or sets the access level. A blank level is rejected so the account
        /// always states what it may do.
        /// </summary>
        public string AccessLevel
        {
            get { return this.accessLevel; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("An administrator must have an access level.", "value");
                }

                this.accessLevel = value.Trim();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this administrator may create, edit and
        /// deactivate user accounts.
        /// </summary>
        public bool CanManageUsers
        {
            get
            {
                return string.Equals(this.accessLevel, "Full", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(this.accessLevel, "Registrar", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this administrator may add and edit courses
        /// and their prerequisite rules.
        /// </summary>
        public bool CanManageCatalogue
        {
            get
            {
                return string.Equals(this.accessLevel, "Full", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(this.accessLevel, "Catalog", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Describes the account for the shell header. Produced by this subclass, so the
        /// same call on a <see cref="User"/> reference gives different text per role.
        /// </summary>
        public override string GetProfileSummary()
        {
            return string.Format("Administrator - access level {0}", this.accessLevel);
        }

        /// <summary>Gets the dashboard title for an administrator.</summary>
        public override string GetDashboardTitle()
        {
            return "Administrator Dashboard";
        }

        /// <summary>
        /// Administrators maintain the catalogue and the accounts but do not approve
        /// academic plans, which is the adviser's responsibility.
        /// </summary>
        public override bool CanApproveCourseRequests
        {
            get { return false; }
        }

        /// <summary>Returns the administrator name together with the access level.</summary>
        public override string ToString()
        {
            return this.FullName + " - " + this.accessLevel;
        }
    }
}
