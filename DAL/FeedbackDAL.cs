using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class FeedbackDAL
    {
        public static void Submit(string submittedBy, string reportType, string? targetUserID, string subject, string description)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO FeedbackReports (SubmittedBy, ReportType, TargetUserID, Subject, Description, IsResolved, CreatedAt)
                VALUES (@By, @Type, @Target, @Subj, @Desc, 0, @Now)",
                Db.Param("@By", submittedBy), Db.Param("@Type", reportType),
                Db.Param("@Target", (object?)targetUserID ?? DBNull.Value),
                Db.Param("@Subj", subject), Db.Param("@Desc", description), Db.Param("@Now", DateTime.Now));
        }

        public static List<FeedbackReportRow> GetAll()
            => Db.Query(@"
                SELECT f.ReportID, f.ReportType, f.Subject, f.Description, f.IsResolved, f.CreatedAt, f.ResolvedAt,
                       u.FullName AS SubmittedByName, f.SubmittedBy,
                       COALESCE(t.FullName, '') AS TargetName, COALESCE(f.TargetUserID, '') AS TargetUserID
                FROM   FeedbackReports f
                JOIN   Users u ON f.SubmittedBy = u.UserID
                LEFT JOIN Users t ON f.TargetUserID = t.UserID
                ORDER  BY f.IsResolved ASC, f.CreatedAt DESC",
                Map);

        public static List<FeedbackReportRow> GetByUser(string userID)
            => Db.Query(@"
                SELECT ReportID, ReportType, Subject, Description, IsResolved, CreatedAt, ResolvedAt,
                       @UID AS SubmittedBy, '' AS SubmittedByName, '' AS TargetUserID, '' AS TargetName
                FROM   FeedbackReports
                WHERE  SubmittedBy = @UID
                ORDER  BY CreatedAt DESC",
                Map, Db.Param("@UID", userID));

        public static void MarkResolved(int reportID)
            => Db.ExecuteNonQuery(
                "UPDATE FeedbackReports SET IsResolved = 1, ResolvedAt = @Now WHERE ReportID = @RID",
                Db.Param("@Now", DateTime.Now), Db.Param("@RID", reportID));

        public static void MarkUnresolved(int reportID)
            => Db.ExecuteNonQuery(
                "UPDATE FeedbackReports SET IsResolved = 0, ResolvedAt = NULL WHERE ReportID = @RID",
                Db.Param("@RID", reportID));

        private static FeedbackReportRow Map(Microsoft.Data.Sqlite.SqliteDataReader r)
            => new()
            {
                ReportID = r.GetInt("ReportID"),
                ReportType = r.GetStr("ReportType"),
                Subject = r.GetStr("Subject"),
                Description = r.GetStr("Description"),
                IsResolved = r.GetBool("IsResolved"),
                CreatedAt = r.GetDt("CreatedAt"),
                ResolvedAt = r.GetDtN("ResolvedAt"),
                SubmittedBy = r.GetStr("SubmittedBy"),
                SubmittedByName = r.GetStr("SubmittedByName"),
                TargetUserID = r.GetStrN("TargetUserID"),
                TargetName = r.GetStr("TargetName")
            };
    }
}
