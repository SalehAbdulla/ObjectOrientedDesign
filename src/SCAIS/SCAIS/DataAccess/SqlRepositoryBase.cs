using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SCAIS.Interfaces;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Supplies the shared ADO.NET plumbing that every repository needs, so that each
    /// concrete repository contains only the SQL and the mapping for its own table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an abstract class rather than an interface because it carries a real
    /// implementation that the subclasses reuse. It implements
    /// <see cref="IRepository{T}"/> so that callers can depend on the contract, and it
    /// declares the five CRUD operations abstract so that each repository must supply
    /// its own SQL. A class can therefore never become a repository by accident.
    /// </para>
    /// <para>
    /// Every command is parameterised. No SQL string in this project is ever built by
    /// concatenating user input, which removes SQL injection as a category of defect.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The domain type this repository stores.</typeparam>
    public abstract class SqlRepositoryBase<T> : IRepository<T>
    {
        /// <summary>
        /// Opens a connection through the Singleton <see cref="DatabaseManager"/>.
        /// </summary>
        /// <returns>An open connection that the caller must dispose.</returns>
        protected static SqlConnection CreateConnection()
        {
            return DatabaseManager.Instance.CreateConnection();
        }

        /// <inheritdoc />
        public abstract T GetById(int id);

        /// <inheritdoc />
        public abstract List<T> GetAll();

        /// <inheritdoc />
        public abstract bool Add(T entity);

        /// <inheritdoc />
        public abstract bool Update(T entity);

        /// <inheritdoc />
        public abstract bool Delete(int id);

        /// <summary>
        /// Runs a query and maps each row with the supplied delegate.
        /// </summary>
        /// <typeparam name="TResult">The type produced for each row.</typeparam>
        /// <param name="sql">The parameterised SQL to run.</param>
        /// <param name="map">A delegate that turns the current reader position into a value.</param>
        /// <param name="parameters">The parameters for the command.</param>
        /// <returns>The mapped rows, never null.</returns>
        protected List<TResult> Query<TResult>(string sql, Func<SqlDataReader, TResult> map,
                                               params SqlParameter[] parameters)
        {
            if (map == null)
            {
                throw new ArgumentNullException("map");
            }

            List<TResult> results = new List<TResult>();

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameters(command, parameters);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(map(reader));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Runs an INSERT, UPDATE or DELETE and returns the number of rows affected.
        /// </summary>
        /// <param name="sql">The parameterised SQL to run.</param>
        /// <param name="parameters">The parameters for the command.</param>
        /// <returns>The number of rows affected.</returns>
        protected int Execute(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameters(command, parameters);
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Runs an INSERT and returns the identity value the database generated.
        /// </summary>
        /// <param name="sql">The parameterised SQL to run, including SELECT SCOPE_IDENTITY().</param>
        /// <param name="parameters">The parameters for the command.</param>
        /// <returns>The new identity value, or zero when none was produced.</returns>
        protected int ExecuteInsert(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameters(command, parameters);
                object result = command.ExecuteScalar();

                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        /// <summary>Builds a parameter whose value may be null.</summary>
        /// <param name="name">The parameter name, including the leading at sign.</param>
        /// <param name="value">The value, or null.</param>
        /// <returns>A parameter ready to be added to a command.</returns>
        protected static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value == null ? (object)DBNull.Value : value);
        }

        /// <summary>Adds the supplied parameters to a command, skipping a null array.</summary>
        private static void AddParameters(SqlCommand command, SqlParameter[] parameters)
        {
            if (parameters == null)
            {
                return;
            }

            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index] != null)
                {
                    command.Parameters.Add(parameters[index]);
                }
            }
        }

        /// <summary>Reads a non-nullable integer from the reader.</summary>
        protected static int ToInt(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        /// <summary>Reads a nullable integer from the reader.</summary>
        protected static int? ToNullableInt(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        /// <summary>Reads a nullable decimal from the reader.</summary>
        protected static decimal? ToNullableDecimal(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value ? (decimal?)null : Convert.ToDecimal(value);
        }

        /// <summary>Reads text from the reader, returning an empty string for a null column.</summary>
        protected static string ToText(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        /// <summary>Reads a nullable date from the reader.</summary>
        protected static DateTime? ToNullableDate(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        /// <summary>Reads a bit column from the reader.</summary>
        protected static bool ToBool(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value != null && value != DBNull.Value && Convert.ToBoolean(value);
        }

        /// <summary>Returns true when the reader holds a null value for the column.</summary>
        protected static bool IsNull(SqlDataReader reader, string column)
        {
            object value = reader[column];
            return value == null || value == DBNull.Value;
        }
    }
}
