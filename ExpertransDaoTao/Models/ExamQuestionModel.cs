using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.Models
{
    public class ExamQuestionModel
    {
        public string partName { get; set; }
        public int totalTime { get; set; }
        public List<QuestionGroupForExam> questions { get; set; }

    }
    public class HomeworkQuestionModel
    {
        public List<QuestionGroupForExam> questions { get; set; }
    }
    public class QuestionGroupForExam
    {
        public int listenAgain { get; set; }
        public string content { get; set; }
        public string docid { get; set; }
        public List<int> questionIds { get; set; }
        public List<QuestionDataObj> questionsData { get; set; }
    }
    public class QuestionDataObj
    {
        public int id { get; set; }
        public float point { get; set; }
        public string type { get; set; }
    }
}
