using Microsoft.Data.SqlClient;

namespace ZavaLending.Web.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
