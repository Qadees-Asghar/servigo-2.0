using SERVIGO.Web.Data;
using SERVIGO.Web.Helpers;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class UserDAL
    {
        // ── ID generation ────────────────────────────────────────────────────

        public static string GenerateUserID()
        {
            var result = Db.ExecuteScalar(
                "SELECT UserID FROM Users WHERE UserID LIKE 'SRV-%' ORDER BY UserID DESC LIMIT 1");

            int next = 1;
            if (result is string last && last.Length >= 9 &&
                int.TryParse(last.AsSpan(4, 5), out int n))
                next = n + 1;

            return $"SRV-{next:D5}";
        }

        // ── Existence checks ─────────────────────────────────────────────────

        public static bool AdminExists()
        {
            var result = Db.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE RoleID = 1");
            return Convert.ToInt64(result) > 0;
        }

        public static bool EmailExists(string email, string? excludeUserID = null)
        {
            string sql = excludeUserID == null
                ? "SELECT COUNT(*) FROM Users WHERE Email = @Email COLLATE NOCASE"
                : "SELECT COUNT(*) FROM Users WHERE Email = @Email COLLATE NOCASE AND UserID <> @UID";
            var parms = excludeUserID == null
                ? new[] { Db.Param("@Email", email) }
                : new[] { Db.Param("@Email", email), Db.Param("@UID", excludeUserID) };
            return Convert.ToInt64(Db.ExecuteScalar(sql, parms)) > 0;
        }

        public static bool CNICExists(string cnic, string? excludeUserID = null)
        {
            string sql = excludeUserID == null
                ? "SELECT COUNT(*) FROM Users WHERE CNIC = @CNIC"
                : "SELECT COUNT(*) FROM Users WHERE CNIC = @CNIC AND UserID <> @UID";
            var parms = excludeUserID == null
                ? new[] { Db.Param("@CNIC", cnic) }
                : new[] { Db.Param("@CNIC", cnic), Db.Param("@UID", excludeUserID) };
            return Convert.ToInt64(Db.ExecuteScalar(sql, parms)) > 0;
        }

        public static bool PhoneExists(string phone, string? excludeUserID = null)
        {
            string sql = excludeUserID == null
                ? "SELECT COUNT(*) FROM Users WHERE Phone = @Phone"
                : "SELECT COUNT(*) FROM Users WHERE Phone = @Phone AND UserID <> @UID";
            var parms = excludeUserID == null
                ? new[] { Db.Param("@Phone", phone) }
                : new[] { Db.Param("@Phone", phone), Db.Param("@UID", excludeUserID) };
            return Convert.ToInt64(Db.ExecuteScalar(sql, parms)) > 0;
        }

        // ── CRUD ─────────────────────────────────────────────────────────────

        public static void CreateUser(User user)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO Users (UserID, FullName, Email, Phone, CNIC,
                                   PasswordHash, RoleID, IsActive, CreatedAt)
                VALUES (@UserID, @FullName, @Email, @Phone, @CNIC,
                        @PasswordHash, @RoleID, @IsActive, @CreatedAt)",
                Db.Param("@UserID", user.UserID),
                Db.Param("@FullName", user.FullName),
                Db.Param("@Email", user.Email),
                Db.Param("@Phone", user.Phone),
                Db.Param("@CNIC", user.CNIC),
                Db.Param("@PasswordHash", user.PasswordHash),
                Db.Param("@RoleID", user.RoleID),
                Db.Param("@IsActive", user.IsActive),
                Db.Param("@CreatedAt", user.CreatedAt));

            AuditDAL.Log("Users", "INSERT", user.UserID, user.UserID,
                $"UserID={user.UserID} | Name={user.FullName} | RoleID={user.RoleID}");
        }

        public static void UpdatePassword(string userID, string newHash)
        {
            Db.ExecuteNonQuery(
                "UPDATE Users SET PasswordHash = @Hash WHERE UserID = @UID",
                Db.Param("@Hash", newHash), Db.Param("@UID", userID));
        }

        public static void DeleteUser(string userID)
        {
            Db.Transaction((conn, tx) =>
            {
                void Exec(string sql) => Db.Exec(conn, tx, sql, Db.Param("@UID", userID));

                Exec("DELETE FROM FeedbackReports WHERE SubmittedBy = @UID");
                Exec("DELETE FROM Notifications WHERE UserID = @UID");
                Exec("UPDATE Ratings SET CustomerID = NULL WHERE CustomerID = @UID");

                Exec(@"DELETE FROM Ratings WHERE BookingID IN (
                           SELECT b.BookingID FROM Bookings b
                           JOIN TimeSlots ts ON b.SlotID = ts.SlotID
                           JOIN ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                           WHERE sp.UserID = @UID)");

                Exec("DELETE FROM Bookings WHERE CustomerID = @UID");

                Exec(@"DELETE FROM Bookings WHERE SlotID IN (
                           SELECT ts.SlotID FROM TimeSlots ts
                           JOIN ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                           WHERE sp.UserID = @UID)");

                Exec(@"DELETE FROM Services WHERE ProviderID IN (
                           SELECT ProviderID FROM ServiceProviders WHERE UserID = @UID)");

                Exec(@"DELETE FROM TimeSlots WHERE ProviderID IN (
                           SELECT ProviderID FROM ServiceProviders WHERE UserID = @UID)");

                Exec("DELETE FROM ServiceProviders WHERE UserID = @UID");
                Exec("DELETE FROM Users WHERE UserID = @UID");
            });

            AuditDAL.Log("Users", "DELETE", userID, userID, $"UserID={userID} deleted");
        }

        public static void SetActiveStatus(string userID, bool isActive)
        {
            Db.ExecuteNonQuery(
                "UPDATE Users SET IsActive = @Active WHERE UserID = @UID",
                Db.Param("@Active", isActive), Db.Param("@UID", userID));

            AuditDAL.Log("Users", "UPDATE", userID, userID, $"UserID={userID} | Active={isActive}");
        }

        // ── Authentication ────────────────────────────────────────────────────

        public static User? Login(string email, string userID, string plainPassword)
        {
            var user = Db.QueryOne(@"
                SELECT UserID, FullName, Email, Phone, CNIC,
                       PasswordHash, RoleID, IsActive, CreatedAt
                FROM   Users
                WHERE  Email = @Email COLLATE NOCASE
                  AND  UserID = @UserID
                  AND  IsActive = 1",
                MapToUser,
                Db.Param("@Email", email), Db.Param("@UserID", userID));

            if (user == null) return null;
            return PasswordHelper.Verify(plainPassword, user.PasswordHash) ? user : null;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public static User? GetByID(string userID)
            => Db.QueryOne("SELECT * FROM Users WHERE UserID = @UID", MapToUser, Db.Param("@UID", userID));

        public static List<CustomerRow> GetAllCustomers()
            => Db.Query(@"
                SELECT u.UserID, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt,
                       (SELECT COUNT(*) FROM Bookings b WHERE b.CustomerID = u.UserID) AS TotalBookings
                FROM   Users u
                WHERE  u.RoleID = 2
                ORDER  BY u.CreatedAt DESC",
                r => new CustomerRow
                {
                    UserID = r.GetStr("UserID"),
                    FullName = r.GetStr("FullName"),
                    Email = r.GetStr("Email"),
                    Phone = r.GetStr("Phone"),
                    IsActive = r.GetBool("IsActive"),
                    CreatedAt = r.GetDt("CreatedAt"),
                    TotalBookings = r.GetInt("TotalBookings")
                });

        public static List<ProviderRow> GetAllProviders()
            => Db.Query(@"
                SELECT u.UserID, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt,
                       sc.CategoryName, sp.IsApproved, sp.AverageRating, sp.ProviderID,
                       (SELECT COUNT(*) FROM Bookings b
                          JOIN TimeSlots ts2 ON b.SlotID = ts2.SlotID
                          WHERE ts2.ProviderID = sp.ProviderID AND b.StatusID = 3) AS CompletedBookings
                FROM   Users u
                JOIN   ServiceProviders sp ON u.UserID = sp.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                WHERE  u.RoleID = 3
                ORDER  BY u.CreatedAt DESC",
                r => new ProviderRow
                {
                    ProviderID = r.GetInt("ProviderID"),
                    UserID = r.GetStr("UserID"),
                    FullName = r.GetStr("FullName"),
                    Email = r.GetStr("Email"),
                    Phone = r.GetStr("Phone"),
                    CategoryName = r.GetStr("CategoryName"),
                    IsApproved = r.GetBool("IsApproved"),
                    AverageRating = r.GetDec("AverageRating"),
                    IsActive = r.GetBool("IsActive"),
                    CreatedAt = r.GetDt("CreatedAt"),
                    CompletedBookings = r.GetInt("CompletedBookings")
                });

        // ── Mapper ────────────────────────────────────────────────────────────

        public static User MapToUser(Microsoft.Data.Sqlite.SqliteDataReader row)
        {
            int roleID = row.GetInt("RoleID");
            User user = roleID switch
            {
                1 => new AdminUser(),
                2 => new CustomerUser(),
                3 => new ServiceProviderUser(),
                _ => throw new InvalidOperationException($"Unknown RoleID: {roleID}")
            };

            user.UserID       = row.GetStr("UserID");
            user.FullName     = row.GetStr("FullName");
            user.Email        = row.GetStr("Email");
            user.Phone        = row.GetStr("Phone").Trim();
            user.CNIC         = row.GetStr("CNIC").Trim();
            user.PasswordHash = row.GetStr("PasswordHash");
            user.RoleID       = roleID;
            user.IsActive     = row.GetBool("IsActive");
            user.CreatedAt    = row.GetDt("CreatedAt");
            return user;
        }
    }
}
