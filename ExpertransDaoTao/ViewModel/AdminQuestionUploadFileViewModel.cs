using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class AdminQuestionUploadFileViewModel
    {

        public int IdQuestion { get; set; }

        public FileResult File { get; set; }
    }
}
