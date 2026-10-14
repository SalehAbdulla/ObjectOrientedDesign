using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the course catalogue.
    /// </summary>
    /// <remarks>
    /// Courses are read joined to their specialization, so a loaded <see cref="Course"/>
    /// is complete enough to be displayed without a second query. A core subject joins
    /// to nothing and its track is left null, which is how the domain model represents
    /// "this subject belongs to every track".
    /// </remarks>
    public class CourseRepository : SqlRepositoryBase<Course>
    {
        /// <summary>The shared projection of a course.</summary>
        private const string SelectSql = @"
SELECT  c.CourseId, c.Code, c.Title, c.CreditHours, c.IsCore, c.Description,
        sp.SpecializationId AS SpecializationId,
        sp.Name             AS SpecializationName,
        sp.Description      AS SpecializationDescription
FROM        dbo.Courses         AS c
LEFT JOIN   dbo.Specializations AS sp ON sp.SpecializationId = c.SpecializationId";

        /// <inheritdoc />
        public override Course GetById(int id)
        {
            List<Course> matches = this.Query(
                SelectSql + " WHERE c.CourseId = @Id", MapCourse, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Course> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY c.Code", MapCourse);
        }

        /// <summary>Returns the core subjects that every student takes.</summary>
        /// <returns>The core courses, ordered by code.</returns>
        public List<Course> GetCoreCourses()
        {
            return this.Query(SelectSql + " WHERE c.IsCore = 1 ORDER BY c.Code", MapCourse);
        }

        /// <summary>
        /// Returns the subjects of one track plus the core subjects every student takes.
        /// </summary>
        /// <param name="specializationId">The track to load.</param>
        /// <returns>The courses for that track, ordered by code.</returns>
        public List<Course> GetForSpecialization(int specializationId)
        {
            return this.Query(
                SelectSql +
                " WHERE c.SpecializationId = @SpecializationId OR c.IsCore = 1 ORDER BY c.Code",
                MapCourse,
                Param("@SpecializationId", specializationId));
        }

        /// <summary>Returns true when the catalogue code is already used by another course.</summary>
        /// <param name="code">The catalogue code to test.</param>
        /// <param name="excludeCourseId">A course to ignore, or zero.</param>
        /// <returns>True when another course already uses the code.</returns>
        public bool CodeExists(string code, int excludeCourseId)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            List<int> matches = this.Query(
                "SELECT COUNT(*) AS Matches FROM dbo.Courses WHERE Code = @Code AND CourseId <> @Id;",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@Code", code.Trim().ToUpperInvariant()),
                Param("@Id", excludeCourseId));

            return matches.Count > 0 && matches[0] > 0;
        }

        /// <inheritdoc />
        public override bool Add(Course entity)
        {
            return this.ExecuteInsert(
                @"INSERT INTO dbo.Courses (Code, Title, CreditHours, IsCore, SpecializationId, Description)
                  VALUES (@Code, @Title, @CreditHours, @IsCore, @SpecializationId, @Description);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@Code", entity.Code),
                Param("@Title", entity.Title),
                Param("@CreditHours", entity.CreditHours),
                Param("@IsCore", entity.IsCore),
                Param("@SpecializationId",
                    entity.Specialization == null ? (object)DBNull.Value : entity.Specialization.SpecializationId),
                Param("@Description", entity.Description)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(Course entity)
        {
            return this.Execute(
                @"UPDATE dbo.Courses
                  SET Code = @Code, Title = @Title, CreditHours = @CreditHours,
                      IsCore = @IsCore, SpecializationId = @SpecializationId, Description = @Description
                  WHERE CourseId = @Id;",
                Param("@Code", entity.Code),
                Param("@Title", entity.Title),
                Param("@CreditHours", entity.CreditHours),
                Param("@IsCore", entity.IsCore),
                Param("@SpecializationId",
                    entity.Specialization == null ? (object)DBNull.Value : entity.Specialization.SpecializationId),
                Param("@Description", entity.Description),
                Param("@Id", entity.CourseId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Courses WHERE CourseId = @Id;", Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to a course, including its track.</summary>
        private static Course MapCourse(SqlDataReader reader)
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

            return course;
        }
    }
}
