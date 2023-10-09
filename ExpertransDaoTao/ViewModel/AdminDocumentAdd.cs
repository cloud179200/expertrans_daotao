using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class AdminDocumentAdd
    {
        public int DocId { get; set; }
        public string DocName { get; set; }
        public string Type { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? Status { get; set; }

        public IFormFile File { get; set; }
    }
}
