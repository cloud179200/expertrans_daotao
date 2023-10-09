using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class HistoryDetailViewModel
    {
        public long HistoryId { get; set; }
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string StudentName { get; set; }
        public string StudentResult { get; set; }
        public double? Total { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
