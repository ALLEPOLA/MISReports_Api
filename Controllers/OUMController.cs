using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/oum")]
    public class OUMController : ApiController
    {
        private readonly OUMRepository _oumRepository = new OUMRepository();

        [HttpPost]
        [Route("upload")]
        public IHttpActionResult UploadExcel()
        {
            try
            {
                var httpRequest = System.Web.HttpContext.Current.Request;
                // Check if any files were uploaded
                if (httpRequest.Files.Count == 0)
                {
                    return Ok(JObject.FromObject(new OUMResponse
                    {
                        Data = null,
                        ErrorMessage = "Please select a file"
                    }));
                }
                // Check if any files were uploaded
                var file = httpRequest.Files[0];
                // Process the Excel file and extract data
                var employees = _oumRepository.ProcessExcelFile(file);
                // Insert data into Informix database
                var insertedRows = _oumRepository.InsertIntoInformix(employees);
                // Refresh the temporary credit table with transformed data
                _oumRepository.RefreshCrdTemp();
                // Get the updated records from temporary table
                var records = _oumRepository.GetCrdTempRecords();

                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = new
                    {
                        insertedCount = insertedRows,
                        records = records
                    },
                    ErrorMessage = insertedRows == 1 ?
                        $"Successfully inserted {insertedRows} record" :
                        $"Successfully inserted {insertedRows} records"
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = null,
                    ErrorMessage = "Error processing file",
                    ErrorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("records")]
        public IHttpActionResult GetRecords()
        {
            try
            {
                // Retrieve all records from temporary table
                var records = _oumRepository.GetCrdTempRecords();

                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = records,
                    ErrorMessage = null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = null,
                    ErrorMessage = "Cannot get records data",
                    ErrorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("approve")]
        public IHttpActionResult ApproveOUM()
        {
            try
            {
                // Move data from temporary to permanent storage
                _oumRepository.InsertData();
                // Get remaining records (should be empty after approval)
                var records = _oumRepository.GetCrdTempRecords();

                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = records,
                    ErrorMessage = "Records Successfully Updated"
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new OUMResponse
                {
                    Data = null,
                    ErrorMessage = "Transaction Failed",
                    ErrorDetails = ex.Message
                }));
            }
        }
    }
}