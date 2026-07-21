using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class BookingDAL
    {
        // ── Time Slots ────────────────────────────────────────────────────────

        public static int CreateSlot(int providerID, DateTime date, TimeSpan start, TimeSpan end)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO TimeSlots (ProviderID, SlotDate, StartTime, EndTime, IsAvailable)
                VALUES (@PID, @Date, @Start, @End, 1)",
                Db.Param("@PID", providerID), Db.Param("@Date", date.Date),
                Db.Param("@Start", start), Db.Param("@End", end));

            return Convert.ToInt32(Db.ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public static void DeleteSlot(int slotID)
        {
            Db.ExecuteNonQuery(
                "DELETE FROM TimeSlots WHERE SlotID = @SID AND IsAvailable = 1", Db.Param("@SID", slotID));
        }

        public static List<TimeSlotModel> GetSlotsByProvider(int providerID)
            => Db.Query(@"
                SELECT SlotID, SlotDate, StartTime, EndTime, IsAvailable
                FROM   TimeSlots
                WHERE  ProviderID = @PID AND SlotDate >= @Today
                ORDER  BY SlotDate, StartTime",
                r => new TimeSlotModel
                {
                    SlotID = r.GetInt("SlotID"),
                    SlotDate = r.GetDt("SlotDate"),
                    StartTime = r.GetTs("StartTime"),
                    EndTime = r.GetTs("EndTime"),
                    IsAvailable = r.GetBool("IsAvailable")
                },
                Db.Param("@PID", providerID), Db.Param("@Today", DateTime.Today));

        public static List<TimeSlotModel> GetAvailableSlots(int providerID)
            => Db.Query(@"
                SELECT SlotID, SlotDate, StartTime, EndTime
                FROM   TimeSlots
                WHERE  ProviderID  = @PID
                  AND  IsAvailable = 1
                  AND  SlotDate   >= @Today
                  AND  SlotDate   <= @Max
                ORDER  BY SlotDate, StartTime",
                r => new TimeSlotModel
                {
                    SlotID = r.GetInt("SlotID"),
                    SlotDate = r.GetDt("SlotDate"),
                    StartTime = r.GetTs("StartTime"),
                    EndTime = r.GetTs("EndTime")
                },
                Db.Param("@PID", providerID), Db.Param("@Today", DateTime.Today),
                Db.Param("@Max", DateTime.Today.AddDays(7)));

        // ── Bookings ──────────────────────────────────────────────────────────

        // Mirrors sp_CreateBooking: validates availability, 7-day window, not-in-past,
        // no duplicate booking, then creates the booking, locks the slot and notifies the provider.
        public static int CreateBooking(string customerID, int slotID, int serviceID, string? notes)
        {
            int newBookingID = 0;

            Db.Transaction((conn, tx) =>
            {
                var slotRow = Db.ExecScalar(conn, tx,
                    "SELECT IsAvailable FROM TimeSlots WHERE SlotID = @SID", Db.Param("@SID", slotID));
                if (slotRow == null || Convert.ToInt64(slotRow) == 0)
                    throw new InvalidOperationException("Selected time slot is not available.");

                var slotDateObj = Db.ExecScalar(conn, tx,
                    "SELECT SlotDate FROM TimeSlots WHERE SlotID = @SID", Db.Param("@SID", slotID));
                var slotDate = DateTime.Parse((string)slotDateObj!);

                if (slotDate > DateTime.Today.AddDays(7))
                    throw new InvalidOperationException("Booking can only be made up to 7 days in advance.");
                if (slotDate < DateTime.Today)
                    throw new InvalidOperationException("Cannot book a past time slot.");

                var dupCount = Convert.ToInt64(Db.ExecScalar(conn, tx, @"
                    SELECT COUNT(*) FROM Bookings
                    WHERE CustomerID = @CID AND SlotID = @SID AND StatusID NOT IN (4,5)",
                    Db.Param("@CID", customerID), Db.Param("@SID", slotID)));
                if (dupCount > 0)
                    throw new InvalidOperationException("You already have a booking for this slot.");

                var now = DateTime.Now;
                Db.Exec(conn, tx, @"
                    INSERT INTO Bookings (CustomerID, SlotID, ServiceID, StatusID, Notes, BookedAt, UpdatedAt)
                    VALUES (@CID, @SID, @SvcID, 1, @Notes, @Now, @Now)",
                    Db.Param("@CID", customerID), Db.Param("@SID", slotID), Db.Param("@SvcID", serviceID),
                    Db.Param("@Notes", (object?)notes ?? DBNull.Value), Db.Param("@Now", now));

                newBookingID = Convert.ToInt32(Db.ExecScalar(conn, tx, "SELECT last_insert_rowid()")!);

                Db.Exec(conn, tx, "UPDATE TimeSlots SET IsAvailable = 0 WHERE SlotID = @SID", Db.Param("@SID", slotID));

                var providerUserID = Db.ExecScalar(conn, tx, @"
                    SELECT u.UserID FROM TimeSlots ts
                    JOIN ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                    JOIN Users u ON sp.UserID = u.UserID
                    WHERE ts.SlotID = @SID", Db.Param("@SID", slotID));

                Db.Exec(conn, tx, "INSERT INTO Notifications (UserID, Message, CreatedAt) VALUES (@UID, @Msg, @Now)",
                    Db.Param("@UID", providerUserID),
                    Db.Param("@Msg", $"New booking request #{newBookingID} from customer {customerID}"),
                    Db.Param("@Now", now));
            });

            AuditDAL.Log("Bookings", "INSERT", newBookingID.ToString(), customerID,
                $"BookingID={newBookingID} | Customer={customerID} | SlotID={slotID}");

            return newBookingID;
        }

        // Mirrors sp_UpdateBookingStatus: updates status, releases the slot on cancel/reject,
        // and notifies the customer (and provider, on customer cancellation).
        public static void UpdateStatus(int bookingID, int newStatusID, string performedBy)
        {
            Db.Transaction((conn, tx) =>
            {
                var row = Db.Query(
                    "SELECT StatusID, CustomerID, SlotID FROM Bookings WHERE BookingID = @BID",
                    r => new { StatusID = r.GetInt("StatusID"), CustomerID = r.GetStr("CustomerID"), SlotID = r.GetInt("SlotID") },
                    Db.Param("@BID", bookingID)).FirstOrDefault();

                if (row == null) throw new InvalidOperationException("Booking not found.");

                string oldStatus = Convert.ToString(Db.ExecScalar(conn, tx,
                    "SELECT StatusName FROM BookingStatuses WHERE StatusID = @S", Db.Param("@S", row.StatusID)))!;
                string newStatus = Convert.ToString(Db.ExecScalar(conn, tx,
                    "SELECT StatusName FROM BookingStatuses WHERE StatusID = @S", Db.Param("@S", newStatusID)))!;

                var now = DateTime.Now;
                Db.Exec(conn, tx, "UPDATE Bookings SET StatusID = @New, UpdatedAt = @Now WHERE BookingID = @BID",
                    Db.Param("@New", newStatusID), Db.Param("@Now", now), Db.Param("@BID", bookingID));

                if (newStatusID is 4 or 5)
                    Db.Exec(conn, tx, "UPDATE TimeSlots SET IsAvailable = 1 WHERE SlotID = @SID", Db.Param("@SID", row.SlotID));

                Db.Exec(conn, tx, "INSERT INTO Notifications (UserID, Message, CreatedAt) VALUES (@UID, @Msg, @Now)",
                    Db.Param("@UID", row.CustomerID),
                    Db.Param("@Msg", $"Booking #{bookingID} status changed: {oldStatus} -> {newStatus}"),
                    Db.Param("@Now", now));

                if (newStatusID == 4)
                {
                    var providerUserID = Db.ExecScalar(conn, tx, @"
                        SELECT u.UserID FROM TimeSlots ts
                        JOIN ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                        JOIN Users u ON sp.UserID = u.UserID
                        WHERE ts.SlotID = @SID", Db.Param("@SID", row.SlotID));

                    Db.Exec(conn, tx, "INSERT INTO Notifications (UserID, Message, CreatedAt) VALUES (@UID, @Msg, @Now)",
                        Db.Param("@UID", providerUserID),
                        Db.Param("@Msg", $"Booking #{bookingID} was cancelled by the customer."),
                        Db.Param("@Now", now));
                }
            });

            AuditDAL.Log("Bookings", "UPDATE", bookingID.ToString(), performedBy,
                $"BookingID={bookingID} | StatusID={newStatusID}");
        }

        public static List<BookingModel> GetCustomerBookings(string customerID)
            => Db.Query(@"
                SELECT b.BookingID, b.StatusID, bs.StatusName, b.BookedAt, b.Notes,
                       s.ServiceName, s.Price, u.FullName AS ProviderName,
                       ts.SlotDate, ts.StartTime, ts.EndTime, sp.ProviderID,
                       CASE WHEN EXISTS (SELECT 1 FROM Ratings r WHERE r.BookingID = b.BookingID) THEN 1 ELSE 0 END AS HasRated
                FROM   Bookings b
                JOIN   BookingStatuses bs ON b.StatusID  = bs.StatusID
                JOIN   Services        s  ON b.ServiceID = s.ServiceID
                JOIN   TimeSlots       ts ON b.SlotID    = ts.SlotID
                JOIN   ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                JOIN   Users           u  ON sp.UserID   = u.UserID
                WHERE  b.CustomerID = @UID
                ORDER  BY b.BookedAt DESC",
                MapBooking, Db.Param("@UID", customerID));

        public static List<BookingModel> GetProviderBookings(int providerID)
            => Db.Query(@"
                SELECT b.BookingID, b.StatusID, bs.StatusName, b.BookedAt, b.Notes,
                       s.ServiceName, s.Price, uc.FullName AS CustomerName, uc.Phone AS CustomerPhone,
                       ts.SlotDate, ts.StartTime, ts.EndTime
                FROM   Bookings b
                JOIN   BookingStatuses bs ON b.StatusID  = bs.StatusID
                JOIN   Services        s  ON b.ServiceID = s.ServiceID
                JOIN   TimeSlots       ts ON b.SlotID    = ts.SlotID
                JOIN   Users           uc ON b.CustomerID = uc.UserID
                WHERE  ts.ProviderID = @PID
                ORDER  BY ts.SlotDate DESC, ts.StartTime DESC",
                MapBooking, Db.Param("@PID", providerID));

        public static List<BookingModel> GetAllBookings()
            => Db.Query(@"
                SELECT b.BookingID, bs.StatusName, b.BookedAt, s.ServiceName, s.Price,
                       uc.FullName AS CustomerName, up.FullName AS ProviderName,
                       ts.SlotDate, ts.StartTime, ts.EndTime
                FROM   Bookings b
                JOIN   BookingStatuses  bs ON b.StatusID  = bs.StatusID
                JOIN   Services         s  ON b.ServiceID = s.ServiceID
                JOIN   TimeSlots        ts ON b.SlotID    = ts.SlotID
                JOIN   Users            uc ON b.CustomerID = uc.UserID
                JOIN   ServiceProviders sp ON ts.ProviderID = sp.ProviderID
                JOIN   Users            up ON sp.UserID    = up.UserID
                ORDER  BY b.BookedAt DESC",
                MapBooking);

        public static List<BookingSummaryRow> GetBookingSummary()
            => Db.Query(@"
                SELECT bs.StatusName, COUNT(b.BookingID) AS TotalBookings
                FROM   Bookings b
                JOIN   BookingStatuses bs ON b.StatusID = bs.StatusID
                GROUP  BY bs.StatusName",
                r => new BookingSummaryRow { StatusName = r.GetStr("StatusName"), TotalBookings = r.GetInt("TotalBookings") });

        public static List<ProviderStatsRow> GetProviderStats()
            => Db.Query(@"
                SELECT sp.ProviderID, u.FullName AS ProviderName, sc.CategoryName,
                       COUNT(b.BookingID) AS TotalBookings,
                       SUM(CASE WHEN b.StatusID = 3 THEN 1 ELSE 0 END) AS Completed,
                       SUM(CASE WHEN b.StatusID IN (4,5) THEN 1 ELSE 0 END) AS CancelledOrRejected,
                       sp.AverageRating
                FROM   ServiceProviders sp
                JOIN   Users             u  ON sp.UserID     = u.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                LEFT JOIN TimeSlots    ts ON ts.ProviderID  = sp.ProviderID
                LEFT JOIN Bookings     b  ON b.SlotID       = ts.SlotID
                GROUP  BY sp.ProviderID, u.FullName, sc.CategoryName, sp.AverageRating
                ORDER  BY TotalBookings DESC",
                r => new ProviderStatsRow
                {
                    ProviderID = r.GetInt("ProviderID"),
                    ProviderName = r.GetStr("ProviderName"),
                    CategoryName = r.GetStr("CategoryName"),
                    TotalBookings = r.GetInt("TotalBookings"),
                    Completed = r.GetInt("Completed"),
                    CancelledOrRejected = r.GetInt("CancelledOrRejected"),
                    AverageRating = r.GetDec("AverageRating")
                });

        public static DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats
            {
                TotalCustomers    = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE RoleID = 2")),
                TotalProviders    = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE RoleID = 3")),
                PendingApprovals  = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM ServiceProviders WHERE IsApproved = 0")),
                TotalBookings     = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM Bookings")),
                PendingBookings   = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM Bookings WHERE StatusID = 1")),
                CompletedBookings = Convert.ToInt32(Db.ExecuteScalar("SELECT COUNT(*) FROM Bookings WHERE StatusID = 3"))
            };
            return stats;
        }

        public static int GetProviderCompletedCount(int providerID)
            => Convert.ToInt32(Db.ExecuteScalar(@"
                SELECT COUNT(*) FROM Bookings b
                JOIN TimeSlots ts ON b.SlotID = ts.SlotID
                WHERE ts.ProviderID = @PID AND b.StatusID = 3", Db.Param("@PID", providerID)));

        private static BookingModel MapBooking(Microsoft.Data.Sqlite.SqliteDataReader r)
        {
            var m = new BookingModel
            {
                BookedAt   = r.GetDt("BookedAt"),
                Notes      = r.GetStrN("Notes"),
                ServiceName = r.GetStr("ServiceName"),
                Price      = r.GetDec("Price"),
                SlotDate   = r.GetDt("SlotDate"),
                StartTime  = r.GetTs("StartTime"),
                EndTime    = r.GetTs("EndTime"),
                StatusName = r.GetStr("StatusName")
            };

            for (int i = 0; i < r.FieldCount; i++)
            {
                switch (r.GetName(i))
                {
                    case "BookingID": m.BookingID = r.GetInt("BookingID"); break;
                    case "StatusID": m.StatusID = r.GetInt("StatusID"); break;
                    case "ProviderName": m.ProviderName = r.GetStr("ProviderName"); break;
                    case "CustomerName": m.CustomerName = r.GetStr("CustomerName"); break;
                    case "CustomerPhone": m.CustomerPhone = r.GetStr("CustomerPhone"); break;
                    case "ProviderID": m.ProviderID = r.GetInt("ProviderID"); break;
                    case "HasRated": m.HasRated = r.GetBool("HasRated"); break;
                }
            }
            return m;
        }
    }
}
