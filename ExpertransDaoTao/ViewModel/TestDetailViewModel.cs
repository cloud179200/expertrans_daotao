using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class Course1Data
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
    }

    public class Teacher1Data
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
    }
    public class TestDetailViewModel
    {
        public int TestId { get; set; }
        public string TestName { get; set; }
        public string TestQuestions { get; set; }
        public int? Time { get; set; }
        public int? Status { get; set; }
        public string DocumentsId { get; set; }
        public string CourseIds { get; set; }
        public string TeacherIds { get; set; }
        public List<Select2Data> SelectedCourses { get; set; }
        public List<Course1Data> Courses { get; set; }
        public List<Teacher1Data> Teachers { get; set; }
        public List<Select2Data> SelectedTeachers { get; set; }
    }
}
