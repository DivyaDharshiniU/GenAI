using System;
using Microsoft.Extensions.Configuration;
using System.Data;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

namespace ApexCounter
{
    // PORT-TODO
    // {"note": "Db is no longer static — register as services.AddScoped<Db>() in Program.cs and inject into all callers (AuthData, CatalogData, CustomerData, OrderData, etc.) that previously used Db.Open(), Db.Query(), etc. as static calls", "files": ["Program.cs", "Code/AuthData.cs", "Code/CatalogData.cs", "Code/CustomerData.cs", "Code/OrderData.cs"]}
    public class Db
    {
        private readonly IConfiguration _configuration;

        public Db(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection Open()
        {
            string connectionString = _configuration.GetConnectionString("ApexCounter");
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = Open())
            {
                return Query(connection, null, sql, parameters);
            }
        }

        public DataTable Query(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
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

        public object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Command(connection, transaction, sql, parameters))
            {
                return command.ExecuteScalar();
            }
        }

        public int Exec(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand command = Command(connection, transaction, sql, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }

        public SqlParameter MoneyParam(string name, decimal value)
        {
            SqlParameter parameter = new SqlParameter(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 2;
            parameter.Value = value;
            return parameter;
        }

        public string Like(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return "%";
            }

            string cleaned = term.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return "%" + cleaned + "%";
        }

        private SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, SqlParameter[] parameters)
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