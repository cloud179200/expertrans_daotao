using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class CourseLevel2
    {
        public CourseLevel2()
        {
            CourseLevel3 = new HashSet<CourseLevel3>();
        }

        public int CourseId2 { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CourseId { get; set; }
        public int? Order { get; set; }

        public virtual Course Course { get; set; }
        public virtual ICollection<CourseLevel3> CourseLevel3 { get; set; }
    }
}
