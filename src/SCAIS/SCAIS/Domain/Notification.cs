using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// A message written for a user, for example when their course plan is approved.
    /// </summary>
    /// <remarks>
    /// Notifications are produced by the Observer participants
    /// (<c>StudentNotificationService</c> and <c>AuditLogService</c>) rather than by the
    /// screen that changed the state, which is why the
    /// <see cref="CourseRequest"/> class never needs to know that notifications exist.
    /// </remarks>
    public class Notification
    {
        /// <summary>Primary key of the Notifications row.</summary>
        private readonly int notificationId;

        /// <summary>The user the message is for.</summary>
        private readonly int userId;

        /// <summary>Backing field for <see cref="Message"/>.</summary>
        private string message;

        /// <summary>When the message was created.</summary>
        private readonly DateTime createdDate;

        /// <summary>Whether the user has read the message.</summary>
        private bool isRead;

        /// <summary>Creates a notification.</summary>
        /// <param name="notificationId">Primary key, or zero when not yet saved.</param>
        /// <param name="userId">The user the message is for.</param>
        /// <param name="message">The message text.</param>
        /// <param name="createdDate">When the message was created.</param>
        /// <param name="isRead">Whether the user has already read it.</param>
        public Notification(int notificationId, int userId, string message, DateTime createdDate, bool isRead)
        {
            this.notificationId = notificationId;
            this.userId = userId;
            this.Message = message;
            this.createdDate = createdDate;
            this.isRead = isRead;
        }

        /// <summary>Creates an unread notification dated now.</summary>
        /// <param name="userId">The user the message is for.</param>
        /// <param name="message">The message text.</param>
        public Notification(int userId, string message)
            : this(0, userId, message, DateTime.Now, false)
        {
        }

        /// <summary>Gets the primary key of the notification.</summary>
        public int NotificationId
        {
            get { return this.notificationId; }
        }

        /// <summary>Gets the user the message is for.</summary>
        public int UserId
        {
            get { return this.userId; }
        }

        /// <summary>
        /// Gets or sets the message text. A blank message is rejected because a
        /// notification with no text tells the user nothing.
        /// </summary>
        public string Message
        {
            get { return this.message; }

            set
            {
                if (string.IsNullOrEmpty(value) || value.Trim().Length == 0)
                {
                    throw new ArgumentException("A notification must have a message.", "value");
                }

                this.message = value.Trim();
            }
        }

        /// <summary>Gets when the message was created.</summary>
        public DateTime CreatedDate
        {
            get { return this.createdDate; }
        }

        /// <summary>Gets a value indicating whether the user has read the message.</summary>
        public bool IsRead
        {
            get { return this.isRead; }
        }

        /// <summary>Marks the message as read.</summary>
        public void MarkAsRead()
        {
            this.isRead = true;
        }

        /// <summary>Returns the creation date and the message.</summary>
        public override string ToString()
        {
            return this.createdDate.ToString("yyyy-MM-dd HH:mm") + " - " + this.message;
        }
    }
}
