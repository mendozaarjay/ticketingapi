using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;


namespace Ticketing.WebApi
{
    public static class SCObjects
    {
        public static string ExecNonQuery(SqlCommand cmd, string SqlConnectionString)
        {
            string result = string.Empty;
            SqlConnection cn = new SqlConnection();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
                result = "Proccess has been successfully completed.";
            }
            catch (Exception ex)
            {
                result = "Error/s:\n" + ex.Message;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return result;
        }
        public static async Task<string> ExecNonQueryAsync(SqlCommand cmd, string SqlConnectionString)
        {
            string result = string.Empty;
            SqlConnection cn = new SqlConnection();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                await cmd.ExecuteNonQueryAsync();
                result = "Proccess has been successfully completed.";
            }
            catch (Exception ex)
            {
                result = "Error/s:\n" + ex.Message;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return result;
        }

        public static DataTable ExecGetData(SqlCommand cmd, string SqlConnectionString)
        {
            DataTable dt = new DataTable();
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                da.SelectCommand = cmd;
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                dt = null;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return dt;
        }
        public static async Task<DataTable> ExecGetDataAsync(SqlCommand cmd, string SqlConnectionString)
        {
            DataTable dt = new DataTable();
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                await cn.OpenAsync();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                da.SelectCommand = cmd;
                await Task.Run(() => da.Fill(dt));
            }
            catch (Exception ex)
            {
                dt = null;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return dt;
        }
        public static DataTable LoadDataTable(string SqlQuery, string SqlConnectionString)
        {
            DataTable dt = new DataTable();
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = SqlQuery;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                da.SelectCommand = cmd;
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                dt = null;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return dt;
        }
        public static async Task<DataTable> LoadDataTableAsync(string SqlQuery, string SqlConnectionString)
        {
            DataTable dt = new DataTable();
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = new SqlCommand();
            try
            {
                cn.ConnectionString = SqlConnectionString;
                await cn.OpenAsync();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = SqlQuery;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                da.SelectCommand = cmd;
                await Task.Run(() => da.Fill(dt));
            }
            catch (Exception ex)
            {
                dt = null;
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return dt;
        }
        public static string ReturnText(string SqlQuery, string SqlConnectionString)
        {
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = new SqlCommand();
            var result = string.Empty;
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = SqlQuery;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                result = cmd.ExecuteScalar().ToString();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return result;
        }
        public static async Task<string> ReturnTextAsync(string SqlQuery, string SqlConnectionString)
        {
            SqlConnection cn = new SqlConnection();
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = new SqlCommand();
            var result = string.Empty;
            try
            {
                cn.ConnectionString = SqlConnectionString;
                cn.Open();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = SqlQuery;
                cmd.Connection = cn;
                cmd.CommandTimeout = 0;
                var _result = await cmd.ExecuteScalarAsync();
                result = _result.ToString();
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return result;
        }
        /// <summary>
        /// This will return a string confirmation if the stored procedure was successfully executed.
        /// </summary>
        /// <param name="cmd">Sql Command</param>
        /// <param name="SqlConnectionString">Sql Connection String</param>
        /// <returns></returns>
        public static string ExecuteNonQueryWithReturn(SqlCommand cmd, string SqlConnectionString)
        {
            string _returnThis = string.Empty;
            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = SqlConnectionString;
            cn.Open();
            SqlTransaction sqlTransaction = cn.BeginTransaction();
            try
            {
                cmd.Connection = cn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Transaction = sqlTransaction;
                sqlTransaction.Commit();
                _returnThis = cmd.ExecuteScalar().ToString();

            }
            catch (Exception ex)
            {
                _returnThis = "Errors.\n" + ex.Message;
                sqlTransaction.Rollback();
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return _returnThis;
        }
        /// <summary>
        /// This will return a string confirmation if the stored procedure was successfully executed asynchronously.
        /// </summary>
        /// <param name="cmd">Sql Command</param>
        /// <param name="SqlConnectionString">Sql Connection String</param>
        /// <returns></returns>
        public static async Task<string> ExecuteNonQueryWithReturnAsync(SqlCommand cmd, string SqlConnectionString)
        {
            string _returnThis = string.Empty;
            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = SqlConnectionString;
            await cn.OpenAsync();
            SqlTransaction sqlTransaction = cn.BeginTransaction();
            try
            {
                cmd.Connection = cn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Transaction = sqlTransaction;
                var a = await cmd.ExecuteScalarAsync();
                sqlTransaction.Commit();
                _returnThis = a.ToString();
            }
            catch (Exception ex)
            {
                _returnThis = "Errors.\n" + ex.Message;
                sqlTransaction.Rollback();
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
            return _returnThis;
        }
    }
}