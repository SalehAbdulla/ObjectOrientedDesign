using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;

namespace SCAIS.DataAccess
{
    /// <summary>
    /// Owns the single point of access to the SQL Server LocalDB database.
    /// </summary>
    /// <remarks>
    /// <para><b>Design pattern: Singleton.</b></para>
    /// <para>
    /// <b>The problem.</b> Several unrelated parts of SCAIS read the database at the
    /// same time: the login form reads Users, the student record screen reads
    /// Enrollments, the adviser review screen reads CourseRequests. If each of those
    /// built its own connection manager, the application would hold several
    /// independent connection strings and several open connections to the same
    /// LocalDB database, which causes file-locking errors and inconsistent reads.
    /// SCAIS needs exactly one object that knows the connection string and hands out
    /// connections.
    /// </para>
    /// <para>
    /// <b>Why Singleton.</b> The requirement is literally "one instance for the whole
    /// application". A static helper class cannot hold application state cleanly and
    /// cannot implement an interface; a dependency-injection container would be the
    /// right answer for a web application but adds a framework dependency that is
    /// disproportionate for a desktop course project.
    /// </para>
    /// <para>
    /// <b>Trade-offs.</b> Singleton introduces global state, which makes unit testing
    /// harder because the instance cannot easily be replaced with a fake, and it hides
    /// the dependency from classes that use it. It is the correct trade-off at this
    /// scale, but a larger multi-user system should prefer dependency injection.
    /// </para>
    /// <para>
    /// The class is <c>sealed</c> so a subclass cannot be used to create a second
    /// instance, and the constructor is private so callers must use <see cref="Instance"/>.
    /// </para>
    /// </remarks>
    public sealed class DatabaseManager
    {
        /// <summary>Logical name of the embedded schema script (see SCAIS.csproj).</summary>
        private const string SchemaResourceName = "SCAIS.Database.Schema.sql";

        /// <summary>Logical name of the embedded seed script (see SCAIS.csproj).</summary>
        private const string SeedResourceName = "SCAIS.Database.Seed.sql";

        /// <summary>Guard used to make lazy initialisation thread safe.</summary>
        private static readonly object SyncRoot = new object();

        /// <summary>The single instance of the manager.</summary>
        private static DatabaseManager instance;

        /// <summary>The connection string used for every connection the system opens.</summary>
        private readonly string connectionString;

        /// <summary>
        /// Private constructor: only <see cref="Instance"/> may create the manager,
        /// which is what makes the "single instance" guarantee hold.
        /// </summary>
        private DatabaseManager()
        {
            this.connectionString = ReadConnectionString();
        }

        /// <summary>
        /// Gets the single application-wide instance, creating it on first use.
        /// </summary>
        public static DatabaseManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new DatabaseManager();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>Gets the connection string used by the system.</summary>
        public string ConnectionString
        {
            get { return this.connectionString; }
        }

        /// <summary>
        /// Opens and returns a new connection to the SCAIS database.
        /// Callers own the connection and must dispose it.
        /// </summary>
        /// <returns>An open <see cref="SqlConnection"/>.</returns>
        public SqlConnection CreateConnection()
        {
            SqlConnection connection = new SqlConnection(this.connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Ensures the SCAIS database exists and contains the schema and seed data.
        /// Called once from Program.Main before any form is shown, so the application
        /// always has a usable database on any machine.
        /// </summary>
        public void EnsureDatabase()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.connectionString);
            string databaseName = builder.InitialCatalog;

            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException(
                    "The SCAIS connection string does not specify an Initial Catalog (database name).");
            }

            this.CreateDatabaseIfMissing(builder, databaseName);

            using (SqlConnection connection = this.CreateConnection())
            {
                if (this.SchemaIsMissing(connection))
                {
                    ExecuteScript(connection, ReadEmbeddedScript(SchemaResourceName));
                    ExecuteScript(connection, ReadEmbeddedScript(SeedResourceName));
                }
            }
        }

        /// <summary>
        /// Connects to the master database and issues CREATE DATABASE when the
        /// target database does not yet exist.
        /// </summary>
        private void CreateDatabaseIfMissing(SqlConnectionStringBuilder builder, string databaseName)
        {
            SqlConnectionStringBuilder masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString);
            masterBuilder.InitialCatalog = "master";

            using (SqlConnection master = new SqlConnection(masterBuilder.ConnectionString))
            {
                master.Open();

                const string sql =
                    "IF DB_ID(@DatabaseName) IS NULL " +
                    "BEGIN " +
                    "    EXEC('CREATE DATABASE [' + @DatabaseName + ']'); " +
                    "END";

                using (SqlCommand command = new SqlCommand(sql, master))
                {
                    command.Parameters.AddWithValue("@DatabaseName", databaseName);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns true when the Users table does not exist, which means the schema
        /// has never been applied to this database.
        /// </summary>
        private bool SchemaIsMissing(SqlConnection connection)
        {
            const string sql = "SELECT OBJECT_ID('dbo.Users', 'U')";

            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                object result = command.ExecuteScalar();
                return result == null || result == DBNull.Value;
            }
        }

        /// <summary>
        /// Reads an embedded SQL script by its logical resource name.
        /// </summary>
        private static string ReadEmbeddedScript(string logicalName)
        {
            Assembly assembly = typeof(DatabaseManager).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(logicalName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        "Embedded SQL script '" + logicalName + "' could not be found in the assembly.");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Executes a T-SQL script, sending each GO-separated batch separately
        /// because GO is an SSMS batch separator and is not valid T-SQL.
        /// </summary>
        private static void ExecuteScript(SqlConnection connection, string script)
        {
            foreach (string batch in SplitIntoBatches(script))
            {
                if (string.IsNullOrEmpty(batch.Trim()))
                {
                    continue;
                }

                using (SqlCommand command = new SqlCommand(batch, connection))
                {
                    command.CommandTimeout = 180;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Splits a script on lines that contain only GO.
        /// </summary>
        private static IEnumerable<string> SplitIntoBatches(string script)
        {
            StringBuilder batch = new StringBuilder();

            foreach (string line in script.Split('\n'))
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return batch.ToString();
                    batch.Length = 0;
                }
                else
                {
                    batch.AppendLine(line);
                }
            }

            yield return batch.ToString();
        }

        /// <summary>
        /// Reads the SCAIS connection string from App.config, falling back to a
        /// sensible LocalDB default when the configuration entry is missing.
        /// </summary>
        private static string ReadConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SCAIS"];

            if (settings != null && !string.IsNullOrEmpty(settings.ConnectionString))
            {
                return settings.ConnectionString;
            }

            return @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=SCAIS;Integrated Security=True;MultipleActiveResultSets=True;";
        }
    }
}
