using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materials")]
    public class MaterialController : ApiController
    {
        private readonly MaterialRepository _materialRepository = new MaterialRepository();
        private readonly MaterialReagionStockRepository _materialStockRepository = new MaterialReagionStockRepository();
        private readonly MaterialStockBalanceRepository _materialStockBalanceRepository = new MaterialStockBalanceRepository();

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetMaterials()
        {
            try
            {
                List<Material> materials = _materialRepository.GetActiveMaterials();
                var response = JObject.Parse(JsonConvert.SerializeObject(new { data = materials }));
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("stocks")]
        public IHttpActionResult GetMaterialStocks()
        {
            try
            {
                List<MaterialReagionStock> stocks = _materialStockRepository.GetMaterialStocks();
                var response = JObject.Parse(JsonConvert.SerializeObject(new { data = stocks }));
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("stock-balances")]
        public IHttpActionResult GetMaterialStockBalances()
        {
            try
            {
                List<MaterialStockBalance> balances = _materialStockBalanceRepository.GetMaterialStockBalances();
                var response = JObject.Parse(JsonConvert.SerializeObject(new { data = balances }));
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
