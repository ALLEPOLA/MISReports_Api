using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/oum")]
    public class OUMController : ApiController
    {
        private readonly OUMRepository _repository = new OUMRepository();

        [HttpPost]
        [Route("upload")]
        public IHttpActionResult OUMEntry()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                if (httpRequest.Files.Count == 0)
                {
                    return Ok(CreateResponse(null, "Please select a file"));
                }

                var file = httpRequest.Files[0];
                if (file.ContentLength == 0)
                {
                    return Ok(CreateResponse(null, "File is empty"));
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    return Ok(CreateResponse(null, "Please upload a valid Excel file (.xlsx or .xls)"));
                }

                // Read Excel data
                var data = new List<Employee>();
                using (var stream = new MemoryStream())
                {
                    file.InputStream.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            return Ok(CreateResponse(null, "Excel file contains no worksheets"));
                        }

                        var worksheet = package.Workbook.Worksheets[1];
                        if (worksheet.Dimension == null)
                        {
                            return Ok(CreateResponse(null, "Excel worksheet is empty"));
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Assuming row 1 is header
                        {
                            // Check if row has data
                            if (worksheet.Cells[row, 1].Value == null) break;

                            try
                            {
                                data.Add(new Employee
                                {
                                    auth_date = DateTime.Parse(worksheet.Cells[row, 1].Text),
                                    order_id = int.Parse(worksheet.Cells[row, 2].Text),
                                    acct_number = worksheet.Cells[row, 3].Text ?? "",
                                    bank_code = worksheet.Cells[row, 4].Text ?? "",
                                    bill_amt = decimal.Parse(worksheet.Cells[row, 5].Text),
                                    tax_amt = decimal.Parse(worksheet.Cells[row, 6].Text),
                                    tot_amt = decimal.Parse(worksheet.Cells[row, 7].Text),
                                    auth_code = worksheet.Cells[row, 8].Text ?? "",
                                    card_no = worksheet.Cells[row, 9].Text ?? "",
                                });
                            }
                            catch (Exception rowEx)
                            {
                                return Ok(CreateResponse(null, $"Error parsing row {row}: {rowEx.Message}"));
                            }
                        }
                    }
                }

                if (data.Count == 0)
                {
                    return Ok(CreateResponse(null, "No valid data found in Excel file"));
                }

                // Insert into database
                var insertedRows = _repository.InsertIntoInformix(data);
                _repository.RefreshCrdTemp();
                var records = _repository.GetCrdTempRecords();

                var message = insertedRows == 1 ?
                    $"Successfully inserted {insertedRows} record" :
                    $"Successfully inserted {insertedRows} records";

                return Ok(CreateResponse(new { records, insertedRows, totalDataRows = data.Count }, message));
            }
            catch (Exception ex)
            {
                return Ok(CreateResponse(null, $"Error: {ex.Message}", ex.StackTrace));
            }
        }

        [HttpGet]
        [Route("records")]
        public IHttpActionResult GetRecords()
        {
            try
            {
                var records = _repository.GetCrdTempRecords();
                var message = records.Count > 0 ?
                    $"{records.Count} OUM Record(s) to be Approved" :
                    "No Records Found";

                return Ok(CreateResponse(new { records, count = records.Count }, message));
            }
            catch (Exception ex)
            {
                return Ok(CreateResponse(null, "Error retrieving records", ex.Message));
            }
        }

        [HttpPost]
        [Route("approve")]
        public IHttpActionResult OUMApprove()
        {
            try
            {
                var result = _repository.InsertData();
                var records = _repository.GetCrdTempRecords();

                return Ok(CreateResponse(new { records, result, count = records.Count }, result));
            }
            catch (Exception ex)
            {
                return Ok(CreateResponse(null, "Error approving records", ex.Message));
            }
        }

        private JObject CreateResponse(object data, string errorMessage = null, string errorDetails = null)
        {
            var response = new OUMResponse
            {
                data = data,
                errorMessage = errorMessage,
                errorDetails = errorDetails
            };

            return JObject.Parse(JsonConvert.SerializeObject(response));
        }
    }
}