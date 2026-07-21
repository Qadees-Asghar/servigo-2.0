using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class NotificationDAL
    {
        public static int GetUnreadCount(string userID)
            => Convert.ToInt32(Db.ExecuteScalar(
                "SELECT COUNT(*) FROM Notifications WHERE UserID = @UID AND IsRead = 0", Db.Param("@UID", userID)));

        public static List<NotificationModel> GetByUser(string userID, int top = 50)
            => Db.Query($@"
                SELECT NotificationID, Message, IsRead, CreatedAt
                FROM   Notifications
                WHERE  UserID = @UID
                ORDER  BY CreatedAt DESC
                LIMIT  {top}",
                r => new NotificationModel
                {
                    NotificationID = r.GetInt("NotificationID"),
                    Message = r.GetStr("Message"),
                    IsRead = r.GetBool("IsRead"),
                    CreatedAt = r.GetDt("CreatedAt")
                },
                Db.Param("@UID", userID));

        public static void MarkAllRead(string userID)
            => Db.ExecuteNonQuery(
                "UPDATE Notifications SET IsRead = 1 WHERE UserID = @UID AND IsRead = 0", Db.Param("@UID", userID));

        public static void Create(string userID, string message)
            => Db.ExecuteNonQuery(
                "INSERT INTO Notifications (UserID, Message, CreatedAt) VALUES (@UID, @Msg, @Now)",
                Db.Param("@UID", userID), Db.Param("@Msg", message), Db.Param("@Now", DateTime.Now));
    }
}
