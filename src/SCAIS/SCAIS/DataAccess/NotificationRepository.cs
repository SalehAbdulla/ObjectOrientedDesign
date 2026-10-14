using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the messages the Observer participants produce.
    /// </summary>
    /// <remarks>
    /// This repository is what makes the Observer pattern visible in the data. When a
    /// plan changes status, <c>StudentNotificationService</c> writes through this class
    /// without the <see cref="CourseRequest"/> subject ever knowing that a notification
    /// table exists.
    /// </remarks>
    public class NotificationRepository : SqlRepositoryBase<Notification>
    {
        /// <summary>The shared projection of a notification.</summary>
        private const string SelectSql =
            "SELECT NotificationId, UserId, Message, CreatedDate, IsRead FROM dbo.Notifications";

        /// <inheritdoc />
        public override Notification GetById(int id)
        {
            List<Notification> matches = this.Query(
                SelectSql + " WHERE NotificationId = @Id", MapNotification, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Notification> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY CreatedDate DESC, NotificationId DESC", MapNotification);
        }

        /// <summary>
        /// Returns the messages for one user, most recent first. This is what the
        /// student's dashboard shows after their plan is approved.
        /// </summary>
        /// <param name="userId">The user whose messages are wanted.</param>
        /// <returns>The messages for that user.</returns>
        public List<Notification> GetForUser(int userId)
        {
            return this.Query(
                SelectSql + " WHERE UserId = @UserId ORDER BY CreatedDate DESC, NotificationId DESC",
                MapNotification,
                Param("@UserId", userId));
        }

        /// <summary>Returns the unread messages for one user.</summary>
        /// <param name="userId">The user whose messages are wanted.</param>
        /// <returns>The unread messages for that user.</returns>
        public List<Notification> GetUnreadForUser(int userId)
        {
            return this.Query(
                SelectSql + " WHERE UserId = @UserId AND IsRead = 0 ORDER BY CreatedDate DESC",
                MapNotification,
                Param("@UserId", userId));
        }

        /// <summary>Counts the unread messages for one user.</summary>
        /// <param name="userId">The user to count for.</param>
        /// <returns>The number of unread messages.</returns>
        public int CountUnread(int userId)
        {
            List<int> matches = this.Query(
                "SELECT COUNT(*) AS Matches FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0;",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@UserId", userId));

            return matches.Count == 0 ? 0 : matches[0];
        }

        /// <inheritdoc />
        public override bool Add(Notification entity)
        {
            return this.ExecuteInsert(
                @"INSERT INTO dbo.Notifications (UserId, Message, CreatedDate, IsRead)
                  VALUES (@UserId, @Message, @CreatedDate, @IsRead);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@UserId", entity.UserId),
                Param("@Message", entity.Message),
                Param("@CreatedDate", entity.CreatedDate),
                Param("@IsRead", entity.IsRead)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(Notification entity)
        {
            return this.Execute(
                "UPDATE dbo.Notifications SET Message = @Message, IsRead = @IsRead WHERE NotificationId = @Id;",
                Param("@Message", entity.Message),
                Param("@IsRead", entity.IsRead),
                Param("@Id", entity.NotificationId)) > 0;
        }

        /// <summary>Marks one message as read.</summary>
        /// <param name="notificationId">The message to mark.</param>
        /// <returns>True when the row was updated.</returns>
        public bool MarkAsRead(int notificationId)
        {
            return this.Execute("UPDATE dbo.Notifications SET IsRead = 1 WHERE NotificationId = @Id;",
                Param("@Id", notificationId)) > 0;
        }

        /// <summary>Marks every message of one user as read.</summary>
        /// <param name="userId">The user whose messages should be marked.</param>
        /// <returns>The number of rows updated.</returns>
        public int MarkAllAsRead(int userId)
        {
            return this.Execute("UPDATE dbo.Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0;",
                Param("@UserId", userId));
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Notifications WHERE NotificationId = @Id;",
                Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to a notification.</summary>
        private static Notification MapNotification(SqlDataReader reader)
        {
            return new Notification(
                ToInt(reader, "NotificationId"),
                ToInt(reader, "UserId"),
                ToText(reader, "Message"),
                ToNullableDate(reader, "CreatedDate") ?? DateTime.Now,
                ToBool(reader, "IsRead"));
        }
    }
}
