using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Question
    {
        public Question()
        {
            Answer = new HashSet<Answer>();
        }

        public int QuestionId { get; set; }
        public string Content { get; set; }
        public string DocIdsContent { get; set; }
        public string TrueAnswers { get; set; }
        public string Type { get; set; }
        public string Tag { get; set; }
        public int? TeacherId { get; set; }

        public virtual ICollection<Answer> Answer { get; set; }
    }
}
