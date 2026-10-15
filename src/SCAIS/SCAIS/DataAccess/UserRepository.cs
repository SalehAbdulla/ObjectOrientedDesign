using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes accounts. Because SCAIS uses one login screen for three roles,
    /// this repository is the only place that knows how a Users row joins to the
    /// Student, Adviser or Administrator row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The account queries use LEFT JOINs against all three role tables at once. That
    /// keeps a single read path for every role and, more importantly, means the
    /// <c>Role</c> column that the database supplies is what decides which subclass
    /// <see cref="UserFactory"/> builds. The object graph can therefore never disagree
    /// with the database about what kind of account it is.
    /// </para>
    /// <para>
    /// Writes are performed inside a transaction, because an account spans two tables:
    /// a failure between the Users insert and the role insert would otherwise leave an
    /// account that cannot sign in to anything.
    /// </para>
    /// </remarks>
    public class UserRepository : SqlRepositoryBase<User>
    {
        /// <summary>
        /// The shared projection of an account. Column names are aliased so that the
        /// three role tables cannot collide in the result set.
        /// </summary>
        private const string SelectAccountSql = @"
SELECT  u.UserId, u.Username, u.PasswordHash, u.FullName, u.Email, u.Role, u.IsActive,
        s.StudentId             AS StudentId,
        s.SpecializationId      AS SpecializationId,
        s.AdviserId             AS StudentAdviserId,
        s.EnrollmentYear        AS EnrollmentYear,
        s.GPA                   AS GPA,
        sp.Name                 AS SpecializationName,
        sp.Description          AS SpecializationDescription,
        a.AdviserId             AS AdviserId,
        a.Department            AS Department,
        a.Office                AS Office,
        ad.AdminId              AS AdminId,
        ad.AccessLevel          AS AccessLevel,
        -- The adviser assigned to this student, read in the same query so that a student
        -- object always knows who reviews its plans. The credential is deliberately not
        -- selected: this adviser is displayed and identified, never authenticated.
        sau.UserId              AS StudentAdviserUserId,
        sau.Username            AS StudentAdviserUsername,
        sau.FullName            AS StudentAdviserName,
        sau.Email               AS StudentAdviserEmail,
        sau.IsActive            AS StudentAdviserIsActive,
        sa.Department           AS StudentAdviserDepartment,
        sa.Office               AS StudentAdviserOffice
FROM         dbo.Users           AS u
LEFT JOIN    dbo.Students        AS s   ON s.UserId   = u.UserId
LEFT JOIN    dbo.Specializations AS sp  ON sp.SpecializationId = s.SpecializationId
LEFT JOIN    dbo.Advisers        AS a   ON a.UserId   = u.UserId
LEFT JOIN    dbo.Administrators  AS ad  ON ad.UserId  = u.UserId
LEFT JOIN    dbo.Advisers        AS sa  ON sa.AdviserId = s.AdviserId
LEFT JOIN    dbo.Users           AS sau ON sau.UserId = sa.UserId";

        /// <inheritdoc />
        public override User GetById(int id)
        {
            List<User> matches = this.Query(
                SelectAccountSql + " WHERE u.UserId = @UserId",
                MapAccount,
                Param("@UserId", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <summary>
        /// Finds the account with the supplied sign-in name. Used by the authentication
        /// service, which is the only caller that needs to look an account up by
        /// something other than its key.
        /// </summary>
        /// <param name="username">The sign-in name to look for.</param>
        /// <returns>The account, or null when no such account exists.</returns>
        public User GetByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            List<User> matches = this.Query(
                SelectAccountSql + " WHERE u.Username = @Username",
                MapAccount,
                Param("@Username", username.Trim()));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<User> GetAll()
        {
            return this.Query(SelectAccountSql + " ORDER BY u.Role, u.FullName", MapAccount);
        }

        /// <summary>
        /// Returns every student account, used by the administrator's assign-advisees
        /// screen.
        /// </summary>
        /// <returns>The student accounts, ordered by name.</returns>
        public List<Student> GetStudents()
        {
            return this.StudentsFrom(
                this.Query(SelectAccountSql + " WHERE u.Role = 'Student' ORDER BY u.FullName", MapAccount));
        }

        /// <summary>
        /// Returns every student assigned to one adviser. This is the query behind the
        /// adviser's advisee list.
        /// </summary>
        /// <param name="adviserId">The adviser whose advisees are wanted.</param>
        /// <returns>The assigned students, ordered by name.</returns>
        public List<Student> GetStudentsByAdviser(int adviserId)
        {
            return this.StudentsFrom(this.Query(
                SelectAccountSql +
                " WHERE u.Role = 'Student' AND s.AdviserId = @AdviserId ORDER BY u.FullName",
                MapAccount,
                Param("@AdviserId", adviserId)));
        }

        /// <summary>
        /// Returns every adviser account, used by the assign-advisees screen.
        /// </summary>
        /// <returns>The adviser accounts, ordered by name.</returns>
        public List<Adviser> GetAdvisers()
        {
            List<Adviser> advisers = new List<Adviser>();

            foreach (User user in this.Query(
                SelectAccountSql + " WHERE u.Role = 'Adviser' ORDER BY u.FullName", MapAccount))
            {
                Adviser adviser = user as Adviser;

                if (adviser != null)
                {
                    advisers.Add(adviser);
                }
            }

            return advisers;
        }

        /// <summary>
        /// Returns the Users key that owns the supplied student. Notifications are
        /// addressed to the account rather than to the student record, so the mapping
        /// has to be resolved from the database rather than assumed.
        /// </summary>
        /// <param name="studentId">The Students key to resolve.</param>
        /// <returns>The matching UserId, or zero when the student does not exist.</returns>
        public int GetUserIdForStudent(int studentId)
        {
            List<int> matches = this.Query(
                "SELECT UserId FROM dbo.Students WHERE StudentId = @StudentId;",
                delegate(SqlDataReader reader) { return ToInt(reader, "UserId"); },
                Param("@StudentId", studentId));

            return matches.Count == 0 ? 0 : matches[0];
        }

        /// <summary>
        /// Returns the adviser currently assigned to a student, or null when none is.
        /// </summary>
        /// <param name="studentId">The Students key to look up.</param>
        /// <returns>The assigned AdviserId, or null.</returns>
        public int? GetAdviserIdForStudent(int studentId)
        {
            List<int> matches = this.Query(
                "SELECT AdviserId FROM dbo.Students WHERE StudentId = @StudentId;",
                delegate(SqlDataReader reader) { return ToInt(reader, "AdviserId"); },
                Param("@StudentId", studentId));

            // A null column is read as zero by ToInt, and zero is not a valid key, so the
            // absence of an adviser is reported as null rather than as a false assignment.
            return matches.Count == 0 || matches[0] == 0 ? (int?)null : matches[0];
        }

        /// <summary>
        /// Narrows a mixed account list to the students it contains.
        /// </summary>
        private List<Student> StudentsFrom(List<User> accounts)
        {
            List<Student> students = new List<Student>();

            foreach (User user in accounts)
            {
                Student student = user as Student;

                if (student != null)
                {
                    students.Add(student);
                }
            }

            return students;
        }

        /// <summary>
        /// Turns the current reader position into the correct concrete account type.
        /// </summary>
        /// <remarks>
        /// The <c>Role</c> column read from the database decides which subclass is built,
        /// so a screen can never disagree with the database about what an account is.
        /// </remarks>
        private static User MapAccount(SqlDataReader reader)
        {
            UserRole role = (UserRole)Enum.Parse(typeof(UserRole), ToText(reader, "Role"), true);

            UserRecord record = new UserRecord();
            record.UserId = ToInt(reader, "UserId");
            record.Username = ToText(reader, "Username");
            record.PasswordHash = ToText(reader, "PasswordHash");
            record.FullName = ToText(reader, "FullName");
            record.Email = ToText(reader, "Email");
            record.Role = role;
            record.IsActive = ToBool(reader, "IsActive");

            switch (role)
            {
                case UserRole.Student:
                    record.RoleRecordId = ToInt(reader, "StudentId");
                    record.EnrollmentYear = ToInt(reader, "EnrollmentYear");
                    record.Gpa = ToNullableDecimal(reader, "GPA");
                    break;

                case UserRole.Adviser:
                    record.RoleRecordId = ToInt(reader, "AdviserId");
                    record.Department = ToText(reader, "Department");
                    record.Office = ToText(reader, "Office");
                    break;

                default:
                    record.RoleRecordId = ToInt(reader, "AdminId");
                    record.AccessLevel = ToText(reader, "AccessLevel");
                    break;
            }

            User user = UserFactory.Create(record);

            // A student also needs the track object, because the track name lives in
            // another table and the user interface shows it wherever the student appears.
            Student student = user as Student;

            if (student != null && !IsNull(reader, "SpecializationId"))
            {
                student.Specialization = new Specialization(
                    ToInt(reader, "SpecializationId"),
                    ToText(reader, "SpecializationName"),
                    ToText(reader, "SpecializationDescription"));
            }

            // The assigned adviser is part of the student's own state rather than something
            // each screen has to look up. Two behaviours depend on it: the plan a student
            // submits must be addressed to the adviser who reviews it (a plan with no adviser
            // never appears in any adviser's queue), and the student dashboard names the
            // adviser. The columns come from the same query, so this costs no extra round trip.
            if (student != null && !IsNull(reader, "StudentAdviserId"))
            {
                UserRecord adviserRecord = new UserRecord();
                adviserRecord.UserId = ToInt(reader, "StudentAdviserUserId");
                adviserRecord.Username = ToText(reader, "StudentAdviserUsername");
                adviserRecord.PasswordHash = string.Empty;
                adviserRecord.FullName = ToText(reader, "StudentAdviserName");
                adviserRecord.Email = ToText(reader, "StudentAdviserEmail");
                adviserRecord.Role = UserRole.Adviser;
                adviserRecord.IsActive = ToBool(reader, "StudentAdviserIsActive");
                adviserRecord.RoleRecordId = ToInt(reader, "StudentAdviserId");
                adviserRecord.Department = ToText(reader, "StudentAdviserDepartment");
                adviserRecord.Office = ToText(reader, "StudentAdviserOffice");

                student.Adviser = (Adviser)UserFactory.Create(adviserRecord);
            }

            return user;
        }

        /// <summary>
        /// Creates an account. The Users row and the role row are written inside one
        /// transaction, so an account can never be half created.
        /// </summary>
        /// <param name="entity">The account to create.</param>
        /// <returns>True when the account was created.</returns>
        public override bool Add(User entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            using (SqlConnection connection = CreateConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                int userId = InsertUserRow(connection, transaction, entity);

                if (userId > 0)
                {
                    InsertRoleRow(connection, transaction, entity, userId);
                }

                transaction.Commit();
                return userId > 0;
            }
        }

        /// <summary>
        /// Updates the account's own attributes and the attributes of its role row.
        /// The password is changed separately, by <see cref="UpdatePassword"/>.
        /// </summary>
        /// <param name="entity">The account to update.</param>
        /// <returns>True when a row was changed.</returns>
        public override bool Update(User entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            using (SqlConnection connection = CreateConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                int affected;

                using (SqlCommand command = new SqlCommand(
                    @"UPDATE dbo.Users
                      SET FullName = @FullName, Email = @Email, IsActive = @IsActive
                      WHERE UserId = @UserId;", connection, transaction))
                {
                    command.Parameters.AddWithValue("@FullName", entity.FullName);
                    command.Parameters.AddWithValue("@Email", entity.Email);
                    command.Parameters.AddWithValue("@IsActive", entity.IsActive);
                    command.Parameters.AddWithValue("@UserId", entity.UserId);
                    affected = command.ExecuteNonQuery();
                }

                Student student = entity as Student;

                if (student != null)
                {
                    affected += this.UpdateStudentRow(connection, transaction, student);
                }

                Adviser adviser = entity as Adviser;

                if (adviser != null)
                {
                    affected += this.UpdateAdviserRow(connection, transaction, adviser);
                }

                Administrator administrator = entity as Administrator;

                if (administrator != null)
                {
                    affected += this.UpdateAdministratorRow(connection, transaction, administrator);
                }

                transaction.Commit();
                return affected > 0;
            }
        }

        /// <summary>
        /// Deletes an account and its role row. The role row is removed first because
        /// it holds the foreign key to Users.
        /// </summary>
        /// <param name="id">The UserId to delete.</param>
        /// <returns>True when the account was deleted.</returns>
        public override bool Delete(int id)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                // The role has to be discovered first, because the role row must go
                // before the Users row that it references.
                string role;

                using (SqlCommand command = new SqlCommand(
                    "SELECT Role FROM dbo.Users WHERE UserId = @UserId;", connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", id);
                    object result = command.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    role = Convert.ToString(result);
                }

                string deleteRoleSql;

                if (role == "Student") { deleteRoleSql = "DELETE FROM dbo.Students WHERE UserId = @UserId;"; }
                else if (role == "Adviser") { deleteRoleSql = "DELETE FROM dbo.Advisers WHERE UserId = @UserId;"; }
                else { deleteRoleSql = "DELETE FROM dbo.Administrators WHERE UserId = @UserId;"; }

                using (SqlCommand command = new SqlCommand(deleteRoleSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", id);
                    command.ExecuteNonQuery();
                }

                int affected;

                using (SqlCommand command = new SqlCommand(
                    "DELETE FROM dbo.Users WHERE UserId = @UserId;", connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", id);
                    affected = command.ExecuteNonQuery();
                }

                transaction.Commit();
                return affected > 0;
            }
        }

        /// <summary>
        /// Assigns or clears a student's adviser. This is the operation behind the
        /// administrator's assign-advisees screen.
        /// </summary>
        /// <param name="studentId">The student to reassign.</param>
        /// <param name="adviserId">The new adviser, or null to unassign.</param>
        /// <returns>True when the student was updated.</returns>
        public bool SetStudentAdviser(int studentId, int? adviserId)
        {
            return this.Execute(
                "UPDATE dbo.Students SET AdviserId = @AdviserId WHERE StudentId = @StudentId;",
                Param("@AdviserId", adviserId),
                Param("@StudentId", studentId)) > 0;
        }

        /// <summary>
        /// Replaces an account's password. The plain text password is hashed here and
        /// never stored, and it is never read back out.
        /// </summary>
        /// <param name="userId">The account to change.</param>
        /// <param name="plainPassword">The new plain text password.</param>
        /// <returns>True when the credential was changed.</returns>
        public bool UpdatePassword(int userId, string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || plainPassword.Length < 6)
            {
                throw new ArgumentException("A password must be at least six characters long.", "plainPassword");
            }

            return this.Execute(
                "UPDATE dbo.Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId;",
                Param("@PasswordHash", PasswordHasher.Hash(plainPassword)),
                Param("@UserId", userId)) > 0;
        }

        /// <summary>
        /// Returns true when the sign-in name is already taken, ignoring one account so
        /// that saving an existing account does not report a clash with itself.
        /// </summary>
        /// <param name="username">The sign-in name to test.</param>
        /// <param name="excludeUserId">An account to ignore, or zero.</param>
        /// <returns>True when another account already uses the name.</returns>
        public bool UsernameExists(string username, int excludeUserId)
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            List<int> matches = this.Query(
                "SELECT COUNT(*) AS Matches FROM dbo.Users WHERE Username = @Username AND UserId <> @UserId;",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@Username", username.Trim()),
                Param("@UserId", excludeUserId));

            return matches.Count > 0 && matches[0] > 0;
        }

        /// <summary>Inserts the Users row and returns the generated key.</summary>
        private static int InsertUserRow(SqlConnection connection, SqlTransaction transaction, User entity)
        {
            const string sql = @"
INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Role, IsActive)
VALUES (@Username, @PasswordHash, @FullName, @Email, @Role, @IsActive);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Username", entity.Username);
                command.Parameters.AddWithValue("@PasswordHash",
                    string.IsNullOrEmpty(entity.PasswordHashForPersistence)
                        ? PasswordHasher.Hash(entity.Username + "123")
                        : entity.PasswordHashForPersistence);
                command.Parameters.AddWithValue("@FullName", entity.FullName);
                command.Parameters.AddWithValue("@Email", entity.Email);
                command.Parameters.AddWithValue("@Role", entity.Role.ToString());
                command.Parameters.AddWithValue("@IsActive", entity.IsActive);

                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        /// <summary>
        /// Inserts the role specific row for a newly created account. The role of the
        /// object decides which table is written, which is the polymorphic use of the
        /// <see cref="User"/> hierarchy on the write path.
        /// </summary>
        private static void InsertRoleRow(SqlConnection connection, SqlTransaction transaction,
                                          User entity, int userId)
        {
            Student student = entity as Student;

            if (student != null)
            {
                const string studentSql = @"
INSERT INTO dbo.Students (UserId, SpecializationId, AdviserId, EnrollmentYear, GPA)
VALUES (@UserId, @SpecializationId, @AdviserId, @EnrollmentYear, @Gpa);";

                using (SqlCommand command = new SqlCommand(studentSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@SpecializationId",
                        student.Specialization == null ? 1 : student.Specialization.SpecializationId);
                    command.Parameters.AddWithValue("@AdviserId",
                        student.Adviser == null ? (object)DBNull.Value : student.Adviser.AdviserId);
                    command.Parameters.AddWithValue("@EnrollmentYear", student.EnrollmentYear);
                    command.Parameters.AddWithValue("@Gpa",
                        student.GPA.HasValue ? (object)student.GPA.Value : DBNull.Value);
                    command.ExecuteNonQuery();
                }

                return;
            }

            Adviser adviser = entity as Adviser;

            if (adviser != null)
            {
                using (SqlCommand command = new SqlCommand(
                    "INSERT INTO dbo.Advisers (UserId, Department, Office) VALUES (@UserId, @Department, @Office);",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Department", adviser.Department);
                    command.Parameters.AddWithValue("@Office", adviser.Office ?? string.Empty);
                    command.ExecuteNonQuery();
                }

                return;
            }

            Administrator administrator = entity as Administrator;

            if (administrator != null)
            {
                using (SqlCommand command = new SqlCommand(
                    "INSERT INTO dbo.Administrators (UserId, AccessLevel) VALUES (@UserId, @AccessLevel);",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@AccessLevel", administrator.AccessLevel);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Updates the Students row of an account.</summary>
        private int UpdateStudentRow(SqlConnection connection, SqlTransaction transaction, Student student)
        {
            const string sql = @"
UPDATE dbo.Students
SET SpecializationId = @SpecializationId,
    AdviserId        = @AdviserId,
    EnrollmentYear   = @EnrollmentYear,
    GPA              = @Gpa
WHERE StudentId = @StudentId;";

            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@SpecializationId",
                    student.Specialization == null ? 1 : student.Specialization.SpecializationId);
                command.Parameters.AddWithValue("@AdviserId",
                    student.Adviser == null ? (object)DBNull.Value : student.Adviser.AdviserId);
                command.Parameters.AddWithValue("@EnrollmentYear", student.EnrollmentYear);
                command.Parameters.AddWithValue("@Gpa",
                    student.GPA.HasValue ? (object)student.GPA.Value : DBNull.Value);
                command.Parameters.AddWithValue("@StudentId", student.StudentId);
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>Updates the Advisers row of an account.</summary>
        private int UpdateAdviserRow(SqlConnection connection, SqlTransaction transaction, Adviser adviser)
        {
            using (SqlCommand command = new SqlCommand(
                "UPDATE dbo.Advisers SET Department = @Department, Office = @Office WHERE AdviserId = @AdviserId;",
                connection, transaction))
            {
                command.Parameters.AddWithValue("@Department", adviser.Department);
                command.Parameters.AddWithValue("@Office", adviser.Office ?? string.Empty);
                command.Parameters.AddWithValue("@AdviserId", adviser.AdviserId);
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>Updates the Administrators row of an account.</summary>
        private int UpdateAdministratorRow(SqlConnection connection, SqlTransaction transaction,
                                           Administrator administrator)
        {
            using (SqlCommand command = new SqlCommand(
                "UPDATE dbo.Administrators SET AccessLevel = @AccessLevel WHERE AdminId = @AdminId;",
                connection, transaction))
            {
                command.Parameters.AddWithValue("@AccessLevel", administrator.AccessLevel);
                command.Parameters.AddWithValue("@AdminId", administrator.AdminId);
                return command.ExecuteNonQuery();
            }
        }
    }
}
