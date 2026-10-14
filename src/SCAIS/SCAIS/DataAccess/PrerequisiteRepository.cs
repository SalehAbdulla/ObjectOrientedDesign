using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the curriculum dependency rules.
    /// </summary>
    /// <remarks>
    /// A rule references the Courses table twice: once for the dependent course and once
    /// for the course that must be completed first. The projection therefore joins
    /// Courses twice with different aliases and the mapper builds two separate
    /// <see cref="Course"/> objects, because a rule that could not name its courses
    /// would be useless to the eligibility rules that consume it.
    /// </remarks>
    public class PrerequisiteRepository : SqlRepositoryBase<Prerequisite>
    {
        /// <summary>The shared projection of a rule, with both courses joined in.</summary>
        private const string SelectSql = @"
SELECT  p.PrerequisiteId, p.CourseId, p.RequiredCourseId, p.RuleType,
        dc.Code        AS DependentCode,
        dc.Title       AS DependentTitle,
        dc.CreditHours AS DependentCredits,
        dc.IsCore      AS DependentIsCore,
        dc.Description AS DependentDescription,
        rc.Code        AS RequiredCode,
        rc.Title       AS RequiredTitle,
        rc.CreditHours AS RequiredCredits,
        rc.IsCore      AS RequiredIsCore,
        rc.Description AS RequiredDescription
FROM        dbo.Prerequisites AS p
INNER JOIN  dbo.Courses       AS dc ON dc.CourseId = p.CourseId
INNER JOIN  dbo.Courses       AS rc ON rc.CourseId = p.RequiredCourseId";

        /// <inheritdoc />
        public override Prerequisite GetById(int id)
        {
            List<Prerequisite> matches = this.Query(
                SelectSql + " WHERE p.PrerequisiteId = @Id", MapPrerequisite, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Prerequisite> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY dc.Code, rc.Code", MapPrerequisite);
        }

        /// <summary>
        /// Returns every rule that applies to one course. This is the query the
        /// eligibility rules run for each course a student selects.
        /// </summary>
        /// <param name="courseId">The dependent course.</param>
        /// <returns>The rules that govern that course.</returns>
        public List<Prerequisite> GetByCourse(int courseId)
        {
            return this.Query(
                SelectSql + " WHERE p.CourseId = @CourseId ORDER BY rc.Code",
                MapPrerequisite,
                Param("@CourseId", courseId));
        }

        /// <summary>Returns every rule of one type, used to build the compliance report.</summary>
        /// <param name="ruleType">The rule type to load.</param>
        /// <returns>The matching rules.</returns>
        public List<Prerequisite> GetByRuleType(RuleType ruleType)
        {
            return this.Query(
                SelectSql + " WHERE p.RuleType = @RuleType ORDER BY dc.Code, rc.Code",
                MapPrerequisite,
                Param("@RuleType", ruleType.ToString()));
        }

        /// <summary>Removes every rule that governs one course.</summary>
        /// <param name="courseId">The dependent course.</param>
        /// <returns>The number of rules removed.</returns>
        public int DeleteByCourse(int courseId)
        {
            return this.Execute("DELETE FROM dbo.Prerequisites WHERE CourseId = @CourseId;",
                Param("@CourseId", courseId));
        }

        /// <inheritdoc />
        public override bool Add(Prerequisite entity)
        {
            return this.ExecuteInsert(
                @"INSERT INTO dbo.Prerequisites (CourseId, RequiredCourseId, RuleType)
                  VALUES (@CourseId, @RequiredCourseId, @RuleType);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@CourseId", entity.Course.CourseId),
                Param("@RequiredCourseId", entity.RequiredCourse.CourseId),
                Param("@RuleType", entity.RuleType.ToString())) > 0;
        }

        /// <summary>
        /// Rules are not edited in place: changing which course a rule points at would
        /// silently change the curriculum, so a rule is deleted and recreated instead.
        /// </summary>
        /// <param name="entity">The rule that cannot be updated.</param>
        /// <returns>Never returns; always throws.</returns>
        public override bool Update(Prerequisite entity)
        {
            throw new NotSupportedException(
                "A prerequisite rule cannot be updated. Delete it and create a new one.");
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Prerequisites WHERE PrerequisiteId = @Id;",
                Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to a rule, building both courses.</summary>
        private static Prerequisite MapPrerequisite(SqlDataReader reader)
        {
            Course dependent = new Course(
                ToInt(reader, "CourseId"),
                ToText(reader, "DependentCode"),
                ToText(reader, "DependentTitle"),
                ToInt(reader, "DependentCredits"),
                ToBool(reader, "DependentIsCore"),
                ToText(reader, "DependentDescription"));

            Course required = new Course(
                ToInt(reader, "RequiredCourseId"),
                ToText(reader, "RequiredCode"),
                ToText(reader, "RequiredTitle"),
                ToInt(reader, "RequiredCredits"),
                ToBool(reader, "RequiredIsCore"),
                ToText(reader, "RequiredDescription"));

            RuleType ruleType = (RuleType)Enum.Parse(typeof(RuleType), ToText(reader, "RuleType"), true);

            return new Prerequisite(ToInt(reader, "PrerequisiteId"), dependent, required, ruleType);
        }
    }
}
