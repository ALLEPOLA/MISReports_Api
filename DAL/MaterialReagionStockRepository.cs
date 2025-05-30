using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class MaterialReagionStockRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        public List<MaterialReagionStock> GetMaterialStocks()
        {
            var stocks = new List<MaterialReagionStock>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END) AS Region,
                        i.mat_cd,
                        SUM(i.qty_on_hand) AS qty_on_hand
                    FROM inwrhmtm i
                    JOIN gldeptm d ON i.dept_id = d.dept_id
                    JOIN glcompm c ON d.comp_id = c.comp_id
                    WHERE i.mat_cd LIKE 'D0210%'
                      AND i.status = 2
                      AND i.GRADE_CD = 'NEW'
                      AND (
                        c.parent_id LIKE 'DISCO%'
                        OR c.Grp_comp LIKE 'DISCO%'
                        OR c.comp_id LIKE 'DISCO%'
                      )
                    GROUP BY (CASE WHEN c.lvl_no = 60 THEN c.parent_id ELSE c.Grp_comp END), i.mat_cd
                    ORDER BY 1, 2";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stocks.Add(new MaterialReagionStock
                        {
                            Region = reader["Region"].ToString().Trim(),
                            MatCd = reader["mat_cd"].ToString().Trim(),
                            QtyOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0
                        });
                    }
                }
            }

            return stocks;
        }
    }
}
