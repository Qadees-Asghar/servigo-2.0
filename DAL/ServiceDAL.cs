using SERVIGO.Web.Data;
using SERVIGO.Web.Models;

namespace SERVIGO.Web.DAL
{
    public static class ServiceDAL
    {
        public static int CreateService(ServiceModel s)
        {
            Db.ExecuteNonQuery(@"
                INSERT INTO Services (ProviderID, ServiceName, Description, Price, DurationMinutes, IsActive)
                VALUES (@PID, @Name, @Desc, @Price, @Dur, 1)",
                Db.Param("@PID", s.ProviderID), Db.Param("@Name", s.ServiceName),
                Db.Param("@Desc", s.Description), Db.Param("@Price", s.Price),
                Db.Param("@Dur", s.DurationMinutes));

            return Convert.ToInt32(Db.ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public static void DeleteService(int serviceID)
        {
            Db.ExecuteNonQuery(
                "UPDATE Services SET IsActive = 0 WHERE ServiceID = @SID", Db.Param("@SID", serviceID));
        }

        public static List<ServiceModel> GetByProvider(int providerID)
            => Db.Query(@"
                SELECT ServiceID, ServiceName, Description, Price, IsActive
                FROM   Services
                WHERE  ProviderID = @PID
                ORDER  BY ServiceName",
                r => new ServiceModel
                {
                    ServiceID = r.GetInt("ServiceID"),
                    ServiceName = r.GetStr("ServiceName"),
                    Description = r.GetStr("Description"),
                    Price = r.GetDec("Price"),
                    IsActive = r.GetBool("IsActive")
                },
                Db.Param("@PID", providerID));

        public static List<ServiceModel> SearchServices(string keyword, int? categoryID)
        {
            string sql = @"
                SELECT s.ServiceID, s.ServiceName, s.Description, s.Price,
                       u.FullName AS ProviderName, sc.CategoryName, sp.ProviderID, sp.AverageRating
                FROM   Services s
                JOIN   ServiceProviders sp ON s.ProviderID  = sp.ProviderID
                JOIN   Users             u  ON sp.UserID     = u.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                WHERE  s.IsActive = 1 AND u.IsActive = 1
                  AND  (@Keyword = '' OR s.ServiceName LIKE '%' || @Keyword || '%'
                                     OR u.FullName    LIKE '%' || @Keyword || '%')
                  AND  (@CatID IS NULL OR sp.CategoryID = @CatID)
                ORDER  BY sp.AverageRating DESC, u.FullName";

            return Db.Query(sql,
                r => new ServiceModel
                {
                    ServiceID = r.GetInt("ServiceID"),
                    ServiceName = r.GetStr("ServiceName"),
                    Description = r.GetStr("Description"),
                    Price = r.GetDec("Price"),
                    ProviderName = r.GetStr("ProviderName"),
                    CategoryName = r.GetStr("CategoryName"),
                    ProviderID = r.GetInt("ProviderID"),
                    AverageRating = r.GetDec("AverageRating")
                },
                Db.Param("@Keyword", keyword ?? string.Empty),
                Db.Param("@CatID", (object?)categoryID ?? DBNull.Value));
        }

        public static ServiceModel? GetByID(int serviceID)
            => Db.QueryOne(@"
                SELECT s.*, u.FullName AS ProviderName, sc.CategoryName
                FROM   Services s
                JOIN   ServiceProviders sp ON s.ProviderID  = sp.ProviderID
                JOIN   Users             u  ON sp.UserID     = u.UserID
                JOIN   ServiceCategories sc ON sp.CategoryID = sc.CategoryID
                WHERE  s.ServiceID = @SID",
                r => new ServiceModel
                {
                    ServiceID = r.GetInt("ServiceID"),
                    ProviderID = r.GetInt("ProviderID"),
                    ProviderName = r.GetStr("ProviderName"),
                    CategoryName = r.GetStr("CategoryName"),
                    ServiceName = r.GetStr("ServiceName"),
                    Description = r.GetStr("Description"),
                    Price = r.GetDec("Price"),
                    DurationMinutes = r.GetInt("DurationMinutes"),
                    IsActive = r.GetBool("IsActive")
                },
                Db.Param("@SID", serviceID));
    }
}
