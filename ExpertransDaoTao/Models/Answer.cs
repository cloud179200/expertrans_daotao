using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Answer
    {
        public int AnswerId { get; set; }
        public int? QuestionId { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public int? DocId { get; set; }

        public virtual Document Doc { get; set; }
        public virtual Question Question { get; set; }
    }
}
