using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class DocumentData
    {
        public int DocId { get; set; }
        public string DocName { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class HomeworkData
    {
        public long HomeworkId { get; set; }
        public string HomeworkName { get; set; }
    }
    public class Course3DetailViewModel
    {
        public int CourseId3 { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? CourseId2 { get; set; }
        public string Course2Name { get; set; }
        public int? Order { get; set; }
        public string DocumentsId { get; set; }
        public string Questions { get; set; }
        public string Content { get; set; }
        public string DocIdsContent { get; set; }
        public string HomeworkIds { get; set; }
        public List<DocumentData> Documents { get; set; }
        public List<HomeworkData> Homeworks { get; set; }
    }
}
