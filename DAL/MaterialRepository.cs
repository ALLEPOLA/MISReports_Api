using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class MaterialRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        public List<Material> GetActiveMaterials()
        {
            var materials = new List<Material>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = "SELECT mat_cd, mat_nm FROM inmatm WHERE status = 2";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        materials.Add(new Material
                        {
                            MatCd = reader["mat_cd"].ToString().Trim(),
                            MatNm = reader["mat_nm"].ToString().Trim()
                        });
                    }
                }
            }

            return materials;
        }
    }
}
