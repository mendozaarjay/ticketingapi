using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Ticketing.WebApi.Services
{
    public class DapperServices
    {
        public string ConnectionString { get; set; }

        public async Task<XReadingReport> GetXReadingDetails(string srNo)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();
                var item = await db.QueryFirstOrDefaultAsync<XReadingReport>("EXEC [dbo].[spGetXReadingInformationNew] @SRNo", new {SRNo = srNo});
                return item;
            }
        }
        public async Task<YReadingReport> GetYReadingDetails(string srNo)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();
                var item = await db.QueryFirstOrDefaultAsync<YReadingReport>("EXEC [dbo].[spGetYReadingInformationNew] @SRNo", new { SRNo = srNo });
                return item;
            }
        }
        public async Task<ZReadingReport> GetZReadingDetails(string srNo)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();
                var item = await db.QueryFirstOrDefaultAsync<ZReadingReport>("EXEC [dbo].[spGetZReadingInformationNew] @SRNo", new { SRNo = srNo });
                return item;
            }
        }
    }
}