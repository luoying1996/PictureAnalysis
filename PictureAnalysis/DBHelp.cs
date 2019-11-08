using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace PictureAnalysis
{
    public class DBHelp
    {
        //数据库连接字符串(web.config来配置)，可以动态更改connectionString支持多数据库.		
        public static string connectionString = ConfigurationManager.AppSettings["ConnectionString"]?.ToString();
        public static DataSet Query(string SQLString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlcom = new SqlCommand())
                {
                    PrepareCommand(sqlcom, conn, CommandType.Text, SQLString);
                    SqlDataAdapter sda = new SqlDataAdapter();
                    sda.SelectCommand = sqlcom;
                    DataSet ds = new DataSet();
                    sda.Fill(ds);
                    sqlcom.Parameters.Clear();
                    return ds;
                }
            }
        }
        public static int ExecuteSql(string SQLString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, CommandType.Text, SQLString);
                        int rows = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();

                        return rows;
                    }
                    catch (System.Data.SqlClient.SqlException E)
                    {
                        throw new Exception(E.Message);
                    }
                }
            }
        }
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, CommandType cmdType, string cmdText)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            cmd.CommandType = cmdType;
        }
    }
}
