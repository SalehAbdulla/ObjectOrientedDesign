using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes the four (and the additional) academic tracks.
    /// </summary>
    public class SpecializationRepository : SqlRepositoryBase<Specialization>
    {
        /// <summary>The shared projection of a track.</summary>
        private const string SelectSql =
            "SELECT SpecializationId, Name, Description FROM dbo.Specializations";

        /// <inheritdoc />
        public override Specialization GetById(int id)
        {
            List<Specialization> matches = this.Query(
                SelectSql + " WHERE SpecializationId = @Id", MapSpecialization, Param("@Id", id));

            return matches.Count == 0 ? null : matches[0];
        }

        /// <inheritdoc />
        public override List<Specialization> GetAll()
        {
            return this.Query(SelectSql + " ORDER BY Name", MapSpecialization);
        }

        /// <inheritdoc />
        public override bool Add(Specialization entity)
        {
            return this.ExecuteInsert(
                "INSERT INTO dbo.Specializations (Name, Description) VALUES (@Name, @Description);" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Param("@Name", entity.Name),
                Param("@Description", entity.Description)) > 0;
        }

        /// <inheritdoc />
        public override bool Update(Specialization entity)
        {
            return this.Execute(
                "UPDATE dbo.Specializations SET Name = @Name, Description = @Description " +
                "WHERE SpecializationId = @Id;",
                Param("@Name", entity.Name),
                Param("@Description", entity.Description),
                Param("@Id", entity.SpecializationId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.Specializations WHERE SpecializationId = @Id;",
                Param("@Id", id)) > 0;
        }

        /// <summary>Maps the current row to a track.</summary>
        private static Specialization MapSpecialization(SqlDataReader reader)
        {
            return new Specialization(
                ToInt(reader, "SpecializationId"),
                ToText(reader, "Name"),
                ToText(reader, "Description"));
        }
    }
}
