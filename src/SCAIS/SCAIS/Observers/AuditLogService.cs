using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Observers
{
    /// <summary>
    /// Records an audit line every time a course plan changes state.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Observer (a second Concrete Observer).</b></para>
    /// <para>
    /// This class is the reason the Observer pattern earns its place rather than being
    /// decoration. It reacts to exactly the same event as
    /// <see cref="StudentNotificationService"/> but does something completely different:
    /// it writes an audit trail rather than a message for the student. The subject does
    /// not distinguish between the two, and neither observer knows the other exists.
    /// </para>
    /// <para>
    /// If the requirement later changed to "every approval must also be e-mailed to the
    /// registrar", the change would be one new class plus one registration line - no
    /// change to <see cref="CourseRequest"/> and no change here.
    /// </para>
    /// </remarks>
    public class AuditLogService : IUserObserver
    {
        /// <summary>The name of the audit file written next to the executable.</summary>
        public const string LogFileName = "SCAIS-audit.log";

        /// <summary>Guards concurrent writes to the file.</summary>
        private static readonly object WriteLock = new object();

        /// <summary>
        /// Writes one audit line describing the status change.
        /// </summary>
        /// <param name="request">The plan whose status changed.</param>
        /// <param name="newStatus">The status the plan now has.</param>
        public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
        {
            if (request == null)
            {
                return;
            }

            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss} | request {1} | student {2} | {3} | courses {4} | credits {5}",
                DateTime.Now,
                request.RequestId,
                request.StudentId,
                newStatus,
                request.GetCourseCount(),
                request.GetTotalCreditHours());

            // Always emit to the trace listener so the line is visible while debugging.
            Trace.WriteLine(line);

            WriteToFile(line);
        }

        /// <summary>
        /// Appends a line to the audit file. A failure to write is swallowed on purpose:
        /// an audit trail that cannot be written must never stop a student's plan from
        /// being approved.
        /// </summary>
        /// <param name="line">The line to append.</param>
        private static void WriteToFile(string line)
        {
            try
            {
                string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (string.IsNullOrEmpty(folder))
                {
                    return;
                }

                lock (WriteLock)
                {
                    File.AppendAllText(Path.Combine(folder, LogFileName), line + Environment.NewLine);
                }
            }
            catch (IOException)
            {
                // The folder may be read-only. The trace listener still has the line.
            }
            catch (UnauthorizedAccessException)
            {
                // The folder may be read-only. The trace listener still has the line.
            }
        }
    }
}
