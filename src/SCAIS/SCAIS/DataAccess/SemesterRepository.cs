using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the academic terms, and answers which term is current.
    /// </summary>
    /// <remarks>
    /// The "current semester" question is answered from the dates in the database
    /// rather than from a constant in the code, so the system keeps working in later
    /// academic years without being rebuilt.
    /// </remarks>
    public class SemesterRepository : SqlRepositoryBase<Semester>
    {
        /// <summary>The shared projection of a semester.</summary>
        private const string SelectSql =
            "SELECT SemesterId, TermName, AcademicYear, StartDate, EndDate FROM dbo.Semesters";

        /// <inheritdoc />
        public override Semester GetById(int id)
        {
            List<Semester> matches = this.Query(
                SelectSql + " WHERE SemesterId = @Id", MapSemester, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Semester> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY AcademicYear DESC, SemesterId DESC", MapSemester);
        }

        /// <summary>
        /// Returns the semester whose dates contain today.
        /// </summary>
        /// <returns>The current semester, or null when the term dates do not cover today.</returns>
        public Semester GetCurrent()
        {
            List<Semester> matches = this.Query(
                SelectSql + " WHERE CAST(GETDATE() AS DATE) BETWEEN StartDate AND EndDate " +
                "ORDER BY SemesterId", MapSemester);

            return matches.Count == 0 ? null : matches[0];
        }

        /// <summary>
        /// Returns the next semester that starts after today, which is the term a
        /// student plans for. When the calendar has run out it falls back to the most
        /// recent term so that planning screens always have something to show.
        /// </summary>
        /// <returns>The coming semester, never null for a seeded database.</returns>
        public Semester GetNext()
        {
            List<Semester> matches = this.Query(
                SelectSql + " WHERE StartDate > CAST(GETDATE() AS DATE) " +
                "ORDER BY StartDate", MapSemester);

            if (matches.Count > 0)
            {
                return matches[0];
            }

            List<Semester> mostRecent = this.Query(
                SelectSql + " ORDER BY AcademicYear DESC, SemesterId DESC", MapSemester);

            return mostRecent.Count == 0 ? null : mostRecent[0];
        }

        /// <inheritdoc />
        public override bool Add(Semester entity)
        {
            return this.ExecuteInsert(
                "INSERT INTO dbo.Semesters (TermName, AcademicYear, StartDate, EndDate) " +
                "VALUES (@TermName, @AcademicYear, @StartDate, @EndDate);" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@TermName", entity.TermName),
                Param("@AcademicYear", entity.AcademicYear),
                Param("@StartDate", entity.StartDate),
                Param("@EndDate", entity.EndDate)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(Semester entity)
        {
            return this.Execute(
                "UPDATE dbo.Semesters SET TermName = @TermName, AcademicYear = @AcademicYear, " +
                "StartDate = @StartDate, EndDate = @EndDate WHERE SemesterId = @Id;",
                Param("@TermName", entity.TermName),
                Param("@AcademicYear", entity.AcademicYear),
                Param("@StartDate", entity.StartDate),
                Param("@EndDate", entity.EndDate),
                Param("@Id", entity.SemesterId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Semesters WHERE SemesterId = @Id;", Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to a semester.</summary>
        private static Semester MapSemester(SqlDataReader reader)
        {
            return new Semester(
                ToInt(reader, "SemesterId"),
                ToText(reader, "TermName"),
                ToInt(reader, "AcademicYear"),
                ToNullableDate(reader, "StartDate"),
                ToNullableDate(reader, "EndDate"));
        }
    }
}
