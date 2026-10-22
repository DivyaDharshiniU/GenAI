using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ApexCounter
{
    public static class Db
    {
        public static SqlConnection Open()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["ApexCounter"];
            SqlConnection connection = new SqlConnection(settings.ConnectionString);
            connection.Open();
            return connection;
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = Open())
            {
                return Query(connection, null, sql, parameters);
            }
        }

        public static DataTable Query(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Command(connection, transaction, sql, parameters))
            {
                DataTable table = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
                return table;
            }
        }

        public static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Command(connection, transaction, sql, parameters))
            {
                return command.ExecuteScalar();
            }
        }

        public static int Exec(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Command(connection, transaction, sql, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }

        public static SqlParameter MoneyParam(string name, decimal value)
        {
            SqlParameter parameter = new SqlParameter(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
            return parameter;
        }

        public static string Like(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return "%";
            }
            string cleaned = term.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return "%" + cleaned + "%";
        }

        private static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, SqlParameter[] parameters)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    command.Parameters.Add(parameters[i]);
                }
            }
            return command;
        }
    }
}
