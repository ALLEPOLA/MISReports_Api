namespace MISReports_Api.Models
{
    public class RegionwiseTrialModel
    {
        public string GL_CD { get; set; }
        public string AC_CD { get; set; }
        public string GL_NM { get; set; }
        public string TitleFlag { get; set; }
        public decimal OP_BAL { get; set; }
        public decimal DR_AMT { get; set; }
        public decimal CR_AMT { get; set; }
        public decimal CL_BAL { get; set; }
        public string COSTCTR { get; set; }
        public string Comp_NM { get; set; }
        public string DEPT_ID { get; set; }
    }

    public class RegionwiseTrialRequest
    {
        public string COMP_ID { get; set; }
        public int YR_IND { get; set; }
        public int MTH_IND { get; set; }
    }
}