using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;

namespace ExpertransDaoTao.ViewModel
{
    public class CourseViewModel
    {
        public IEnumerable<Course> Courses { get; set; }
        public IEnumerable<CourseLevel2> CoursesLevel2 { get; set; }
        public IEnumerable<CourseLevel3> CoursesLevel3 { get; set; }
        public IEnumerable<Question> Questions { get; set; }
        public IEnumerable<Answer> Answers { get; set; }
        public IEnumerable<Document> Documents { get; set; }
        public IEnumerable<Test> Tests { get; set; }
        public IEnumerable<Homework> Homeworks { get; set; }
        public IEnumerable<HomeworkHistory> HomeworkHistorys { get; set; }

        public int historyId { get; set; }

    }
}
