using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class RatingDAL
    {
        public static bool HasRated(int bookingID)
            => Convert.ToInt64(Db.ExecuteScalar(
                "SELECT COUNT(*) FROM Ratings WHERE BookingID = @BID", Db.Param("@BID", bookingID))) > 0;

        public static void SubmitRating(int bookingID, int providerID, string customerID, int stars, string comment)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO Ratings (BookingID, ProviderID, CustomerID, Stars, Comment, CreatedAt)
                VALUES (@BID, @PID, @CID, @Stars, @Comment, @Now)",
                Db.Param("@BID", bookingID), Db.Param("@PID", providerID), Db.Param("@CID", customerID),
                Db.Param("@Stars", stars),
                Db.Param("@Comment", string.IsNullOrWhiteSpace(comment) ? (object)DBNull.Value : comment),
                Db.Param("@Now", DateTime.Now));

            RecalculateAverage(providerID);
        }

        public static void UpdateRating(int bookingID, int stars, string comment)
        {
            var providerID = Convert.ToInt32(Db.ExecuteScalar(
                "SELECT ProviderID FROM Ratings WHERE BookingID = @BID", Db.Param("@BID", bookingID)));

            Db.ExecuteNonQuery(@"
                UPDATE Ratings SET Stars = @Stars, Comment = @Comment WHERE BookingID = @BID",
                Db.Param("@BID", bookingID), Db.Param("@Stars", stars),
                Db.Param("@Comment", string.IsNullOrWhiteSpace(comment) ? (object)DBNull.Value : comment));

            RecalculateAverage(providerID);
        }

        // Mirrors trg_UpdateAverageRating
        private static void RecalculateAverage(int providerID)
        {
            Db.ExecuteNonQuery(@"
                UPDATE ServiceProviders SET AverageRating = (
                    SELECT CAST(COALESCE(AVG(Stars), 0) AS TEXT) FROM Ratings WHERE ProviderID = @PID
                ) WHERE ProviderID = @PID",
                Db.Param("@PID", providerID));
        }

        public static (int Stars, string Comment) GetRating(int bookingID)
        {
            var row = Db.QueryOne(
                "SELECT Stars, COALESCE(Comment,'') AS Comment FROM Ratings WHERE BookingID = @BID",
                r => new { Stars = r.GetInt("Stars"), Comment = r.GetStr("Comment") },
                Db.Param("@BID", bookingID));
            return row == null ? (0, string.Empty) : (row.Stars, row.Comment);
        }

        public static List<UnreviewedBookingRow> GetUnreviewedBookings(string customerID)
            => Db.Query(@"
                SELECT b.BookingID, s.ServiceName, u.FullName AS ProviderName, sp.ProviderID, ts.SlotDate, s.Price
                FROM   Bookings b
                JOIN   Services        s  ON b.ServiceID = s.ServiceID
                JOIN   TimeSlots       ts ON b.SlotID    = ts.SlotID
                JOIN   ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                JOIN   Users           u  ON sp.UserID   = u.UserID
                WHERE  b.CustomerID = @UID
                  AND  b.StatusID = 3
                  AND  NOT EXISTS (SELECT 1 FROM Ratings r WHERE r.BookingID = b.BookingID)
                ORDER  BY b.BookedAt DESC",
                r => new UnreviewedBookingRow
                {
                    BookingID = r.GetInt("BookingID"),
                    ServiceName = r.GetStr("ServiceName"),
                    ProviderName = r.GetStr("ProviderName"),
                    ProviderID = r.GetInt("ProviderID"),
                    SlotDate = r.GetDt("SlotDate"),
                    Price = r.GetDec("Price")
                },
                Db.Param("@UID", customerID));

        public static List<ReviewedBookingRow> GetReviewedBookings(string customerID)
            => Db.Query(@"
                SELECT b.BookingID, s.ServiceName, u.FullName AS ProviderName, sp.ProviderID, ts.SlotDate, s.Price,
                       r.Stars, COALESCE(r.Comment, '') AS Comment
                FROM   Bookings b
                JOIN   Services        s  ON b.ServiceID = s.ServiceID
                JOIN   TimeSlots       ts ON b.SlotID    = ts.SlotID
                JOIN   ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                JOIN   Users           u  ON sp.UserID   = u.UserID
                JOIN   Ratings         r  ON r.BookingID = b.BookingID
                WHERE  b.CustomerID = @UID
                ORDER  BY r.CreatedAt DESC",
                r => new ReviewedBookingRow
                {
                    BookingID = r.GetInt("BookingID"),
                    ServiceName = r.GetStr("ServiceName"),
                    ProviderName = r.GetStr("ProviderName"),
                    ProviderID = r.GetInt("ProviderID"),
                    SlotDate = r.GetDt("SlotDate"),
                    Price = r.GetDec("Price"),
                    Stars = r.GetInt("Stars"),
                    Comment = r.GetStr("Comment")
                },
                Db.Param("@UID", customerID));

        public static List<RatingRow> GetByProvider(int providerID)
            => Db.Query(@"
                SELECT r.Stars, r.Comment, r.CreatedAt, COALESCE(u.FullName, 'Anonymous') AS CustomerName
                FROM   Ratings r
                LEFT JOIN Users u ON r.CustomerID = u.UserID
                WHERE  r.ProviderID = @PID
                ORDER  BY r.CreatedAt DESC",
                r => new RatingRow
                {
                    Stars = r.GetInt("Stars"),
                    Comment = r.GetStr("Comment"),
                    CreatedAt = r.GetDt("CreatedAt"),
                    CustomerName = r.GetStr("CustomerName")
                },
                Db.Param("@PID", providerID));
    }
}
