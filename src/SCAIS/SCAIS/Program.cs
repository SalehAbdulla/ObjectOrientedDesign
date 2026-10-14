using System;
using System.Windows.Forms;
using SCAIS.DataAccess;
using SCAIS.Domain;
using SCAIS.UI;

namespace SCAIS
{
    /// <summary>
    /// The entry point of the SCAIS desktop application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The start-up sequence is deliberately short and does three things in order:
    /// prepare the database, show the single login screen, then hand the signed-in
    /// account to the dashboard factory.
    /// </para>
    /// <para>
    /// The database is prepared <b>before</b> the login form appears. That ordering
    /// matters: the login form is the first thing that reads the Users table, so if the
    /// database were prepared lazily the very first query on a new machine would fail
    /// with a confusing error instead of the system simply creating what it needs.
    /// </para>
    /// </remarks>
    internal static class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Catch anything that escapes a screen, so the application reports a message
            // instead of disappearing.
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            if (!PrepareDatabase())
            {
                return;
            }

            User authenticatedUser = ShowLogin();

            if (authenticatedUser == null)
            {
                return;
            }

            // The factory decides which concrete dashboard the account needs. That is the
            // single place in the user interface where the role is inspected.
            using (Form dashboard = DashboardFactory.Create(authenticatedUser))
            {
                Application.Run(dashboard);
            }
        }

        /// <summary>
        /// Creates the database and its seed data when they are missing.
        /// </summary>
        /// <returns>True when the database is ready to be used.</returns>
        private static bool PrepareDatabase()
        {
            try
            {
                DatabaseManager.Instance.EnsureDatabase();
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "SCAIS could not prepare its database.\r\n\r\n" + exception.Message +
                    "\r\n\r\nThe connection string is in App.config. SQL Server LocalDB must be " +
                    "installed, which it is on any machine with Visual Studio.",
                    "SCAIS - database error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }

        /// <summary>
        /// Shows the single login screen and returns the account that signed in.
        /// </summary>
        /// <returns>The signed-in account, or null when the user cancelled.</returns>
        private static User ShowLogin()
        {
            using (frmLogin login = new frmLogin())
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return null;
                }

                return login.AuthenticatedUser;
            }
        }

        /// <summary>Reports an exception raised on the user interface thread.</summary>
        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            ReportFailure(e.Exception);
        }

        /// <summary>Reports an exception that escaped the message loop.</summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportFailure(e.ExceptionObject as Exception);
        }

        /// <summary>Shows one consistent message for an unexpected failure.</summary>
        private static void ReportFailure(Exception exception)
        {
            string detail = exception == null ? "No detail was available." : exception.Message;

            MessageBox.Show(
                "SCAIS has encountered an unexpected problem.\r\n\r\n" + detail,
                "SCAIS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
