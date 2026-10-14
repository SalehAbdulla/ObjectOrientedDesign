using System;
using SCAIS.DataAccess;
using SCAIS.Domain;

namespace SCAIS.Business
{
    /// <summary>
    /// The outcome of an attempt to sign in.
    /// </summary>
    /// <remarks>
    /// A result object is returned instead of throwing or returning a bare boolean
    /// because the login screen needs three pieces of information: whether it worked,
    /// which account signed in, and what to tell the person if it did not. A nullable
    /// <see cref="User"/> would have lost the reason, and throwing would have made an
    /// ordinary wrong password look like a fault.
    /// </remarks>
    public class LoginResult
    {
        /// <summary>Whether the attempt succeeded.</summary>
        private readonly bool succeeded;

        /// <summary>The account that signed in, or null.</summary>
        private readonly User user;

        /// <summary>The message to show the person.</summary>
        private readonly string message;

        /// <summary>Creates a result.</summary>
        /// <param name="succeeded">Whether the attempt succeeded.</param>
        /// <param name="user">The account that signed in, or null.</param>
        /// <param name="message">The message to show.</param>
        public LoginResult(bool succeeded, User user, string message)
        {
            this.succeeded = succeeded;
            this.user = user;
            this.message = message ?? string.Empty;
        }

        /// <summary>Gets a value indicating whether the attempt succeeded.</summary>
        public bool Succeeded
        {
            get { return this.succeeded; }
        }

        /// <summary>Gets the account that signed in, or null.</summary>
        public User User
        {
            get { return this.user; }
        }

        /// <summary>Gets the message to show the person.</summary>
        public string Message
        {
            get { return this.message; }
        }

        /// <summary>Creates a successful result.</summary>
        /// <param name="user">The account that signed in.</param>
        /// <returns>A successful result.</returns>
        public static LoginResult Success(User user)
        {
            return new LoginResult(true, user, "Signed in as " + user.FullName + ".");
        }

        /// <summary>Creates a failed result.</summary>
        /// <param name="message">The reason to show.</param>
        /// <returns>A failed result.</returns>
        public static LoginResult Failure(string message)
        {
            return new LoginResult(false, null, message);
        }
    }

    /// <summary>
    /// Verifies credentials and starts a session. This is the only class in SCAIS that
    /// is allowed to ask an account whether a password is correct.
    /// </summary>
    /// <remarks>
    /// The service never says whether it was the username or the password that was
    /// wrong. Telling the person which half was wrong would let anyone enumerate valid
    /// usernames, so a single message covers both cases.
    /// </remarks>
    public class AuthenticationService
    {
        /// <summary>The repository used to find accounts.</summary>
        private readonly UserRepository repository;

        /// <summary>Creates the service with its own repository.</summary>
        public AuthenticationService()
            : this(new UserRepository())
        {
        }

        /// <summary>Creates the service with a supplied repository.</summary>
        /// <param name="repository">The repository to look accounts up in.</param>
        public AuthenticationService(UserRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
        }

        /// <summary>
        /// Verifies the supplied credentials and, when they are correct, records the
        /// session so that the rest of the application knows who is signed in.
        /// </summary>
        /// <param name="username">The sign-in name.</param>
        /// <param name="password">The plain text password.</param>
        /// <returns>The outcome of the attempt.</returns>
        public LoginResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return LoginResult.Failure("Enter both your username and your password.");
            }

            User user = this.repository.GetByUsername(username.Trim());

            // The same message is used for an unknown account and a wrong password.
            if (user == null)
            {
                return LoginResult.Failure("The username or password is not correct.");
            }

            if (!user.IsActive)
            {
                return LoginResult.Failure(
                    "This account has been deactivated. Please contact the administrator.");
            }

            if (!user.Authenticate(password))
            {
                return LoginResult.Failure("The username or password is not correct.");
            }

            SessionManager.SignIn(user);
            return LoginResult.Success(user);
        }

        /// <summary>
        /// Changes an account's password after checking the current one.
        /// </summary>
        /// <param name="userId">The account to change.</param>
        /// <param name="currentPassword">The password currently in force.</param>
        /// <param name="newPassword">The password to move to.</param>
        /// <returns>The outcome of the change.</returns>
        public LoginResult ChangePassword(int userId, string currentPassword, string newPassword)
        {
            User user = this.repository.GetById(userId);

            if (user == null)
            {
                return LoginResult.Failure("The account could not be found.");
            }

            if (!user.Authenticate(currentPassword))
            {
                return LoginResult.Failure("The current password is not correct.");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return LoginResult.Failure("The new password must be at least six characters long.");
            }

            // The domain object validates and hashes, and the repository only stores the
            // result. The plain text password is never written anywhere.
            user.SetPassword(newPassword);

            if (!this.repository.UpdatePassword(userId, newPassword))
            {
                return LoginResult.Failure("The new password could not be saved.");
            }

            return LoginResult.Success(user);
        }

        /// <summary>
        /// Signs the current account out of the session.
        /// </summary>
        public void Logout()
        {
            SessionManager.SignOut();
        }
    }
}
