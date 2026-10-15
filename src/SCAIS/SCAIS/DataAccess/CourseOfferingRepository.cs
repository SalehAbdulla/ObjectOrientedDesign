using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the timetable: which course runs in which academic term.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The projection joins the offering to its course (and that course's track) and to its
    /// semester, so a loaded <see cref="CourseOffering"/> is complete enough for the
    /// maintenance screen to display without a second query. It also counts the enrolments
    /// for the same course and term, which is how the screen shows how many of the places are
    /// already taken.
    /// </para>
    /// <para>
    /// The count is taken over every enrolment in that course and term regardless of status,
    /// because a place is occupied whether the course has been completed, is in progress or is
    /// still pending.
    /// </para>
    /// </remarks>
    public class CourseOfferingRepository : SqlRepositoryBase<CourseOffering>
    {
        /// <summary>The shared projection of an offering.</summary>
        private const string SelectSql = @"
SELECT  o.OfferingId, o.Capacity,
        c.CourseId, c.Code, c.Title, c.CreditHours, c.IsCore, c.Description,
        sp.SpecializationId AS SpecializationId,
        sp.Name             AS SpecializationName,
        sp.Description      AS SpecializationDescription,
        s.SemesterId, s.TermName, s.AcademicYear, s.StartDate, s.EndDate,
        (SELECT COUNT(*)
         FROM   dbo.Enrollments AS e
         WHERE  e.CourseId = o.CourseId AND e.SemesterId = o.SemesterId) AS Enrolled
FROM        dbo.CourseOfferings AS o
INNER JOIN  dbo.Courses         AS c  ON c.CourseId = o.CourseId
INNER JOIN  dbo.Semesters       AS s  ON s.SemesterId = o.SemesterId
LEFT  JOIN  dbo.Specializations AS sp ON sp.SpecializationId = c.SpecializationId";

        /// <inheritdoc />
        public override CourseOffering GetById(int id)
        {
            List<CourseOffering> matches = this.Query(
                SelectSql + " WHERE o.OfferingId = @Id", MapOffering, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<CourseOffering> GetAll()
        {
            return this.Query(
                SelectSql + " ORDER BY s.AcademicYear DESC, s.SemesterId DESC, c.Code", MapOffering);
        }

        /// <summary>Returns everything offered in one term.</summary>
        /// <param name="semesterId">The term to list.</param>
        /// <returns>The offerings for that term, ordered by catalogue code.</returns>
        public List<CourseOffering> GetBySemester(int semesterId)
        {
            return this.Query(
                SelectSql + " WHERE o.SemesterId = @SemesterId ORDER BY c.Code",
                MapOffering,
                Param("@SemesterId", semesterId));
        }

        /// <summary>Returns every term in which one course is offered.</summary>
        /// <param name="courseId">The course to look for.</param>
        /// <returns>The offerings of that course, most recent term first.</returns>
        public List<CourseOffering> GetByCourse(int courseId)
        {
            return this.Query(
                SelectSql + " WHERE o.CourseId = @CourseId ORDER BY s.AcademicYear DESC, s.SemesterId DESC",
                MapOffering,
                Param("@CourseId", courseId));
        }

        /// <summary>
        /// Returns true when the same course is already offered in the same term, which the
        /// database also enforces through <c>UQ_CourseOfferings</c>. Checking here lets the
        /// screen explain the clash instead of showing a constraint violation.
        /// </summary>
        /// <param name="courseId">The course to test.</param>
        /// <param name="semesterId">The term to test.</param>
        /// <param name="excludeOfferingId">An offering to ignore, or zero.</param>
        /// <returns>True when another offering already pairs the course and the term.</returns>
        public bool Exists(int courseId, int semesterId, int excludeOfferingId)
        {
            List<int> matches = this.Query(
                "SELECT COUNT(*) AS Matches FROM dbo.CourseOfferings " +
                "WHERE CourseId = @CourseId AND SemesterId = @SemesterId AND OfferingId <> @Id;",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@CourseId", courseId),
                Param("@SemesterId", semesterId),
                Param("@Id", excludeOfferingId));

            return matches.Count > 0 && matches[0] > 0;
        }

        /// <inheritdoc />
        public override bool Add(CourseOffering entity)
        {
            return this.ExecuteInsert(
                @"INSERT INTO dbo.CourseOfferings (CourseId, SemesterId, Capacity)
                  VALUES (@CourseId, @SemesterId, @Capacity);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@CourseId", entity.Course.CourseId),
                Param("@SemesterId", entity.Semester.SemesterId),
                Param("@Capacity", entity.Capacity)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(CourseOffering entity)
        {
            return this.Execute(
                @"UPDATE dbo.CourseOfferings
                  SET CourseId = @CourseId, SemesterId = @SemesterId, Capacity = @Capacity
                  WHERE OfferingId = @Id;",
                Param("@CourseId", entity.Course.CourseId),
                Param("@SemesterId", entity.Semester.SemesterId),
                Param("@Capacity", entity.Capacity),
                Param("@Id", entity.OfferingId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute(
                "DELETE FROM dbo.CourseOfferings WHERE OfferingId = @Id;", Param("@Id", id)) > 0;
        }

        /// <summary>
        /// Adds to the target term every course offered in the source term that is not already
        /// offered in the target.
        /// </summary>
        /// <remarks>
        /// The work is done as one set-based statement rather than by reading a list and issuing
        /// an insert per row, and the <c>NOT EXISTS</c> clause makes the operation idempotent:
        /// running it twice adds nothing the second time. An administrator therefore does not
        /// have to check what is already there before pressing the button.
        /// </remarks>
        /// <param name="sourceSemesterId">The term to copy from.</param>
        /// <param name="targetSemesterId">The term to copy into.</param>
        /// <returns>The number of offerings added.</returns>
        public int CopyFrom(int sourceSemesterId, int targetSemesterId)
        {
            return this.Execute(
                @"INSERT INTO dbo.CourseOfferings (CourseId, SemesterId, Capacity)
                  SELECT  o.CourseId, @TargetSemesterId, o.Capacity
                  FROM    dbo.CourseOfferings AS o
                  WHERE   o.SemesterId = @SourceSemesterId
                  AND     NOT EXISTS (SELECT 1
                                      FROM   dbo.CourseOfferings AS existing
                                      WHERE  existing.CourseId = o.CourseId
                                      AND    existing.SemesterId = @TargetSemesterId);",
                Param("@SourceSemesterId", sourceSemesterId),
                Param("@TargetSemesterId", targetSemesterId));
        }

        /// <summary>Maps the current row to an offering, its course, its track and its term.</summary>
        private static CourseOffering MapOffering(SqlDataReader reader)
        {
            Course course = new Course(
                ToInt(reader, "CourseId"),
                ToText(reader, "Code"),
                ToText(reader, "Title"),
                ToInt(reader, "CreditHours"),
                ToBool(reader, "IsCore"),
                ToText(reader, "Description"));

            if (!IsNull(reader, "SpecializationId"))
            {
                course.Specialization = new Specialization(
                    ToInt(reader, "SpecializationId"),
                    ToText(reader, "SpecializationName"),
                    ToText(reader, "SpecializationDescription"));
            }

            Semester semester = new Semester(
                ToInt(reader, "SemesterId"),
                ToText(reader, "TermName"),
                ToInt(reader, "AcademicYear"),
                ToNullableDate(reader, "StartDate"),
                ToNullableDate(reader, "EndDate"));

            CourseOffering offering = new CourseOffering(
                ToInt(reader, "OfferingId"), course, semester, ToInt(reader, "Capacity"));

            offering.Enrolled = ToInt(reader, "Enrolled");

            return offering;
        }
    }
}
