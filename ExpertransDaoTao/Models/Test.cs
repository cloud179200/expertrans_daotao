using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Test
    {
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string TestQuestions { get; set; }
        public int? Time { get; set; }
        public int? Status { get; set; }
        public string DocumentsId { get; set; }
        public string CourseIds { get; set; }
        public string TotalPoint { get; set; }
        public string TeacherIds { get; set; }
    }
}
