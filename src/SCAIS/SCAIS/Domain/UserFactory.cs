using System;

namespace SCAIS.Domain
{
    /// <summary>
    /// The flat set of columns that identify one account, read from the Users table
    /// joined to whichever role table applies.
    /// </summary>
    /// <remarks>
    /// This type exists so that <see cref="UserFactory"/> does not have to take a
    /// dozen loose parameters, and so that the data layer can hand over what it read
    /// without the factory needing to know anything about ADO.NET. The fields that do
    /// not apply to a given role are simply left at their defaults.
    /// </remarks>
    public class UserRecord
    {
        /// <summary>Gets or sets the primary key from Users.</summary>
        public int UserId { get; set; }

        /// <summary>Gets or sets the sign-in name.</summary>
        public string Username { get; set; }

        /// <summary>Gets or sets the stored password hash.</summary>
        public string PasswordHash { get; set; }

        /// <summary>Gets or sets the display name.</summary>
        public string FullName { get; set; }

        /// <summary>Gets or sets the e-mail address.</summary>
        public string Email { get; set; }

        /// <summary>Gets or sets the role, which decides which subclass is created.</summary>
        public UserRole Role { get; set; }

        /// <summary>Gets or sets whether the account may sign in.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the primary key of the role row: StudentId, AdviserId or AdminId
        /// depending on <see cref="Role"/>.
        /// </summary>
        public int RoleRecordId { get; set; }

        /// <summary>Gets or sets the adviser department. Only used for an adviser.</summary>
        public string Department { get; set; }

        /// <summary>Gets or sets the adviser office. Only used for an adviser.</summary>
        public string Office { get; set; }

        /// <summary>Gets or sets the access level. Only used for an administrator.</summary>
        public string AccessLevel { get; set; }

        /// <summary>Gets or sets the enrolment year. Only used for a student.</summary>
        public int EnrollmentYear { get; set; }

        /// <summary>Gets or sets the grade point average. Only used for a student.</summary>
        public decimal? Gpa { get; set; }
    }

    /// <summary>
    /// Creates the correct <see cref="User"/> subclass for an account, given the role
    /// the database says the account has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is where the inheritance hierarchy pays for itself. The caller hands
    /// over one flat record and receives a <see cref="User"/>; whether that object is a
    /// <see cref="Student"/>, an <see cref="Adviser"/> or an
    /// <see cref="Administrator"/> is decided once, here, instead of by every screen
    /// that reads an account.
    /// </para>
    /// <para>
    /// The role value comes from the database, not from the caller's opinion, so the
    /// object graph can never disagree with the Users.Role column.
    /// </para>
    /// <para>
    /// The <c>default</c> branch throws rather than returning something plausible: an
    /// unknown role means the database and the code disagree, which must be loud.
    /// </para>
    /// </remarks>
    public static class UserFactory
    {
        /// <summary>
        /// Creates the concrete account type that matches the record's role.
        /// </summary>
        /// <param name="record">The flat record read from the database.</param>
        /// <returns>The matching concrete <see cref="User"/> subclass.</returns>
        /// <exception cref="ArgumentNullException">When the record is null.</exception>
        /// <exception cref="NotSupportedException">When the role is not recognised.</exception>
        public static User Create(UserRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            switch (record.Role)
            {
                case UserRole.Student:
                    return new Student(
                        record.UserId, record.Username, record.PasswordHash, record.FullName,
                        record.Email, record.IsActive, record.RoleRecordId,
                        record.EnrollmentYear, record.Gpa);

                case UserRole.Adviser:
                    return new Adviser(
                        record.UserId, record.Username, record.PasswordHash, record.FullName,
                        record.Email, record.IsActive, record.RoleRecordId,
                        string.IsNullOrEmpty(record.Department) ? "Unassigned" : record.Department,
                        record.Office);

                case UserRole.Administrator:
                    return new Administrator(
                        record.UserId, record.Username, record.PasswordHash, record.FullName,
                        record.Email, record.IsActive, record.RoleRecordId,
                        string.IsNullOrEmpty(record.AccessLevel) ? "ReadOnly" : record.AccessLevel);

                default:
                    throw new NotSupportedException(
                        "The account '" + record.Username + "' has an unrecognised role: " + record.Role + ".");
            }
        }
    }
}
