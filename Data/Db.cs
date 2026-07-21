using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SERVIGO.Web.Data
{
    public static class Db
    {
        private static string _connectionString = string.Empty;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;

            var builder = new SqliteConnectionStringBuilder(connectionString);
            var dir = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = Schema.Sql;
            cmd.ExecuteNonQuery();

            SeedReferenceData(conn);
        }

        private static void SeedReferenceData(SqliteConnection conn)
        {
            long roleCount = (long)(ExecuteScalarOn(conn, "SELECT COUNT(*) FROM Roles") ?? 0L);
            if (roleCount == 0)
            {
                ExecuteNonQueryOn(conn, "INSERT INTO Roles (RoleID, RoleName) VALUES (1,'Admin'), (2,'Customer'), (3,'ServiceProvider')");
            }

            long statusCount = (long)(ExecuteScalarOn(conn, "SELECT COUNT(*) FROM BookingStatuses") ?? 0L);
            if (statusCount == 0)
            {
                ExecuteNonQueryOn(conn,
                    "INSERT INTO BookingStatuses (StatusID, StatusName) VALUES " +
                    "(1,'Pending'), (2,'Accepted'), (3,'Completed'), (4,'Cancelled'), (5,'Rejected')");
            }

            long catCount = (long)(ExecuteScalarOn(conn, "SELECT COUNT(*) FROM ServiceCategories") ?? 0L);
            if (catCount == 0)
            {
                ExecuteNonQueryOn(conn, @"
                    INSERT INTO ServiceCategories (CategoryName) VALUES
                    ('Electrician'), ('Plumber'), ('Mechanic'), ('Laundry'),
                    ('Painter'), ('Carpenter'), ('Cleaner'), ('AC Repair'), ('Mason'), ('Gardener')");
            }
        }

        public static SqliteConnection GetConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
            return conn;
        }

        // ── Parameter factory ────────────────────────────────────────────────

        public static SqliteParameter Param(string name, object? value)
        {
            object stored = value switch
            {
                null            => DBNull.Value,
                DateTime dt     => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                TimeSpan ts     => ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                decimal dec     => dec.ToString(CultureInfo.InvariantCulture),
                bool b          => b ? 1 : 0,
                _               => value
            };
            return new SqliteParameter(name, stored ?? DBNull.Value);
        }

        // ── Core execution helpers ───────────────────────────────────────────

        public static int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
        {
            using var conn = GetConnection();
            return ExecuteNonQueryOn(conn, sql, parameters);
        }

        private static int ExecuteNonQueryOn(SqliteConnection conn, string sql, params SqliteParameter[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, params SqliteParameter[] parameters)
        {
            using var conn = GetConnection();
            return ExecuteScalarOn(conn, sql, parameters);
        }

        private static object? ExecuteScalarOn(SqliteConnection conn, string sql, params SqliteParameter[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        public static List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();
            var list = new List<T>();
            while (reader.Read())
                list.Add(map(reader));
            return list;
        }

        public static T? QueryOne<T>(string sql, Func<SqliteDataReader, T> map, params SqliteParameter[] parameters)
            where T : class
            => Query(sql, map, parameters).FirstOrDefault();

        // ── Executes several statements inside a single transaction ─────────

        public static void Transaction(Action<SqliteConnection, SqliteTransaction> work)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                work(conn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static int Exec(SqliteConnection conn, SqliteTransaction tx, string sql, params SqliteParameter[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecScalar(SqliteConnection conn, SqliteTransaction tx, string sql, params SqliteParameter[] parameters)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }
    }

    // ── Reader extension helpers (typed column access by name) ──────────────
    public static class ReaderExtensions
    {
        public static string GetStr(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? string.Empty : r.GetString(i);
        }

        public static string? GetStrN(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        public static int GetInt(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
        }

        public static bool GetBool(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return !r.IsDBNull(i) && Convert.ToInt64(r.GetValue(i)) != 0;
        }

        public static decimal GetDec(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return 0m;
            var raw = r.GetValue(i);
            return raw switch
            {
                string s => decimal.Parse(s, CultureInfo.InvariantCulture),
                double d => (decimal)d,
                long l   => l,
                _        => Convert.ToDecimal(raw, CultureInfo.InvariantCulture)
            };
        }

        public static DateTime GetDt(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return default;
            return DateTime.Parse(r.GetString(i), CultureInfo.InvariantCulture);
        }

        public static DateTime? GetDtN(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : DateTime.Parse(r.GetString(i), CultureInfo.InvariantCulture);
        }

        public static TimeSpan GetTs(this SqliteDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            if (r.IsDBNull(i)) return default;
            return TimeSpan.Parse(r.GetString(i), CultureInfo.InvariantCulture);
        }
    }
}
