using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SCAIS.Domain;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Reads and writes course plans and, because the lines are a composition, the
    /// requested courses that belong to them.
    /// </summary>
    /// <remarks>
    /// A plan and its lines live in two tables, so a save is performed inside a
    /// transaction. Deleting a plan needs no special handling for the lines because the
    /// foreign key cascades, which is the database expressing the same composition the
    /// class diagram shows.
    /// </remarks>
    public class CourseRequestRepository : SqlRepositoryBase<CourseRequest>
    {
        /// <summary>The shared projection of a plan header.</summary>
        private const string SelectSql = @"
SELECT  r.RequestId, r.StudentId, r.AdviserId, r.SemesterId,
        r.Status, r.SubmittedDate, r.AdviserRemark
FROM dbo.CourseRequests AS r";

        /// <summary>The projection of the courses inside a plan.</summary>
        private const string SelectLinesSql = @"
SELECT  l.LineId, l.RequestId, l.CourseId, l.EligibilityNote,
        c.Code        AS CourseCode,
        c.Title       AS CourseTitle,
        c.CreditHours AS CourseCredits,
        c.IsCore      AS CourseIsCore,
        c.Description AS CourseDescription
FROM        dbo.CourseRequestLines AS l
INNER JOIN  dbo.Courses            AS c ON c.CourseId = l.CourseId";

        /// <inheritdoc />
        public override CourseRequest GetById(int id)
        {
            List<CourseRequest> matches = this.Query(
                SelectSql + " WHERE r.RequestId = @Id", MapRequest, Param("@Id", id));

            if (matches.Count == 0)
            {
                return null;
            }

            CourseRequest request = matches[0];
            this.LoadLines(request);
            return request;
        }

        /// <inheritdoc />
        public override List<CourseRequest> GetAll()
        {
            List<CourseRequest> requests = this.Query(
                SelectSql + " ORDER BY r.RequestId DESC", MapRequest);

            for (int index = 0; index < requests.Count; index++)
            {
                this.LoadLines(requests[index]);
            }

            return requests;
        }

        /// <summary>Returns every plan submitted by one student.</summary>
        /// <param name="studentId">The student whose plans are wanted.</param>
        /// <returns>The plans, most recent first, each with its lines loaded.</returns>
        public List<CourseRequest> GetByStudent(int studentId)
        {
            List<CourseRequest> requests = this.Query(
                SelectSql + " WHERE r.StudentId = @StudentId ORDER BY r.RequestId DESC",
                MapRequest,
                Param("@StudentId", studentId));

            for (int index = 0; index < requests.Count; index++)
            {
                this.LoadLines(requests[index]);
            }

            return requests;
        }

        /// <summary>
        /// Returns the plans awaiting one adviser's decision. This is the query behind
        /// the adviser's review screen.
        /// </summary>
        /// <param name="adviserId">The adviser whose queue is wanted.</param>
        /// <returns>The submitted plans, oldest first, each with its lines loaded.</returns>
        public List<CourseRequest> GetSubmittedForAdviser(int adviserId)
        {
            List<CourseRequest> requests = this.Query(
                SelectSql + " WHERE r.AdviserId = @AdviserId AND r.Status = 'Submitted' ORDER BY r.RequestId",
                MapRequest,
                Param("@AdviserId", adviserId));

            for (int index = 0; index < requests.Count; index++)
            {
                this.LoadLines(requests[index]);
            }

            return requests;
        }

        /// <summary>
        /// Returns every plan a student has in a given state, used to show the student
        /// what is still waiting on their adviser.
        /// </summary>
        /// <param name="studentId">The student whose plans are wanted.</param>
        /// <param name="status">The state to filter by.</param>
        /// <returns>The matching plans.</returns>
        public List<CourseRequest> GetByStudentAndStatus(int studentId, RequestStatus status)
        {
            List<CourseRequest> requests = this.Query(
                SelectSql + " WHERE r.StudentId = @StudentId AND r.Status = @Status ORDER BY r.RequestId DESC",
                MapRequest,
                Param("@StudentId", studentId),
                Param("@Status", status.ToString()));

            for (int index = 0; index < requests.Count; index++)
            {
                this.LoadLines(requests[index]);
            }

            return requests;
        }

        /// <summary>Counts the plans in one state, used by the dashboards.</summary>
        /// <param name="status">The state to count.</param>
        /// <param name="adviserId">An adviser to restrict the count to, or null for all.</param>
        /// <returns>The number of matching plans.</returns>
        public int CountByStatus(RequestStatus status, int? adviserId)
        {
            string sql = "SELECT COUNT(*) AS Matches FROM dbo.CourseRequests WHERE Status = @Status";

            if (adviserId.HasValue)
            {
                sql += " AND AdviserId = @AdviserId";
            }

            List<int> matches = this.Query(
                sql + ";",
                delegate(SqlDataReader reader) { return ToInt(reader, "Matches"); },
                Param("@Status", status.ToString()),
                Param("@AdviserId", adviserId));

            return matches.Count == 0 ? 0 : matches[0];
        }

        /// <summary>
        /// Creates a plan and all of its lines inside one transaction, so a plan can
        /// never be saved without the courses it asked for.
        /// </summary>
        /// <param name="entity">The plan to save, including its lines.</param>
        /// <returns>True when the plan was created.</returns>
        public override bool Add(CourseRequest entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            using (SqlConnection connection = CreateConnection())
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                int requestId;

                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.CourseRequests (StudentId, AdviserId, SemesterId, Status, SubmittedDate, AdviserRemark)
                      VALUES (@StudentId, @AdviserId, @SemesterId, @Status, @SubmittedDate, @AdviserRemark);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                {
                    command.Parameters.AddWithValue("@StudentId", entity.StudentId);
                    command.Parameters.AddWithValue("@AdviserId",
                        entity.AdviserId.HasValue ? (object)entity.AdviserId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@SemesterId", entity.SemesterId);
                    command.Parameters.AddWithValue("@Status", entity.Status.ToString());
                    command.Parameters.AddWithValue("@SubmittedDate",
                        entity.SubmittedDate.HasValue ? (object)entity.SubmittedDate.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@AdviserRemark",
                        (object)entity.AdviserRemark ?? DBNull.Value);

                    object result = command.ExecuteScalar();
                    requestId = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }

                if (requestId <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                foreach (CourseRequestLine line in entity.Lines)
                {
                    using (SqlCommand command = new SqlCommand(
                        @"INSERT INTO dbo.CourseRequestLines (RequestId, CourseId, EligibilityNote)
                          VALUES (@RequestId, @CourseId, @EligibilityNote);", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@RequestId", requestId);
                        command.Parameters.AddWithValue("@CourseId", line.Course.CourseId);
                        command.Parameters.AddWithValue("@EligibilityNote",
                            (object)line.EligibilityNote ?? DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
        }

        /// <summary>
        /// Persists the outcome of a review. The domain object has already validated the
        /// transition, so this method only has to store the result.
        /// </summary>
        /// <param name="entity">The plan whose state changed.</param>
        /// <returns>True when the row was updated.</returns>
        public override bool Update(CourseRequest entity)
        {
            return this.Execute(
                @"UPDATE dbo.CourseRequests
                  SET AdviserId = @AdviserId, Status = @Status,
                      SubmittedDate = @SubmittedDate, AdviserRemark = @AdviserRemark
                  WHERE RequestId = @Id;",
                Param("@AdviserId", entity.AdviserId),
                Param("@Status", entity.Status.ToString()),
                Param("@SubmittedDate", entity.SubmittedDate),
                Param("@AdviserRemark", entity.AdviserRemark),
                Param("@Id", entity.RequestId)) > 0;
        }

        /// <inheritdoc />
        public override bool Delete(int id)
        {
            return this.Execute("DELETE FROM dbo.CourseRequests WHERE RequestId = @Id;",
                Param("@Id", id)) > 0;
        }

        /// <summary>
        /// Loads the courses of a plan into the plan object. The lines are added through
        /// <see cref="CourseRequest.AddCourse"/>, so the plan stays the only thing that
        /// creates its own lines.
        /// </summary>
        /// <param name="request">The plan to populate.</param>
        private void LoadLines(CourseRequest request)
        {
            List<CourseRequestLine> lines = this.Query(
                SelectLinesSql + " WHERE l.RequestId = @RequestId ORDER BY c.Code",
                MapLine,
                Param("@RequestId", request.RequestId));

            foreach (CourseRequestLine line in lines)
            {
                request.AddCourse(line.Course, line.EligibilityNote);
            }
        }

        /// <summary>Maps the current row to a plan header.</summary>
        private static CourseRequest MapRequest(SqlDataReader reader)
        {
            CourseRequest request = new CourseRequest(
                ToInt(reader, "RequestId"),
                ToInt(reader, "StudentId"),
                ToInt(reader, "SemesterId"),
                ToNullableInt(reader, "AdviserId"));

            // The constructor always starts a plan as a Draft, so a plan that has moved
            // on is restored through RestoreState rather than by writing the status field.
            string status = ToText(reader, "Status");
            string remark = IsNull(reader, "AdviserRemark") ? null : ToText(reader, "AdviserRemark");

            if (status != "Draft")
            {
                request.RestoreState(status, ToNullableDate(reader, "SubmittedDate"), remark);
            }

            return request;
        }

        /// <summary>Maps the current row to a requested course line.</summary>
        private static CourseRequestLine MapLine(SqlDataReader reader)
        {
            Course course = new Course(
                ToInt(reader, "CourseId"),
                ToText(reader, "CourseCode"),
                ToText(reader, "CourseTitle"),
                ToInt(reader, "CourseCredits"),
                ToBool(reader, "CourseIsCore"),
                ToText(reader, "CourseDescription"));

            string note = IsNull(reader, "EligibilityNote") ? null : ToText(reader, "EligibilityNote");

            return new CourseRequestLine(
                ToInt(reader, "LineId"),
                ToInt(reader, "RequestId"),
                course,
                note);
        }
    }
}
