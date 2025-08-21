using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using OfficeOpenXml;
using System.IO;
using System.Web;

namespace MISReports_Api.DAL
{
    public class OUMRepository

    // Database connection string from configuration
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixCreditCard"].ConnectionString;
       
        // Method to process uploaded Excel file
        public List<Employee> ProcessExcelFile(HttpPostedFile file)
        {
            var data = new List<Employee>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    // Copy file content
                    file.InputStream.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        for (int row = 1; row <= worksheet.Dimension.End.Row; row++)
                        {
                            // Create Employee object from each row
                            data.Add(new Employee
                            {
                                auth_date = DateTime.Parse(worksheet.Cells[row, 1].Text),
                                order_id = int.Parse(worksheet.Cells[row, 2].Text),
                                acct_number = worksheet.Cells[row, 3].Text,
                                bank_code = worksheet.Cells[row, 4].Text,
                                bill_amt = Decimal.Parse(worksheet.Cells[row, 5].Text),
                                tax_amt = Decimal.Parse(worksheet.Cells[row, 6].Text),
                                tot_amt = Decimal.Parse(worksheet.Cells[row, 7].Text),
                                auth_code = worksheet.Cells[row, 8].Text,
                                card_no = worksheet.Cells[row, 9].Text,
                            });
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing Excel file: {ex.Message}", ex);
            }
        }
        // Method to insert processed data into Informix database
        public int InsertIntoInformix(List<Employee> data)
        {
            int count = 0;
            try
            {
                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    // clear the temporary table

                    using (var cmd = new OleDbCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = @"delete from test_amex2 ";
                        cmd.ExecuteNonQuery();
                    }
                    // Insert each employee record
                    foreach (var item in data)
                    {
                        using (var cmd = new OleDbCommand())
                        {
                            cmd.Connection = conn;
                            // Parameterized query to prevent SQL injection
                            cmd.CommandText = @"INSERT INTO test_amex2 (pdate, o_id, acct_no, cname, bill_amt, tax, tot_amt, authcode, cno) 
                                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                            cmd.Parameters.AddWithValue("pdate", item.auth_date);
                            cmd.Parameters.AddWithValue("o_id", item.order_id);
                            cmd.Parameters.AddWithValue("acct_no", item.acct_number);
                            cmd.Parameters.AddWithValue("cname", item.bank_code);
                            cmd.Parameters.AddWithValue("bill_amt", item.bill_amt);
                            cmd.Parameters.AddWithValue("tax", item.tax_amt);
                            cmd.Parameters.AddWithValue("tot_amt", item.tot_amt);
                            cmd.Parameters.AddWithValue("authcode", item.auth_code);
                            cmd.Parameters.AddWithValue("cno", item.card_no);

                            count += cmd.ExecuteNonQuery();
                        }
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting into Informix: {ex.Message}", ex);
            }
        }
        //  refresh the temporary credit table
        public void RefreshCrdTemp()
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    //  Clear existing temporary data
                    cmd.CommandText = @"DELETE FROM test_crdt_tmp";
                    cmd.ExecuteNonQuery();
                    // Populate with transformed data from test_amex2
                    cmd.CommandText = @"INSERT INTO test_crdt_tmp 
                    SELECT o_id, acct_no, '-', '-', bill_amt, tax, tot_amt, 'S',
                    authcode, pdate, pdate, 'S', '0', cname, 'CRC', '', '', '', '', '', '',
                    cno, 'Bil', acct_no, 'RSK', ''
                    FROM test_amex2";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"UPDATE test_crdt_tmp 
                    SET updt_flag = NULL, post_flag = NULL, err_flag = NULL, sms_st = NULL";
                    cmd.ExecuteNonQuery();
                    // Update payment type based on reference number length
                    cmd.CommandText = @"UPDATE test_crdt_tmp SET payment_type = 'PIV' WHERE LENGTH(ref_number) > 10";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // Method to retrieve records from temporary table
        public List<CrdTemp> GetCrdTempRecords()
        {
            List<CrdTemp> records = new List<CrdTemp>();

            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OleDbCommand("SELECT * FROM test_crdt_tmp", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Map database columns to CrdTemp object properties
                        records.Add(new CrdTemp
                        {
                            order_id = Convert.ToInt32(reader[0]),
                            acct_number = reader[1]?.ToString().Trim(),
                            custname = reader[2]?.ToString().Trim(),
                            username = reader[3]?.ToString().Trim(),
                            bill_amt = Convert.ToDecimal(reader[4]),
                            tax_amt = Convert.ToDecimal(reader[5]),
                            tot_amt = Convert.ToDecimal(reader[6]),
                            trstatus = reader[7]?.ToString().Trim(),
                            authcode = reader[8]?.ToString().Trim(),
                            pmnt_date = Convert.ToDateTime(reader[9]),
                            auth_date = Convert.ToDateTime(reader[10]),
                            cebres = reader[11]?.ToString().Trim(),
                            serl_no = Convert.ToInt32(reader[12]),
                            bank_code = reader[13]?.ToString().Trim(),
                            bran_code = reader[14]?.ToString().Trim(),
                            card_no = reader[21]?.ToString().Trim(),
                            payment_type = reader[22]?.ToString().Trim(),
                            ref_number = reader[23]?.ToString().Trim(),
                            reference_type = reader[24]?.ToString().Trim()
                        });
                    }
                }
            }
            return records;
        }
        // Method to move data from temporary to permanent storage
        public void InsertData()
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {

                        // Move data to main table
                        using (var cmd = new OleDbCommand("INSERT INTO test_crdtcdslt SELECT * FROM test_crdt_tmp;", conn, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        // Backup temporary data
                        using (var cmd = new OleDbCommand("INSERT INTO appadm1.crdt_tmp_backup SELECT * FROM appadm1.test_crdt_tmp;", conn, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        // Clear temporary table
                        using (var cmd = new OleDbCommand("DELETE FROM appadm1.test_crdt_tmp;", conn, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        // Commit transaction if all operations succeed
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}