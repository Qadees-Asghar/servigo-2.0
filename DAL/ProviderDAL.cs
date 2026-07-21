using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class ProviderDAL
    {
        public static void CreateProvider(int categoryID, string userID, string description)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO ServiceProviders (UserID, CategoryID, Description, IsApproved, AverageRating, CreatedAt)
                VALUES (@UserID, @CatID, @Desc, 0, '0', @Created)",
                Db.Param("@UserID", userID), Db.Param("@CatID", categoryID),
                Db.Param("@Desc", description), Db.Param("@Created", DateTime.Now));

            AuditDAL.Log("ServiceProviders", "INSERT", userID, userID, $"UserID={userID}");
        }

        public static void SetApproval(int providerID, bool approved)
        {
            Db.ExecuteNonQuery(
                "UPDATE ServiceProviders SET IsApproved = @A WHERE ProviderID = @PID",
                Db.Param("@A", approved), Db.Param("@PID", providerID));

            AuditDAL.Log("ServiceProviders", "UPDATE", providerID.ToString(), null,
                $"ProviderID={providerID} | IsApproved={approved}");
        }

        public static void DeleteProvider(int providerID)
        {
            Db.Transaction((conn, tx) =>
            {
                void Exec(string sql) => Db.Exec(conn, tx, sql, Db.Param("@PID", providerID));

                Exec(@"DELETE FROM Ratings WHERE BookingID IN (
                           SELECT b.BookingID FROM Bookings b
                           JOIN TimeSlots ts ON b.SlotID = ts.SlotID
                           WHERE ts.ProviderID = @PID)");

                Exec(@"DELETE FROM Bookings WHERE SlotID IN (
                           SELECT SlotID FROM TimeSlots WHERE ProviderID = @PID)");

                Exec("DELETE FROM Services WHERE ProviderID = @PID");
                Exec("DELETE FROM TimeSlots WHERE ProviderID = @PID");
                Exec("DELETE FROM ServiceProviders WHERE ProviderID = @PID");
            });

            AuditDAL.Log("ServiceProviders", "DELETE", providerID.ToString(), null, $"ProviderID={providerID}");
        }

        public static int? GetProviderIDByUserID(string userID)
        {
            var result = Db.ExecuteScalar(
                "SELECT ProviderID FROM ServiceProviders WHERE UserID = @UID", Db.Param("@UID", userID));
            return result == null || result is DBNull ? null : Convert.ToInt32(result);
        }

        public static ServiceProviderUser? GetProviderByUserID(string userID)
            => Db.QueryOne(@"
                SELECT u.UserID, u.FullName, u.Email, u.Phone, u.CNIC,
                       u.PasswordHash, u.IsActive, u.CreatedAt,
                       sp.ProviderID, sp.CategoryID, sc.CategoryName,
                       sp.Description, sp.IsApproved, sp.AverageRating
                FROM   Users u
                JOIN   ServiceProviders sp ON u.UserID = sp.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                WHERE  u.UserID = @UID",
                MapToProvider, Db.Param("@UID", userID));

        public static ServiceProviderUser? GetByProviderID(int providerID)
            => Db.QueryOne(@"
                SELECT u.UserID, u.FullName, u.Email, u.Phone, u.CNIC,
                       u.PasswordHash, u.IsActive, u.CreatedAt,
                       sp.ProviderID, sp.CategoryID, sc.CategoryName,
                       sp.Description, sp.IsApproved, sp.AverageRating
                FROM   Users u
                JOIN   ServiceProviders sp ON u.UserID = sp.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                WHERE  sp.ProviderID = @PID",
                MapToProvider, Db.Param("@PID", providerID));

        public static List<CategoryOption> GetCategories()
            => Db.Query(
                "SELECT CategoryID, CategoryName FROM ServiceCategories ORDER BY CategoryName",
                r => new CategoryOption { CategoryID = r.GetInt("CategoryID"), CategoryName = r.GetStr("CategoryName") });

        private static ServiceProviderUser MapToProvider(Microsoft.Data.Sqlite.SqliteDataReader row)
            => new()
            {
                UserID        = row.GetStr("UserID"),
                FullName      = row.GetStr("FullName"),
                Email         = row.GetStr("Email"),
                Phone         = row.GetStr("Phone").Trim(),
                CNIC          = row.GetStr("CNIC").Trim(),
                PasswordHash  = row.GetStr("PasswordHash"),
                IsActive      = row.GetBool("IsActive"),
                CreatedAt     = row.GetDt("CreatedAt"),
                ProviderID    = row.GetInt("ProviderID"),
                CategoryID    = row.GetInt("CategoryID"),
                CategoryName  = row.GetStr("CategoryName"),
                Description   = row.GetStr("Description"),
                IsApproved    = row.GetBool("IsApproved"),
                AverageRating = row.GetDec("AverageRating")
            };
    }
}
