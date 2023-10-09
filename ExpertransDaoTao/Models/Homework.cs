using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Homework
    {
        public long HomeworkId { get; set; }
        public string HomeworkName { get; set; }
        public string DocumentIds { get; set; }
        public string HomeworkQuestions { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public string TotalPoint { get; set; }
    }
}
