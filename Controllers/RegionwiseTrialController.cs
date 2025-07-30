using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/regionwisetrial")]
    public class RegionwiseTrialController : ApiController
    {
        private readonly RegionwiseTrialRepository _repository = new RegionwiseTrialRepository();

        [HttpGet]
        [Route("balance")]
        public IHttpActionResult GetRegionwiseTrialBalance([FromUri] string COMP_ID, [FromUri] int YR_IND, [FromUri] int MTH_IND)
        {
            if (string.IsNullOrWhiteSpace(COMP_ID))
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "COMP_ID parameter is required."
                }));
            }

            if (YR_IND <= 0)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Valid YR_IND parameter is required."
                }));
            }

            if (MTH_IND <= 0 || MTH_IND > 12)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Valid MTH_IND parameter (1-12) is required."
                }));
            }

            try
            {
                var trialData = _repository.GetRegionwiseTrialData(COMP_ID, YR_IND, MTH_IND);

                return Ok(JObject.FromObject(new
                {
                    data = trialData,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                // Log the error details for debugging
                System.Diagnostics.Trace.TraceError($"Error in GetRegionwiseTrialBalance: {ex}");

                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get regionwise trial balance data.",
                    errorDetails = ex.Message
                }));
            }
        }





    }
}