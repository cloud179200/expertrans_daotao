using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.Models
{
    public class JsonResultModel
    {
        public List<QuestionResult> Result { get; set; }
        public int IdTest { get; set; }
        public int IdHistory { get; set; }
        public string HTMLHistory { get; set; }
        public int Part { get; set; }
    }
    public class JsonResultHomeworkModel
    {
        public List<QuestionResult> Result { get; set; }
        public int IdHomework { get; set; }
        public int IdHistory { get; set; }
        public string HTMLHistory { get; set; }
    }
    public class QuestionResult
    {
        public int IdQuestion { get; set; }
        public string AnswerJsonString { set; get; }
    }
    public class QuestionResultType1
    {
        public int IdAnswer { get; set; }
        public bool Answer { get; set; }
    }
    public class QuestionResultType2
    {
        public int Place { get; set; }
        public RightLeftObject Answer { get; set; }
    }
    public class RightLeftObject
    {
        public int Right { get; set; }
        public int Left { get; set; }
    }
    public class QuestionResultType3
    {
        public int IdAnswer { get; set; }
        public string Answer { get; set; }
    }
    public class QuestionResultType6
    {
        public int IdAnswer { get; set; }
        public bool Answer { get; set; }
    }
}
