using System;
using SCAIS.Domain;

namespace SCAIS.Business
{
    /// <summary>
    /// Holds the account that is currently signed in for the lifetime of the application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A desktop application has exactly one signed-in user at a time, and every screen
    /// after the login form needs to know who that is. Passing the account through every
    /// constructor would push the same parameter through a dozen classes and would make
    /// the dashboards responsible for something that is not theirs.
    /// </para>
    /// <para>
    /// The property is deliberately get-only from outside. <see cref="SignIn"/> and
    /// <see cref="SignOut"/> are the only ways the state can change, which mirrors the
    /// encapsulation used in the domain model: the session cannot be put into the
    /// impossible state of being signed in with no account.
    /// </para>
    /// <para>
    /// Unlike <c>DatabaseManager</c>, this class is a plain static holder rather than a
    /// Singleton. The reason is that there is nothing to construct and nothing to
    /// configure - only a reference to remember - so a full pattern implementation would
    /// add code without adding a guarantee.
    /// </para>
    /// </remarks>
    public static class SessionManager
    {
        /// <summary>The account that is signed in, or null when nobody is.</summary>
        private static User currentUser;

        /// <summary>
        /// Gets the account that is signed in, or null when nobody is.
        /// </summary>
        public static User CurrentUser
        {
            get { return currentUser; }
        }

        /// <summary>
        /// Gets a value indicating whether an account is signed in.
        /// </summary>
        public static bool IsSignedIn
        {
            get { return currentUser != null; }
        }

        /// <summary>
        /// Gets the signed-in account as a student, or null when the account is not a
        /// student. The cast is safe because the role was decided by the factory when
        /// the account was loaded.
        /// </summary>
        public static Student CurrentStudent
        {
            get { return currentUser as Student; }
        }

        /// <summary>
        /// Gets the signed-in account as an adviser, or null when the account is not an
        /// adviser.
        /// </summary>
        public static Adviser CurrentAdviser
        {
            get { return currentUser as Adviser; }
        }

        /// <summary>
        /// Gets the signed-in account as an administrator, or null when the account is
        /// not an administrator.
        /// </summary>
        public static Administrator CurrentAdministrator
        {
            get { return currentUser as Administrator; }
        }

        /// <summary>
        /// Records the account that has signed in.
        /// </summary>
        /// <param name="user">The account that signed in.</param>
        /// <exception cref="ArgumentNullException">When the account is null.</exception>
        public static void SignIn(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            currentUser = user;
        }

        /// <summary>
        /// Clears the session.
        /// </summary>
        public static void SignOut()
        {
            currentUser = null;
        }
    }
}
