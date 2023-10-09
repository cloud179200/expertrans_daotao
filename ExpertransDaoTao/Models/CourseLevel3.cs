using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class CourseLevel3
    {
        public int CourseId3 { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CourseId2 { get; set; }
        public int? Order { get; set; }
        public string DocumentsId { get; set; }
        public string Questions { get; set; }
        public string Content { get; set; }
        public string DocIdsContent { get; set; }
        public string HomeworkIds { get; set; }

        public virtual CourseLevel2 CourseId2Navigation { get; set; }
    }
}
