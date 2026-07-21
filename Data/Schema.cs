namespace SERVIGO.Web.Data
{
    // SQLite schema — auto-created on first run, no SSMS or external DB server required.
    public static class Schema
    {
        public const string Sql = @"
CREATE TABLE IF NOT EXISTS Roles (
    RoleID   INTEGER PRIMARY KEY,
    RoleName TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS BookingStatuses (
    StatusID   INTEGER PRIMARY KEY,
    StatusName TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS ServiceCategories (
    CategoryID   INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryName TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Users (
    UserID       TEXT PRIMARY KEY,
    FullName     TEXT NOT NULL,
    Email        TEXT NOT NULL UNIQUE,
    Phone        TEXT NOT NULL UNIQUE,
    CNIC         TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    RoleID       INTEGER NOT NULL REFERENCES Roles(RoleID),
    IsActive     INTEGER NOT NULL DEFAULT 1,
    CreatedAt    TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ServiceProviders (
    ProviderID    INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID        TEXT NOT NULL UNIQUE REFERENCES Users(UserID),
    CategoryID    INTEGER NOT NULL REFERENCES ServiceCategories(CategoryID),
    Description   TEXT,
    IsApproved    INTEGER NOT NULL DEFAULT 0,
    AverageRating TEXT NOT NULL DEFAULT '0',
    CreatedAt     TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Services (
    ServiceID       INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderID      INTEGER NOT NULL REFERENCES ServiceProviders(ProviderID),
    ServiceName     TEXT NOT NULL,
    Description     TEXT,
    Price           TEXT NOT NULL,
    DurationMinutes INTEGER NOT NULL DEFAULT 0,
    IsActive        INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS TimeSlots (
    SlotID      INTEGER PRIMARY KEY AUTOINCREMENT,
    ProviderID  INTEGER NOT NULL REFERENCES ServiceProviders(ProviderID),
    SlotDate    TEXT NOT NULL,
    StartTime   TEXT NOT NULL,
    EndTime     TEXT NOT NULL,
    IsAvailable INTEGER NOT NULL DEFAULT 1,
    UNIQUE (ProviderID, SlotDate, StartTime)
);

CREATE TABLE IF NOT EXISTS Bookings (
    BookingID  INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerID TEXT NOT NULL REFERENCES Users(UserID),
    SlotID     INTEGER NOT NULL REFERENCES TimeSlots(SlotID),
    ServiceID  INTEGER NOT NULL REFERENCES Services(ServiceID),
    StatusID   INTEGER NOT NULL REFERENCES BookingStatuses(StatusID),
    Notes      TEXT,
    BookedAt   TEXT NOT NULL,
    UpdatedAt  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Notifications (
    NotificationID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID         TEXT NOT NULL REFERENCES Users(UserID),
    Message        TEXT NOT NULL,
    IsRead         INTEGER NOT NULL DEFAULT 0,
    CreatedAt      TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Ratings (
    RatingID   INTEGER PRIMARY KEY AUTOINCREMENT,
    BookingID  INTEGER NOT NULL UNIQUE,
    ProviderID INTEGER NOT NULL,
    CustomerID TEXT,
    Stars      INTEGER NOT NULL CHECK (Stars BETWEEN 1 AND 5),
    Comment    TEXT,
    CreatedAt  TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (BookingID)  REFERENCES Bookings(BookingID),
    FOREIGN KEY (ProviderID) REFERENCES ServiceProviders(ProviderID)
);

CREATE TABLE IF NOT EXISTS FeedbackReports (
    ReportID     INTEGER PRIMARY KEY AUTOINCREMENT,
    SubmittedBy  TEXT NOT NULL,
    ReportType   TEXT NOT NULL,
    TargetUserID TEXT,
    Subject      TEXT NOT NULL,
    Description  TEXT NOT NULL,
    IsResolved   INTEGER NOT NULL DEFAULT 0,
    ResolvedAt   TEXT,
    CreatedAt    TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    LogID       INTEGER PRIMARY KEY AUTOINCREMENT,
    TableName   TEXT NOT NULL,
    Action      TEXT NOT NULL,
    RecordID    TEXT,
    PerformedBy TEXT,
    Details     TEXT,
    LoggedAt    TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS IX_Users_Email          ON Users(Email);
CREATE INDEX IF NOT EXISTS IX_Users_RoleID         ON Users(RoleID);
CREATE INDEX IF NOT EXISTS IX_Bookings_CustomerID  ON Bookings(CustomerID);
CREATE INDEX IF NOT EXISTS IX_Bookings_StatusID    ON Bookings(StatusID);
CREATE INDEX IF NOT EXISTS IX_Bookings_SlotID      ON Bookings(SlotID);
CREATE INDEX IF NOT EXISTS IX_TimeSlots_Provider   ON TimeSlots(ProviderID, SlotDate);
CREATE INDEX IF NOT EXISTS IX_Notifications_User   ON Notifications(UserID, IsRead);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Table      ON AuditLogs(TableName, LoggedAt);
CREATE INDEX IF NOT EXISTS IX_Services_Provider    ON Services(ProviderID);
CREATE INDEX IF NOT EXISTS IX_Ratings_Provider     ON Ratings(ProviderID);
CREATE INDEX IF NOT EXISTS IX_FeedbackReports_By   ON FeedbackReports(SubmittedBy);
";
    }
}
