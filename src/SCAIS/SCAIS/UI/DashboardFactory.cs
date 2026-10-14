using System;
using System.Windows.Forms;
using SCAIS.Domain;

namespace SCAIS.UI
{
    /// <summary>
    /// Creates the dashboard that matches the role of the account that signed in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the only place in the user interface that inspects the role, and that is
    /// deliberate. Somebody has to map "this account is an adviser" to "open the adviser
    /// dashboard", and concentrating that decision in one factory is better than
    /// scattering role tests through the screens.
    /// </para>
    /// <para>
    /// <b>Why a factory rather than a polymorphic method on the domain.</b> A method such
    /// as <c>User.CreateDashboard()</c> would have made the domain model depend on Windows
    /// Forms, which would be a serious design error: the business case states that web and
    /// mobile front ends are planned, and a domain class that constructs a desktop form
    /// could never be reused by them. The role specific behaviour that genuinely belongs
    /// in the domain - the dashboard title, the profile summary and whether the account
    /// may approve plans - is polymorphic on <see cref="User"/>; only the choice of form
    /// lives here.
    /// </para>
    /// <para>
    /// The role is read from the account rather than passed in, so a caller cannot ask
    /// for a dashboard that contradicts the account it holds.
    /// </para>
    /// </remarks>
    public static class DashboardFactory
    {
        /// <summary>
        /// Creates the dashboard for an account.
        /// </summary>
        /// <param name="user">The account that signed in.</param>
        /// <returns>The dashboard form for that account's role.</returns>
        /// <exception cref="ArgumentNullException">When the account is null.</exception>
        /// <exception cref="NotSupportedException">When the role is not recognised.</exception>
        public static Form Create(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            switch (user.Role)
            {
                case UserRole.Administrator:
                    return new frmAdminDashboard((Administrator)user);

                case UserRole.Adviser:
                    return new frmAdviserDashboard((Adviser)user);

                case UserRole.Student:
                    return new frmStudentDashboard((Student)user);

                default:
                    throw new NotSupportedException(
                        "No dashboard is defined for the role " + user.Role + ".");
            }
        }
    }
}
