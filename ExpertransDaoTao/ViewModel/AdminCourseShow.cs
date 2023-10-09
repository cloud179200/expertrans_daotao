using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;

namespace ExpertransDaoTao.ViewModel
{
    public class AdminCourseShow
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseTitle { get; set; }
        public string CourseDescription { get; set; }

        public List<CourseLevel2> Course2 { get; set; }
    }
}
