using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class RegionwiseTrialRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultOracle"].ConnectionString;

        public List<RegionwiseTrialModel> GetRegionwiseTrialData(string COMP_ID, int YR_IND, int MTH_IND)
        {
            var trialBalanceList = new List<RegionwiseTrialModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string sql = @"
                    SELECT 
                        glledgrm.GL_CD,
                        glledgrm.AC_CD,
                        glledgrm.GL_NM,
                        CASE WHEN SUBSTR(glledgrm.AC_CD,1,1) IN ('A') THEN 'A'  
                             WHEN SUBSTR(glledgrm.AC_CD,1,1) IN ('E') THEN 'E'
                             WHEN SUBSTR(glledgrm.AC_CD,1,1) IN ('L') THEN 'L' 
                             ELSE 'R' END as TitleFlag,
                        ROUND(SUM(gllegbal.OP_BAL), 2) as OP_BAL,
                        ROUND(SUM(gllegbal.DR_AMT), 2) as DR_AMT,
                        ROUND(SUM(gllegbal.CR_AMT), 2) as CR_AMT,
                        ROUND(SUM(gllegbal.CL_BAL), 2) as CL_BAL,
                        CASE WHEN gldeptm.COMP_ID = :COMP_ID THEN gldeptm.DEPT_ID  
                             WHEN glcompm.PARENT_ID = :COMP_ID THEN glcompm.COMP_ID
                             WHEN glcompm.GRP_COMP = :COMP_ID THEN glcompm.PARENT_ID  
                             ELSE '' END as COSTCTR,
                        CASE WHEN gldeptm.COMP_ID = :COMP_ID THEN gldeptm.DEPT_NM  
                             WHEN glcompm.PARENT_ID = :COMP_ID THEN glcompm.COMP_NM
                             WHEN glcompm.GRP_COMP = :COMP_ID THEN 
                                 (SELECT DISTINCT a.COMP_NM FROM glcompm a 
                                  WHERE a.COMP_ID=glcompm.PARENT_ID AND a.STATUS=2)  
                             ELSE '' END as Comp_NM,
                        gldeptm.DEPT_ID
                    FROM gllegbal, glledgrm, glacgrpm, gltitlm, gldeptm, glcompm
                    WHERE glledgrm.GL_CD = gllegbal.GL_CD
                    AND glledgrm.AC_CD = glacgrpm.AC_CD
                    AND gllegbal.DEPT_ID = gldeptm.DEPT_ID 
                    AND glcompm.COMP_ID = gldeptm.COMP_ID 
                    AND glacgrpm.DEPT_ID = '900.00'
                    AND gldeptm.STATUS = 2 
                    AND glcompm.STATUS = 2
                    AND glacgrpm.TITLE_CD = gltitlm.TITLE_CD
                    AND glledgrm.STATUS = 2
                    AND gllegbal.DEPT_ID IN (
                        SELECT DEPT_ID FROM gldeptm 
                        WHERE STATUS = 2 AND COMP_ID IN (
                            SELECT COMP_ID FROM glcompm
                            WHERE COMP_ID = :COMP_ID OR PARENT_ID = :COMP_ID OR GRP_COMP = :COMP_ID
                        )
                    )
                    AND gllegbal.YR_IND = :YR_IND
                    AND gllegbal.MTH_IND = :MTH_IND
                    AND gltitlm.TITLE_CD LIKE 'TB%'
                    GROUP BY 
                        CASE WHEN gldeptm.COMP_ID = :COMP_ID THEN gldeptm.DEPT_ID  
                             WHEN glcompm.PARENT_ID = :COMP_ID THEN glcompm.COMP_ID
                             WHEN glcompm.GRP_COMP = :COMP_ID THEN glcompm.PARENT_ID  
                             ELSE '' END,
                        glcompm.GRP_COMP,
                        glcompm.PARENT_ID,
                        glcompm.COMP_ID,
                        gldeptm.COMP_ID,
                        gldeptm.DEPT_ID,
                        glledgrm.AC_CD,
                        glledgrm.GL_NM,
                        gldeptm.DEPT_NM,
                        glcompm.COMP_NM
                    ORDER BY 
                        CASE WHEN gldeptm.COMP_ID = :COMP_ID THEN gldeptm.DEPT_ID  
                             WHEN glcompm.PARENT_ID = :COMP_ID THEN glcompm.COMP_ID
                             WHEN glcompm.GRP_COMP = :COMP_ID THEN glcompm.PARENT_ID  
                             ELSE '' END";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        // Add parameters (13 occurrences of COMP_ID)
                        for (int i = 0; i < 13; i++)
                        {
                            cmd.Parameters.Add(new OracleParameter("COMP_ID", COMP_ID));
                        }

                        cmd.Parameters.Add(new OracleParameter("YR_IND", YR_IND));
                        cmd.Parameters.Add(new OracleParameter("MTH_IND", MTH_IND));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new RegionwiseTrialModel
                                {
                                    GL_CD = reader["GL_CD"]?.ToString(),
                                    AC_CD = reader["AC_CD"]?.ToString(),
                                    GL_NM = reader["GL_NM"]?.ToString(),
                                    TitleFlag = reader["TitleFlag"]?.ToString(),
                                    OP_BAL = reader["OP_BAL"] != DBNull.Value ? Convert.ToDecimal(reader["OP_BAL"]) : 0,
                                    DR_AMT = reader["DR_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DR_AMT"]) : 0,
                                    CR_AMT = reader["CR_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CR_AMT"]) : 0,
                                    CL_BAL = reader["CL_BAL"] != DBNull.Value ? Convert.ToDecimal(reader["CL_BAL"]) : 0,
                                    COSTCTR = reader["COSTCTR"]?.ToString(),
                                    Comp_NM = reader["Comp_NM"]?.ToString(),
                                    DEPT_ID = reader["DEPT_ID"]?.ToString()
                                };

                                trialBalanceList.Add(item);
                            }
                        }
                    }
                }
                catch (OracleException ex)
                {
                    System.Diagnostics.Trace.TraceError($"Oracle Error {ex.Number}: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"Error: {ex.Message}");
                    throw;
                }
            }

            return trialBalanceList;
        }
    }
}