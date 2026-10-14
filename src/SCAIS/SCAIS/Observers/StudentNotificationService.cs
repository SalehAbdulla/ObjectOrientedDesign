using System;
using SCAIS.DataAccess;
using SCAIS.Domain;
using SCAIS.Interfaces;

namespace SCAIS.Observers
{
    /// <summary>
    /// Writes a message for the student whenever one of their course plans changes state.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Observer (a Concrete Observer).</b></para>
    /// <para>
    /// This class exists because of a genuine requirement: the student must be told when
    /// their plan is approved or rejected. Before the pattern was applied,
    /// <c>CourseRequest.Approve</c> would have had to call a notification method
    /// directly, which would have forced a domain class to know about the notification
    /// table and about the user interface. Instead the request simply announces that its
    /// status changed, and this observer reacts.
    /// </para>
    /// <para>
    /// The practical benefit is visible in the class list: adding a second way of
    /// telling the student - an e-mail, a desktop toast, an SMS - means adding a class
    /// that implements <see cref="IUserObserver"/>, and no existing class is modified.
    /// </para>
    /// <para>
    /// The observer deliberately catches nothing. It is the subject's
    /// <c>NotifyObservers</c> method that guards the loop, so a failure here cannot
    /// prevent the other observers from being told.
    /// </para>
    /// </remarks>
    public class StudentNotificationService : IUserObserver
    {
        /// <summary>The repository used to store the message.</summary>
        private readonly NotificationRepository repository;

        /// <summary>Creates the observer with its own repository.</summary>
        public StudentNotificationService()
            : this(new NotificationRepository())
        {
        }

        /// <summary>Creates the observer with a supplied repository.</summary>
        /// <param name="repository">The repository to write messages through.</param>
        public StudentNotificationService(NotificationRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            this.repository = repository;
        }

        /// <summary>
        /// Builds a sentence describing what happened and stores it against the student.
        /// </summary>
        /// <param name="request">The plan whose status changed.</param>
        /// <param name="newStatus">The status the plan now has.</param>
        public void OnRequestStatusChanged(CourseRequest request, RequestStatus newStatus)
        {
            if (request == null)
            {
                return;
            }

            string message = BuildMessage(request, newStatus);

            // The notification is addressed to the student's account, which is the Users
            // key rather than the Students key. The mapping is resolved from the database
            // rather than assumed from the seed data.
            int userId = new UserRepository().GetUserIdForStudent(request.StudentId);

            if (userId <= 0)
            {
                return;
            }

            this.repository.Add(new Notification(userId, message));
        }

        /// <summary>
        /// Turns a status change into the sentence the student reads.
        /// </summary>
        /// <param name="request">The plan whose status changed.</param>
        /// <param name="newStatus">The new status.</param>
        /// <returns>The message text.</returns>
        public static string BuildMessage(CourseRequest request, RequestStatus newStatus)
        {
            string reference = "Course plan #" + request.RequestId;

            switch (newStatus)
            {
                case RequestStatus.Submitted:
                    return reference + " was submitted to your adviser for review.";

                case RequestStatus.Approved:
                    return reference + " was approved."
                        + (string.IsNullOrEmpty(request.AdviserRemark) ? string.Empty : " Remark: " + request.AdviserRemark);

                case RequestStatus.Rejected:
                    return reference + " was rejected."
                        + (string.IsNullOrEmpty(request.AdviserRemark) ? string.Empty : " Reason: " + request.AdviserRemark);

                default:
                    return reference + " was saved as a draft.";
            }
        }
    }
}
