using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class HomeworkHistory
    {
        public long HistoryId { get; set; }
        public long? HomeworkId { get; set; }
        public long? StudentId { get; set; }
        public string StudentResult { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? Total { get; set; }
        public int? TotalQuestions { get; set; }
        public int? Status { get; set; }
        public double? WritingScore { get; set; }
        public string Marker { get; set; }
        public string DetailWritingMark { get; set; }
        public double? TotalScore { get; set; }
    }
}
