using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Document
    {
        public Document()
        {
            Answer = new HashSet<Answer>();
        }

        public int DocId { get; set; }
        public string DocName { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? Confirm { get; set; }
        public string Trace { get; set; }

        public virtual ICollection<Answer> Answer { get; set; }
    }
}
