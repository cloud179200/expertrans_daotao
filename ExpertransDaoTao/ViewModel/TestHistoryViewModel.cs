using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class TestHistoryViewModel
    {
        public long? StudentId { get; set; }
        public string Name { get; set; }
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string TestQuestions { get; set; }
        public int? Time { get; set; }
        public long HistoryId { get; set; }
        public double? Total { get; set; }
        public string StudentResult { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double TotalStudentTime { get; set; }
    }
}
