using SCAIS.Domain;

namespace SCAIS.Interfaces
{
    /// <summary>
    /// The contract every object must satisfy in order to be told that a course request
    /// has changed status.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Observer (the Observer interface).</b></para>
    /// <para>
    /// The interface exists so that the <see cref="CourseRequest"/> subject does not
    /// have to know which concrete objects are listening. It only knows that they can
    /// answer <see cref="OnRequestStatusChanged"/>. That is what allows a new reaction -
    /// for example sending an e-mail, or writing to an external audit system - to be
    /// added as a new class without editing the subject at all.
    /// </para>
    /// <para>
    /// An interface is the right tool here rather than a base class because the
    /// observers are unrelated types: <c>StudentNotificationService</c> writes to the
    /// database and <c>AuditLogService</c> writes to a log. Neither "is a" kind of
    /// anything in common, so inheritance would be wrong and the shared contract is the
    /// only thing they genuinely have in common.
    /// </para>
    /// <para>
    /// Implementations must not throw. The subject guards the notification loop, but an
    /// observer that fails repeatedly is a defect.
    /// </para>
    /// </remarks>
    public interface IUserObserver
    {
        /// <summary>
        /// Called by the subject after the status of a request has changed.
        /// </summary>
        /// <param name="request">The request whose status changed.</param>
        /// <param name="newStatus">The status the request now has.</param>
        void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus);
    }
}
