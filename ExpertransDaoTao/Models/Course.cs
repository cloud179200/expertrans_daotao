using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Course
    {
        public Course()
        {
            CourseLevel2 = new HashSet<CourseLevel2>();
        }

        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseDescription { get; set; }
        public string Tag { get; set; }
        public string StudentIds { get; set; }
        public string TeacherIds { get; set; }

        public virtual ICollection<CourseLevel2> CourseLevel2 { get; set; }
    }
}
