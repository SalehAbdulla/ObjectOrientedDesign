using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the academic record, and is the repository behind the
    /// completed / in-progress / pending status tracking.
    /// </summary>
    /// <remarks>
    /// Enrolments are read joined to their course, so a loaded <see cref="Enrollment"/>
    /// can report its course code, title and credit hours without a second query. Status
    /// and grade are read through the constructor, which means the domain invariants are
    /// re-checked on every load rather than trusted.
    /// </remarks>
    public class EnrollmentRepository : SqlRepositoryBase<Enrollment>
    {
        /// <summary>The shared projection of an enrolment, with its course joined in.</summary>
        private const string SelectSql = @"
SELECT  e.EnrollmentId, e.StudentId, e.CourseId, e.SemesterId, e.Status, e.Grade,
        c.Code        AS CourseCode,
        c.Title       AS CourseTitle,
        c.CreditHours AS CourseCredits,
        c.IsCore      AS CourseIsCore,
        c.Description AS CourseDescription,
        sp.SpecializationId AS SpecializationId,
        sp.Name             AS SpecializationName,
        sp.Description      AS SpecializationDescription
FROM        dbo.Enrollments     AS e
INNER JOIN  dbo.Courses         AS c  ON c.CourseId = e.CourseId
LEFT JOIN   dbo.Specializations AS sp ON sp.SpecializationId = c.SpecializationId";

        /// <inheritdoc />
        public override Enrollment GetById(int id)
        {
            List<Enrollment> matches = this.Query(
                SelectSql + " WHERE e.EnrollmentId = @Id", MapEnrollment, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Enrollment> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY e.StudentId, c.Code", MapEnrollment);
        }

        /// <summary>
        /// Returns the complete academic record of one student. This single query is what
        /// fills <see cref="Student.LoadAcademicRecord"/> and therefore what makes the
        /// status tracking and the prerequisite checks work in memory.
        /// </summary>
        /// <param name="studentId">The student whose record is wanted.</param>
        /// <returns>The enrolments, ordered by course code.</returns>
        public List<Enrollment> GetByStudent(int studentId)
        {
            return this.Query(
                SelectSql + " WHERE e.StudentId = @StudentId ORDER BY c.Code",
                MapEnrollment,
                Param("@StudentId", studentId));
        }

        /// <summary>Returns the enrolments of one student in one semester.</summary>
        /// <param name="studentId">The student whose record is wanted.</param>
        /// <param name="semesterId">The semester to filter by.</param>
        /// <returns>The matching enrolments.</returns>
        public List<Enrollment> GetByStudentAndSemester(int studentId, int semesterId)
        {
            return this.Query(
                SelectSql + " WHERE e.StudentId = @StudentId AND e.SemesterId = @SemesterId ORDER BY c.Code",
                MapEnrollment,
                Param("@StudentId", studentId),
                Param("@SemesterId", semesterId));
        }

        /// <summary>
        /// Returns every enrolment of one course. Used by the prerequisite compliance
        /// report to find out who has and has not completed a subject.
        /// </summary>
        /// <param name="courseId">The course to report on.</param>
        /// <returns>The matching enrolments.</returns>
        public List<Enrollment> GetByCourse(int courseId)
        {
            return this.Query(
                SelectSql + " WHERE e.CourseId = @CourseId ORDER BY e.StudentId",
                MapEnrollment,
                Param("@CourseId", courseId));
        }

        /// <summary>
        /// Counts the enrolments of one student in one status, used by the progress
        /// report summary.
        /// </summary>
        /// <param name="studentId">The student to count for.</param>
        /// <param name="status">The status to count.</param>
        /// <returns>The number of matching enrolments.</returns>
        public int CountByStatus(int studentId, EnrollmentStatus status)
        {
            List<int> matches = this.Query(
                "SELECT COUNT(*) AS Matches FROM dbo.Enrollments " +
                "WHERE StudentId = @StudentId AND Status = @Status;",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@StudentId", studentId),
                Param("@Status", status.ToString()));

            return matches.Count == 0 ? 0 : matches[0];
        }

        /// <inheritdoc />
        public override bool Add(Enrollment entity)
        {
            return this.ExecuteInsert(
                @"INSERT INTO dbo.Enrollments (StudentId, CourseId, SemesterId, Status, Grade)
                  VALUES (@StudentId, @CourseId, @SemesterId, @Status, @Grade);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@StudentId", entity.StudentId),
                Param("@CourseId", entity.Course.CourseId),
                Param("@SemesterId", entity.SemesterId),
                Param("@Status", entity.Status.ToString()),
                Param("@Grade", entity.Grade)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(Enrollment entity)
        {
            return this.Execute(
                @"UPDATE dbo.Enrollments
                  SET SemesterId = @SemesterId, Status = @Status, Grade = @Grade
                  WHERE EnrollmentId = @Id;",
                Param("@SemesterId", entity.SemesterId),
                Param("@Status", entity.Status.ToString()),
                Param("@Grade", entity.Grade),
                Param("@Id", entity.EnrollmentId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Enrollments WHERE EnrollmentId = @Id;",
                Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to an enrolment, building its course.</summary>
        private static Enrollment MapEnrollment(SqlDataReader reader)
        {
            Course course = new Course(
                ToInt(reader, "CourseId"),
                ToText(reader, "CourseCode"),
                ToText(reader, "CourseTitle"),
                ToInt(reader, "CourseCredits"),
                ToBool(reader, "CourseIsCore"),
                ToText(reader, "CourseDescription"));

            if (!IsNull(reader, "SpecializationId"))
            {
                course.Specialization = new Specialization(
                    ToInt(reader, "SpecializationId"),
                    ToText(reader, "SpecializationName"),
                    ToText(reader, "SpecializationDescription"));
            }

            EnrollmentStatus status = (EnrollmentStatus)Enum.Parse(
                typeof(EnrollmentStatus), ToText(reader, "Status"), true);

            string grade = IsNull(reader, "Grade") ? null : ToText(reader, "Grade");

            return new Enrollment(
                ToInt(reader, "EnrollmentId"),
                ToInt(reader, "StudentId"),
                course,
                ToInt(reader, "SemesterId"),
                status,
                grade);
        }
    }
}
