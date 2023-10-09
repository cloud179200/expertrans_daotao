using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;

namespace ExpertransDaoTao.ViewModel
{
    public class StudentData
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }

    public class TeacherData
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }

    public class CourseData
    {
        public int CourseId2 { get; set; }
        public string Course2Name { get; set; }
        public int Course2Order { get; set; }
        public List<CourseLevel3> Course3Data { get; set; }
    }
    public class AdminCourseDetail
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseTitle { get; set; }
        public string CourseDescription { get; set; }
        public string Tag { get; set; }
        public string StudentIds { get; set; }
        public string TeacherIds { get; set; }

        public List<CourseData> Data { get; set; }
        public List<StudentData> StudentRecord { get; set; }
        public List<TeacherData> TeacherRecord { get; set; }

    }
}
