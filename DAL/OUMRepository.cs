using System.Data.OleDb;
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class OUMRepository
    {
        // Use the OLE DB connection string (more stable)
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public int InsertIntoInformix(List<Employee> data)
        {
            int count = 0;
            try
            {
                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();

                    // Clear existing data
                    using (var cmd = new OleDbCommand("DELETE FROM test_amex2", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert new data
                    foreach (var item in data)
                    {
                        using (var cmd = new OleDbCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = @"INSERT INTO test_amex2 (pdate, o_id, acct_no, cname, bill_amt, tax, tot_amt, authcode, cno) 
                                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                            // OLE DB uses positional parameters
                            cmd.Parameters.Add("@p1", OleDbType.DBTimeStamp).Value = item.auth_date;
                            cmd.Parameters.Add("@p2", OleDbType.Integer).Value = item.order_id;
                            cmd.Parameters.Add("@p3", OleDbType.VarChar).Value = item.acct_number ?? "";
                            cmd.Parameters.Add("@p4", OleDbType.VarChar).Value = item.bank_code ?? "";
                            cmd.Parameters.Add("@p5", OleDbType.Decimal).Value = item.bill_amt;
                            cmd.Parameters.Add("@p6", OleDbType.Decimal).Value = item.tax_amt;
                            cmd.Parameters.Add("@p7", OleDbType.Decimal).Value = item.tot_amt;
                            cmd.Parameters.Add("@p8", OleDbType.VarChar).Value = item.auth_code ?? "";
                            cmd.Parameters.Add("@p9", OleDbType.VarChar).Value = item.card_no ?? "";

                            count += cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InsertIntoInformix: {ex.Message}");
                throw;
            }
            return count;
        }

        public void RefreshCrdTemp()
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    try
                    {
                        // Step 1: Delete existing records
                        cmd.CommandText = @"DELETE FROM test_crdt_tmp";
                        cmd.ExecuteNonQuery();

                        // Step 2: Insert from amex2
                        cmd.CommandText = @"INSERT INTO test_crdt_tmp 
                        SELECT o_id, acct_no, '-', '-', bill_amt, tax, tot_amt, 'S',
                        authcode, pdate, pdate, 'S', '0', cname, 'CRC', '', '', '', '', '', '',
                        cno, 'Bil', acct_no, 'RSK', ''
                        FROM test_amex2";
                        cmd.ExecuteNonQuery();

                        // Step 3: Update null values
                        cmd.CommandText = @"UPDATE test_crdt_tmp 
                        SET updt_flag = NULL, post_flag = NULL, err_flag = NULL, sms_st = NULL";
                        cmd.ExecuteNonQuery();

                        // Step 4: Update payment_type
                        cmd.CommandText = @"UPDATE test_crdt_tmp SET payment_type = 'PIV' WHERE LENGTH(ref_number) > 10";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in RefreshCrdTemp: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public List<CrdTemp> GetCrdTempRecords()
        {
            var records = new List<CrdTemp>();

            try
            {
                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT * FROM test_crdt_tmp", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new CrdTemp
                            {
                                order_id = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader[0]),
                                acct_number = reader.IsDBNull(1) ? "" : reader[1].ToString(),
                                custname = reader.IsDBNull(2) ? "" : reader[2].ToString(),
                                username = reader.IsDBNull(3) ? "" : reader[3].ToString(),
                                bill_amt = reader.IsDBNull(4) ? 0 : Convert.ToDecimal(reader[4]),
                                tax_amt = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader[5]),
                                tot_amt = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader[6]),
                                trstatus = reader.IsDBNull(7) ? "" : reader[7].ToString(),
                                authcode = reader.IsDBNull(8) ? "" : reader[8].ToString(),
                                pmnt_date = reader.IsDBNull(9) ? DateTime.MinValue : Convert.ToDateTime(reader[9]),
                                auth_date = reader.IsDBNull(10) ? DateTime.MinValue : Convert.ToDateTime(reader[10]),
                                cebres = reader.IsDBNull(11) ? "" : reader[11].ToString(),
                                serl_no = reader.IsDBNull(12) ? 0 : Convert.ToInt32(reader[12]),
                                bank_code = reader.IsDBNull(13) ? "" : reader[13].ToString(),
                                bran_code = reader.IsDBNull(14) ? "" : reader[14].ToString(),
                                card_no = reader.FieldCount > 21 && !reader.IsDBNull(21) ? reader[21].ToString() : "",
                                payment_type = reader.FieldCount > 22 && !reader.IsDBNull(22) ? reader[22].ToString() : "",
                                ref_number = reader.FieldCount > 23 && !reader.IsDBNull(23) ? reader[23].ToString() : "",
                                reference_type = reader.FieldCount > 24 && !reader.IsDBNull(24) ? reader[24].ToString() : ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCrdTempRecords: {ex.Message}");
                throw;
            }
            return records;
        }

        public string InsertData()
        {
            try
            {
                using (var connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert into main table
                            using (var command = new OleDbCommand("INSERT INTO test_crdtcdslt SELECT * FROM test_crdt_tmp", connection))
                            {
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();
                            }

                            // Insert into backup
                            using (var command = new OleDbCommand("INSERT INTO appadm1.crdt_tmp_backup SELECT * FROM appadm1.test_crdt_tmp", connection))
                            {
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();
                            }

                            // Delete from temp
                            using (var command = new OleDbCommand("DELETE FROM appadm1.test_crdt_tmp", connection))
                            {
                                command.Transaction = transaction;
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return "Records Successfully Updated";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Debug.WriteLine($"Transaction failed: {ex.Message}");
                            return $"Error: Transaction Failed - {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database connection failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}