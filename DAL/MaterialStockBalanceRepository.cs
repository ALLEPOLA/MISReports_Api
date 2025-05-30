using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class MaterialStockBalanceRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        public List<MaterialStockBalance> GetMaterialStockBalances()
        {
            var balances = new List<MaterialStockBalance>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT
                      T1.MAT_CD,
                      (
                         SELECT CASE WHEN lvl_no = 60 THEN parent_id ELSE grp_comp END
                         FROM glcompm
                         WHERE comp_id IN 
                      (
                          SELECT comp_id FROM gldeptm WHERE dept_id = T1.dept_id
                        )
                      ) AS region,
                      (
                         SELECT CASE WHEN lvl_no = 60 THEN comp_id ELSE parent_id END
                         FROM glcompm
                         WHERE comp_id IN 
                      (
                          SELECT comp_id FROM gldeptm WHERE dept_id = T1.dept_id
                        )
                      ) AS province,
                      (
                       T1.dept_id || ' - ' ||
                      (
                      SELECT dept_nm FROM gldeptm WHERE dept_id = T1.dept_id)
                      ) AS dept_id,
                      T2.MAT_NM,
                      T2.unit_price,
                      SUM(T1.QTY_ON_HAND) AS committed_cost,
                      T1.UOM_CD
                   FROM INWRHMTM T1
                   JOIN INMATM T2 ON T2.MAT_CD = T1.MAT_CD
                   WHERE
                     T1.dept_id IN 
                     
                      (
                         SELECT dept_id
                         FROM gldeptm
                         WHERE comp_id IN 
                      (
                         SELECT comp_id
                         FROM glcompm
                         WHERE status = 2
                             AND (
                               parent_id LIKE 'DISCO%' OR
                               grp_comp  LIKE 'DISCO%' OR
                               comp_id    LIKE 'DISCO%'
                             )
                        )
                     )
                     AND T1.MAT_CD LIKE 'D%'
                     AND T1.GRADE_CD = 'NEW'
                     AND T1.status   = 2
                   GROUP BY
                     T1.MAT_CD,
                     T2.MAT_NM,
                     T1.UOM_CD,
                     T1.dept_id,
                     T2.unit_price
                   ORDER BY 1, 2, 3, 4";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        balances.Add(new MaterialStockBalance
                        {
                            MatCd = reader["MAT_CD"].ToString().Trim(),
                            Region = reader["region"].ToString().Trim(),
                            Province = reader["province"].ToString().Trim(),
                            DeptId = reader["dept_id"].ToString().Trim(),
                            MatNm = reader["MAT_NM"].ToString().Trim(),
                            UnitPrice = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0,
                            CommittedCost = reader["committed_cost"] != DBNull.Value ? Convert.ToDecimal(reader["committed_cost"]) : 0,
                            UomCd = reader["UOM_CD"].ToString().Trim()
                        });
                    }
                }
            }

            return balances;
        }
    }
}
