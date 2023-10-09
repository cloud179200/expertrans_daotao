using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;

namespace ExpertransDaoTao.ViewModel
{
    public class StudentHistoryViewModel
    {
        public IEnumerable<Students> Students { get; set; }
        public IEnumerable<Teachers> Teachers { get; set; }
        public IEnumerable<Users> Users { get; set; }

        public IEnumerable<Test> Tests { get; set; }
        public IEnumerable<Course> Courses { get; set; }
        public IEnumerable<CourseLevel2> CourseLevel2s { get; set; }
        public IEnumerable<CourseLevel3> CourseLevel3s { get; set; }
        public IEnumerable<TestHistory> TestHistorys { get; set; }
        public IEnumerable<Question> Questions { get; set; }
        public IEnumerable<Answer> Answers { get; set; }

    }
    public class StudentHomeworkHistoryViewModel
    {
        public IEnumerable<Students> Students { get; set; }
        public IEnumerable<Teachers> Teachers { get; set; }
        public IEnumerable<Users> Users { get; set; }
        public IEnumerable<Homework> Homeworks { get; set; }
        public IEnumerable<Course> Courses { get; set; }
        public IEnumerable<CourseLevel2> CourseLevel2s { get; set; }
        public IEnumerable<CourseLevel3> CourseLevel3s { get; set; }

        public IEnumerable<HomeworkHistory> HomeworkHistorys { get; set; }
        public IEnumerable<Question> Questions { get; set; }
        public IEnumerable<Answer> Answers { get; set; }

    }
}
