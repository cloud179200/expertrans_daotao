using ExpertransDaoTao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class StudentExamViewModel
    {
        public IEnumerable<Test> Tests { get; set; }
        public IEnumerable<Question> Questions { get; set; }
        public IEnumerable<Answer> Answers { get; set; }
        public IEnumerable<Course> Courses { get; set; }

        public int historyId { get; set; }
        public List<ExamQuestionModel> examQuestions { get; set; }
    }
}
