using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    // Replaces the SQL Server AFTER INSERT/UPDATE/DELETE triggers — the app layer
    // logs the same events explicitly since SQLite has no server-side trigger tooling to manage.
    public static class AuditDAL
    {
        public static void Log(string tableName, string action, string? recordID, string? performedBy, string details)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO AuditLogs (TableName, Action, RecordID, PerformedBy, Details, LoggedAt)
                VALUES (@Table, @Action, @Record, @By, @Details, @Logged)",
                Db.Param("@Table", tableName),
                Db.Param("@Action", action),
                Db.Param("@Record", recordID),
                Db.Param("@By", performedBy),
                Db.Param("@Details", details),
                Db.Param("@Logged", DateTime.Now));
        }

        public static List<AuditLogModel> GetAll(int top = 200)
            => Db.Query($@"
                SELECT LogID, TableName, Action, RecordID, PerformedBy, Details, LoggedAt
                FROM   AuditLogs
                ORDER  BY LoggedAt DESC
                LIMIT  {top}",
                r => new AuditLogModel
                {
                    LogID = r.GetInt("LogID"),
                    TableName = r.GetStr("TableName"),
                    Action = r.GetStr("Action"),
                    RecordID = r.GetStr("RecordID"),
                    PerformedBy = r.GetStr("PerformedBy"),
                    Details = r.GetStr("Details"),
                    LoggedAt = r.GetDt("LoggedAt")
                });
    }
}
