using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// The base type for every account that can sign in to SCAIS.
    /// </summary>
    /// <remarks>
    /// <para><b>Abstraction.</b> A <c>User</c> exposes only what a caller needs in
    /// order to work with an account: its identity, whether it is active, and the
    /// three operations that differ by role. The storage of the credential
    /// (the password hash field) is not exposed at all.</para>
    ///
    /// <para><b>Inheritance.</b> The class is abstract because there is no such thing
    /// as a generic user in SCAIS: every row in the Users table belongs to exactly one
    /// of the three concrete roles. Identity, credentials, activation state and
    /// authentication are implemented once here and reused by all three children,
    /// while the role-specific behaviour is deferred to them.</para>
    ///
    /// <para><b>Polymorphism.</b> The single login form holds a <c>User</c> reference.
    /// It calls <see cref="GetDashboardTitle"/>, <see cref="GetProfileSummary"/> and
    /// <see cref="CanApproveCourseRequests"/> without ever testing which subclass it
    /// has, and each subclass produces different behaviour for the same call.</para>
    ///
    /// <para><b>Encapsulation.</b> <see cref="FullName"/> and <see cref="Email"/> are
    /// validated by their setters, <see cref="UserId"/>, <see cref="Username"/> and
    /// <see cref="Role"/> are read-only, and the password hash has no public accessor
    /// whatsoever: the only way in is <see cref="SetPassword"/> and the only way to
    /// check a credential is <see cref="Authenticate"/>. An object of this type can
    /// therefore never hold a blank name, an invalid e-mail address, or a password in
    /// plain text.</para>
    /// </remarks>
    public abstract class User
    {
        /// <summary>Database identity of the account. Read-only.</summary>
        private readonly int userId;

        /// <summary>Sign-in name of the account. Read-only after construction.</summary>
        private readonly string username;

        /// <summary>Hashed credential. Never exposed and never returned.</summary>
        private string passwordHash;

        /// <summary>Backing field for <see cref="FullName"/>.</summary>
        private string fullName;

        /// <summary>Backing field for <see cref="Email"/>.</summary>
        private string email;

        /// <summary>Backing field for <see cref="IsActive"/>.</summary>
        private bool isActive;

        /// <summary>The role this account belongs to. Read-only.</summary>
        private readonly UserRole role;

        /// <summary>
        /// Initialises the shared state of an account. Called by the subclasses.
        /// </summary>
        /// <param name="userId">Database identity, or zero for an account not yet saved.</param>
        /// <param name="username">Sign-in name.</param>
        /// <param name="passwordHash">Already hashed credential.</param>
        /// <param name="fullName">Display name of the person.</param>
        /// <param name="email">Contact e-mail address.</param>
        /// <param name="role">The role this account belongs to.</param>
        /// <param name="isActive">Whether the account may sign in.</param>
        protected User(int userId, string username, string passwordHash, string fullName,
                       string email, UserRole role, bool isActive)
        {
            this.userId = userId;
            this.username = username;
            this.passwordHash = passwordHash;
            this.role = role;
            this.isActive = isActive;

            // Routed through the properties so the validation rules always apply.
            this.FullName = fullName;
            this.Email = email;
        }

        /// <summary>Gets the database identity of the account.</summary>
        public int UserId
        {
            get { return this.userId; }
        }

        /// <summary>Gets the sign-in name of the account.</summary>
        public string Username
        {
            get { return this.username; }
        }

        /// <summary>Gets the role this account belongs to.</summary>
        public UserRole Role
        {
            get { return this.role; }
        }

        /// <summary>
        /// Gets or sets the display name. A blank name is rejected so the account can
        /// never be left without one.
        /// </summary>
        public string FullName
        {
            get { return this.fullName; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A user must have a full name.", "value");
                }

                this.fullName = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets the e-mail address. The setter rejects a value that does not
        /// look like an address.
        /// </summary>
        public string Email
        {
            get { return this.email; }

            set
            {
                if (!IsValidEmail(value))
                {
                    throw new ArgumentException("A user must have a valid e-mail address.", "value");
                }

                this.email = value.Trim();
            }
        }

        /// <summary>
        /// Gets or sets whether the account may sign in. Deactivated accounts are
        /// rejected by <see cref="Authenticate"/>.
        /// </summary>
        public bool IsActive
        {
            get { return this.isActive; }
            set { this.isActive = value; }
        }

        /// <summary>
        /// Gets the stored password hash so that the data layer can persist it.
        /// </summary>
        /// <remarks>
        /// This member is <c>internal</c>, not <c>public</c>. Inside the SCAIS assembly
        /// the repository can read the credential in order to write it to the database,
        /// but any code outside the assembly - and every screen in the user interface -
        /// cannot see it at all. That is the third level of encapsulation in this class:
        /// <c>private</c> for the validation helpers, <c>internal</c> for persistence,
        /// and <c>public</c> for the behaviour the rest of the system is allowed to use.
        /// </remarks>
        internal string PasswordHashForPersistence
        {
            get { return this.passwordHash; }
        }

        /// <summary>
        /// Gets a human readable description of the account, produced differently by
        /// each role. This is the polymorphic member used by the shell header.
        /// </summary>
        public abstract string GetProfileSummary();

        /// <summary>
        /// Gets the title of the dashboard that this role should be shown after
        /// signing in. This is the polymorphic member that drives the role redirect.
        /// </summary>
        public abstract string GetDashboardTitle();

        /// <summary>
        /// Gets a value indicating whether this role is allowed to approve or reject
        /// course plans. This is the polymorphic member used to enable the approval
        /// action, so the user interface never has to test the role itself.
        /// </summary>
        public abstract bool CanApproveCourseRequests { get; }

        /// <summary>
        /// Checks a plain text password against the stored credential.
        /// The hash is never returned to the caller.
        /// </summary>
        /// <param name="plainPassword">The password supplied by the person signing in.</param>
        /// <returns>True when the password is correct and the account is active.</returns>
        public bool Authenticate(string plainPassword)
        {
            if (!this.isActive || string.IsNullOrEmpty(plainPassword))
            {
                return false;
            }

            return PasswordHasher.Verify(plainPassword, this.passwordHash);
        }

        /// <summary>
        /// Replaces the credential. The plain text password is hashed immediately and
        /// is never retained.
        /// </summary>
        /// <param name="plainPassword">The new plain text password.</param>
        public void SetPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || plainPassword.Length < 6)
            {
                throw new ArgumentException("A password must be at least six characters long.", "plainPassword");
            }

            this.passwordHash = PasswordHasher.Hash(plainPassword);
        }

        /// <summary>
        /// Returns the display name of the account together with its sign-in name.
        /// </summary>
        public override string ToString()
        {
            return this.fullName + " (" + this.username + ")";
        }

        /// <summary>
        /// Simple structural check that the value looks like an e-mail address.
        /// </summary>
        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            int atIndex = trimmed.IndexOf('@');

            return atIndex > 0
                && atIndex < trimmed.Length - 1
                && trimmed.IndexOf('.', atIndex) > atIndex + 1
                && trimmed.IndexOf(' ') < 0;
        }
    }
}
