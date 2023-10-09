using ExpertransDaoTao.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;
using System.Security.Claims;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace ExpertransDaoTao.Controllers
{
    public class StudentController : Controller
    {
        private readonly expertrans_educateContext _db;
        private readonly expertrans_liveContext _db_live;


        public StudentController(expertrans_educateContext db, expertrans_liveContext db_live)
        {
            _db = db;
            _db_live = db_live;
        }

        [Authorize(Roles = "Student")]
        public IActionResult Index()
        {
            IEnumerable<Students> studentInfo = _db_live.Students.Where(st => st.UserName == User.FindFirst(ClaimTypes.NameIdentifier).Value).ToList();
            return View(studentInfo);
        }
        [Authorize(Roles = "Student")]
        [Route("[controller]/[action]/{courseId}/{courseIdLvl_2}")]
        [Route("[controller]/[action]/{courseId}/{courseIdLvl_2}/{courseIdLvl_3}")]
        [Route("[controller]/[action]/{courseId}/{courseIdLvl_2}/{courseIdLvl_3}/{idPage}")]

        public IActionResult Course(int courseId, int courseIdLvl_2, int courseIdLvl_3, int idPage, int idHomework)
        {
            Course course = _db.Course.SingleOrDefault(c => c.CourseId == courseId);
            if (course != null)
            {
                List<string> listStudent = course.StudentIds.Split(",").ToList();
                if (listStudent.IndexOf(User.FindFirst(ClaimTypes.Authentication).Value) == -1)
                {
                    Redirect("/");
                }
            }

            var tables = new CourseViewModel
            {
                Courses = _db.Course.Where(crs => crs.CourseId == courseId).ToList(),
                CoursesLevel2 = _db.CourseLevel2.Where(crslvl2 => crslvl2.CourseId == courseId).ToList(),
                CoursesLevel3 = _db.CourseLevel3.Where(crslvl3 => crslvl3.CourseId2 == courseIdLvl_2).ToList(),
                Questions = _db.Question.ToList(),
                Documents = _db.Document.ToList(),
                Answers = _db.Answer.ToList(),
                Homeworks = _db.Homework.ToList(),
                HomeworkHistorys = _db.HomeworkHistory.ToList()
            };
            ViewData["Id"] = courseId.ToString();
            ViewData["IdLvl2"] = courseIdLvl_2.ToString();
            if (courseIdLvl_3 != 0)
            {
                ViewData["IdLvl3"] = courseIdLvl_3.ToString();
                ViewData["IdPage"] = idPage.ToString();
                if (idHomework != 0)
                {
                    CourseLevel3 courseLvl3 = _db.CourseLevel3.SingleOrDefault(clvl3 => clvl3.CourseId3 == courseIdLvl_3);
                    if (courseLvl3 != null)
                    {
                        if (!string.IsNullOrEmpty(courseLvl3.HomeworkIds))
                        {
                            var listHomeworkId = courseLvl3.HomeworkIds.Split(",").ToList();
                            if (listHomeworkId.IndexOf(idHomework.ToString()) != -1)
                            {
                                HomeworkHistory historyCheck = _db.HomeworkHistory.SingleOrDefault(h => h.StudentId == int.Parse(User.FindFirst(ClaimTypes.Authentication).Value) && h.HomeworkId == idHomework);
                                Homework homework = _db.Homework.SingleOrDefault(hw => hw.HomeworkId == idHomework);
                                bool isHaveOldQuestion = true;
                                bool isOldQuestion = true;
                                List<int> listQuestionNow = new List<int>();
                                List<HomeworkQuestionModel> homeworkQuestions = JsonConvert.DeserializeObject<List<HomeworkQuestionModel>>(homework.HomeworkQuestions);
                                foreach (HomeworkQuestionModel eqm in homeworkQuestions)
                                {
                                    foreach (QuestionGroupForExam qg in eqm.questions)
                                    {
                                        listQuestionNow.AddRange(qg.questionIds);
                                    }
                                }
                                if (historyCheck != null)
                                {
                                    JsonResultHomeworkModel jhm = JsonConvert.DeserializeObject<List<JsonResultHomeworkModel>>(historyCheck.StudentResult)[0];
                                    if (jhm.Result != null)
                                    {
                                        foreach (QuestionResult qrs in jhm.Result)
                                        {
                                            Question questionCheck = _db.Question.SingleOrDefault(q => q.QuestionId == qrs.IdQuestion);
                                            if (questionCheck == null)
                                            {
                                                isHaveOldQuestion = false;
                                                break;
                                            }
                                            if (listQuestionNow.IndexOf(qrs.IdQuestion) == -1)
                                            {
                                                isOldQuestion = false;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (historyCheck != null && isHaveOldQuestion && isOldQuestion && historyCheck.StudentResult != null)
                                {
                                    if (homeworkQuestions.Count > 0)
                                    {
                                        double totalScore = 0;
                                        List<object> DetailWritingMark = new List<object>();
                                        foreach (HomeworkQuestionModel eqm in homeworkQuestions)
                                        {
                                            foreach (QuestionGroupForExam qg in eqm.questions)
                                            {
                                                foreach (QuestionDataObj qdobj in qg.questionsData)
                                                {
                                                    totalScore += qdobj.point;
                                                    if (qdobj.type == "4" || qdobj.type == "5")
                                                    {
                                                        DetailWritingMark.Add(new { QuestionId = qdobj.id, mark = 0.0, maxScore = qdobj.point });
                                                    }
                                                }
                                            }
                                        }
                                        historyCheck.TotalScore = totalScore;
                                        historyCheck.DetailWritingMark = JsonConvert.SerializeObject(DetailWritingMark);
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            return NotFound(ex.Message);
                                        }
                                        PostHomework(JsonConvert.DeserializeObject<List<JsonResultHomeworkModel>>(historyCheck.StudentResult)[0]);
                                        tables.historyId = (int)historyCheck.HistoryId;
                                        ViewData["IdHomework"] = idHomework.ToString();
                                    }
                                    else
                                    {
                                        ViewData["Error"] = "Bài tập này không có câu hỏi nào!";
                                    }
                                }
                                else
                                {
                                    if (historyCheck != null)
                                    {
                                        _db.HomeworkHistory.RemoveRange(_db.HomeworkHistory.Where(h => h.StudentId == int.Parse(User.FindFirst(ClaimTypes.Authentication).Value) && h.HomeworkId == idHomework));
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            return NotFound(ex.Message);
                                        }
                                    }
                                    //getTotal questions
                                    if (homeworkQuestions.Count > 0)
                                    {
                                        int totalQuestion = 0;
                                        double totalScore = 0;
                                        List<object> DetailWritingMark = new List<object>();
                                        foreach (HomeworkQuestionModel eqm in homeworkQuestions)
                                        {
                                            foreach (QuestionGroupForExam qg in eqm.questions)
                                            {
                                                totalQuestion += qg.questionIds.Count();
                                                foreach (QuestionDataObj qdobj in qg.questionsData)
                                                {
                                                    totalScore += qdobj.point;
                                                    if (qdobj.type == "4" || qdobj.type == "5")
                                                    {
                                                        DetailWritingMark.Add(new { QuestionId = qdobj.id, mark = 0.0, maxScore = qdobj.point });
                                                    }
                                                }
                                            }
                                        }
                                        HomeworkHistory newHistory = new HomeworkHistory
                                        {
                                            StartDate = DateTime.Now,
                                            HomeworkId = idHomework,
                                            StudentId = int.Parse(User.FindFirst(ClaimTypes.Authentication).Value),
                                            TotalQuestions = totalQuestion,
                                            TotalScore = totalScore,
                                            DetailWritingMark = JsonConvert.SerializeObject(DetailWritingMark)
                                        };
                                        _db.HomeworkHistory.Add(newHistory);
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            return NotFound(ex.Message);
                                        }
                                        tables.historyId = (int)newHistory.HistoryId;
                                        ViewData["IdHomework"] = idHomework.ToString();
                                    }
                                    else
                                    {
                                        ViewData["Error"] = "Bài tập này không có câu hỏi nào!";
                                    }
                                }
                            }
                            else
                            {
                                ViewData["Error"] = "Bài tập bạn chọn không thuộc lớp này!";
                            }
                        }
                        else
                        {
                            ViewData["Error"] = "Không có bài tập trong lớp này!";
                        }

                    }

                }
            }
            return View(tables);
        }
        [Authorize(Roles = "Student")]
        [HttpGet]
        public IActionResult GetStudentResultHomework(int idHistory)
        {
            HomeworkHistory history = _db.HomeworkHistory.SingleOrDefault(h => h.HistoryId == idHistory);
            if (history != null)
            {
                return Json(history.StudentResult);
            }
            return NotFound();
        }
        [Authorize(Roles = "Student")]
        [HttpGet]
        public IActionResult GetTrueAnswersPracticeQuestion(int IdCourseLevel3)
        {
            if (IdCourseLevel3 == 0)
            {
                return NotFound();
            }
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            //!!! Sửa chỗ này
            CourseLevel3 courseLvl3 = _db.CourseLevel3.SingleOrDefault(crs => crs.CourseId3 == IdCourseLevel3);
            if (courseLvl3 != null && !string.IsNullOrEmpty(courseLvl3.Questions))
            {
                if (!string.IsNullOrEmpty(courseLvl3.Questions) && courseLvl3.Questions.Split(",").ToList().Count > 0)
                {
                    List<List<int>> listIdQuestion = JsonConvert.DeserializeObject<List<List<int>>>(courseLvl3.Questions);
                    var query = _db.Question.ToList();
                    List<object> questionsAndTrueAnswers = new List<object>();
                    foreach (Question ques in query)
                    {
                        int indexQuestionId = -1;
                        foreach (List<int> listId in listIdQuestion)
                        {
                            if (listId.IndexOf(ques.QuestionId) != -1)
                            {
                                indexQuestionId = listId.IndexOf(ques.QuestionId);
                                break;
                            }
                        };
                        if (indexQuestionId != -1)
                        {
                            if (!string.IsNullOrEmpty(ques.TrueAnswers) && ques.TrueAnswers.Split(",").ToList().Count > 0)
                            {
                                if (ques.Type == "1" || ques.Type == "6")
                                {
                                    var trueAnswersToDisplay = ques.TrueAnswers.Split(",").ToList();
                                    questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = trueAnswersToDisplay });
                                }
                                else if (ques.Type == "2")
                                {
                                    var trueAnswersToDisplay = JsonConvert.DeserializeObject<List<List<int>>>(ques.TrueAnswers);
                                    questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = trueAnswersToDisplay });
                                }
                                else if (ques.Type == "3")
                                {
                                    var listTrueAnswers = ques.TrueAnswers.Split(",").ToList();
                                    List<object> trueAnswerToDisplay = new List<object>();
                                    foreach (string idTrueAnswer in listTrueAnswers)
                                    {
                                        Answer trueAnswer = _db.Answer.SingleOrDefault(s => s.AnswerId.ToString() == idTrueAnswer);
                                        trueAnswerToDisplay.Add(new { trueAnswer.AnswerId, trueAnswer.Content });
                                    }
                                    questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = trueAnswerToDisplay });
                                }
                                else if (ques.Type == "4")
                                {
                                    var trueAnswersToDisplay = ques.TrueAnswers;
                                    questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = trueAnswersToDisplay });
                                }
                                else if (ques.Type == "5")
                                {
                                    var trueAnswersToDisplay = ques.TrueAnswers;
                                    questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = trueAnswersToDisplay });
                                }

                            }
                            else
                            {
                                questionsAndTrueAnswers.Add(new { ques.QuestionId, ques.Type, TrueAnswers = new List<string>() });
                            }
                        }
                    }
                    return Json(JsonConvert.SerializeObject(questionsAndTrueAnswers, settings));
                }
            }
            return NotFound();

        }
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult PostExam(JsonResultModel rs)
        {
            if (rs is null || rs.HTMLHistory is null || rs.IdHistory == 0 || rs.IdTest == 0 || rs.Result is null)
            {
                return NotFound();
            }
            //Kiểm tra xem có phần kết quả trong lịch sử liền kề của bài thi này không
            var history = _db.TestHistory.SingleOrDefault(h => h.HistoryId == rs.IdHistory);
            var test = _db.Test.SingleOrDefault(t => t.TestId == rs.IdTest);
            var examQuestions = JsonConvert.DeserializeObject<List<ExamQuestionModel>>(test.TestQuestions);
            if (string.IsNullOrEmpty(history.StudentResult) && rs.Part == 0)
            {
                List<JsonResultModel> listResult = new List<JsonResultModel>();
                listResult.Add(rs);
                string newResult = JsonConvert.SerializeObject(listResult);
                history.Total = 0;
                foreach (JsonResultModel jrs in listResult)
                {
                    foreach (QuestionResult qrs in jrs.Result)
                    {
                        Question questionToCheck = _db.Question.SingleOrDefault(q => q.QuestionId == qrs.IdQuestion);
                        if (questionToCheck != null)
                        {
                            if (!string.IsNullOrEmpty(qrs.AnswerJsonString))
                            {
                                if (questionToCheck.Type == "1")
                                {
                                    List<QuestionResultType1> answers = JsonConvert.DeserializeObject<List<QuestionResultType1>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType1 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "2")
                                {
                                    List<QuestionResultType2> answers = JsonConvert.DeserializeObject<List<QuestionResultType2>>(qrs.AnswerJsonString);
                                    List<List<int>> rightAnswers = JsonConvert.DeserializeObject<List<List<int>>>(questionToCheck.TrueAnswers);

                                    bool questionCheck = false;

                                    int resultCheck = 0;
                                    foreach (QuestionResultType2 qrsType2 in answers)
                                    {
                                        foreach (List<int> trueAnswers in rightAnswers)
                                        {
                                            if (trueAnswers[0] == qrsType2.Answer.Left && trueAnswers[1] == qrsType2.Answer.Right)
                                            {
                                                resultCheck += 1;
                                                break;
                                            }
                                        }
                                    }
                                    if (resultCheck == rightAnswers.Count())
                                    {
                                        questionCheck = true;
                                    }
                                    if (questionCheck == true)
                                    {

                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "3")
                                {
                                    List<QuestionResultType3> answers = JsonConvert.DeserializeObject<List<QuestionResultType3>>(qrs.AnswerJsonString);
                                    bool questionCheck = true;
                                    foreach (QuestionResultType3 qrsType3 in answers)
                                    {
                                        Answer answer = _db.Answer.SingleOrDefault(a => a.AnswerId == qrsType3.IdAnswer);
                                        if (answer.Content.ToLower() != qrsType3.Answer)
                                        {
                                            questionCheck = false;
                                            break;
                                        }
                                    }

                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "4")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "5")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "6")
                                {
                                    List<QuestionResultType6> answers = JsonConvert.DeserializeObject<List<QuestionResultType6>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType6 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                            }
                        }
                    }
                }
                history.StudentResult = newResult;
                history.EndDate = DateTime.Now;
                if (history.Status != 1 && rs.Part == (examQuestions.Count() - 1))
                {
                    history.Status = 2;
                }
                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                if (examQuestions.ElementAtOrDefault(rs.Part + 1) != null)
                {
                    return Json(JsonConvert.SerializeObject(new { success = 1, next = rs.Part + 1 }));
                }
                return Json(JsonConvert.SerializeObject(new { success = 1 }));
            }
            else if (!string.IsNullOrEmpty(history.StudentResult) && rs.Part != 0)
            {
                var resultExamForCheck = JsonConvert.DeserializeObject<List<JsonResultModel>>(history.StudentResult);
                if (resultExamForCheck.FindIndex(r => r.Part == rs.Part) != -1)
                {
                    return NotFound();
                }
                else
                {
                    if (resultExamForCheck.FindIndex(r => r.Part == rs.Part - 1) == -1)
                    {
                        return NotFound();
                    }
                    else
                    {
                        //get questions data
                        List<QuestionDataObj> listQuestionsData = new List<QuestionDataObj>();

                        //
                        resultExamForCheck.Add(rs);
                        foreach (QuestionResult qrs in rs.Result)
                        {
                            Question questionToCheck = _db.Question.SingleOrDefault(q => q.QuestionId == qrs.IdQuestion);
                            if (questionToCheck != null)
                            {
                                if (questionToCheck.Type == "1")
                                {
                                    List<QuestionResultType1> answers = JsonConvert.DeserializeObject<List<QuestionResultType1>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType1 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "2")
                                {
                                    List<QuestionResultType2> answers = JsonConvert.DeserializeObject<List<QuestionResultType2>>(qrs.AnswerJsonString);
                                    List<List<int>> rightAnswers = JsonConvert.DeserializeObject<List<List<int>>>(questionToCheck.TrueAnswers);

                                    bool questionCheck = false;

                                    int resultCheck = 0;
                                    foreach (QuestionResultType2 qrsType2 in answers)
                                    {
                                        foreach (List<int> trueAnswers in rightAnswers)
                                        {
                                            if (trueAnswers[0] == qrsType2.Answer.Left && trueAnswers[1] == qrsType2.Answer.Right)
                                            {
                                                resultCheck += 1;
                                                break;
                                            }
                                        }
                                    }
                                    if (resultCheck == rightAnswers.Count())
                                    {
                                        questionCheck = true;
                                    }
                                    if (questionCheck == true)
                                    {

                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "3")
                                {
                                    List<QuestionResultType3> answers = JsonConvert.DeserializeObject<List<QuestionResultType3>>(qrs.AnswerJsonString);
                                    bool questionCheck = true;
                                    foreach (QuestionResultType3 qrsType3 in answers)
                                    {
                                        Answer answer = _db.Answer.SingleOrDefault(a => a.AnswerId == qrsType3.IdAnswer);
                                        if (answer.Content.ToLower() != qrsType3.Answer)
                                        {
                                            questionCheck = false;
                                            break;
                                        }
                                    }

                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "4")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "5")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "6")
                                {
                                    List<QuestionResultType6> answers = JsonConvert.DeserializeObject<List<QuestionResultType6>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType6 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                            }
                        }
                        if (history.Status != 1 && rs.Part == (examQuestions.Count() - 1))
                        {
                            history.Status = 2;
                        }
                        string newResult = JsonConvert.SerializeObject(resultExamForCheck);
                        history.StudentResult = newResult;
                        history.EndDate = DateTime.Now;
                        try
                        {
                            _db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                        if (examQuestions.ElementAtOrDefault(rs.Part + 1) != null)
                        {
                            return Json(JsonConvert.SerializeObject(new { success = 1, next = rs.Part + 1 }));
                        }
                        return Json(JsonConvert.SerializeObject(new { success = 1 }));
                    }
                }
            }
            return NotFound();

        }
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult PostHomework(JsonResultHomeworkModel rs)
        {
            if (rs is null || rs.HTMLHistory is null || rs.IdHistory == 0 || rs.IdHomework == 0 || rs.Result is null)
            {
                return NotFound();
            }
            //Kiểm tra xem có phần kết quả trong lịch sử liền kề của bài thi này không
            var history = _db.HomeworkHistory.SingleOrDefault(h => h.HistoryId == rs.IdHistory);
            var homework = _db.Homework.SingleOrDefault(t => t.HomeworkId == rs.IdHomework);
            var homeworkQuestions = JsonConvert.DeserializeObject<List<ExamQuestionModel>>(homework.HomeworkQuestions);
            if (DateTime.Compare(homework.ExpiredDate.GetValueOrDefault(DateTime.Now), DateTime.Now) >= 0)
            {
                List<JsonResultHomeworkModel> listResult = new List<JsonResultHomeworkModel>();
                listResult.Add(rs);
                string newResult = JsonConvert.SerializeObject(listResult);
                history.Total = 0;
                foreach (JsonResultHomeworkModel jrs in listResult)
                {
                    foreach (QuestionResult qrs in jrs.Result)
                    {
                        Question questionToCheck = _db.Question.SingleOrDefault(q => q.QuestionId == qrs.IdQuestion);
                        if (questionToCheck != null)
                        {
                            if (!string.IsNullOrEmpty(qrs.AnswerJsonString))
                            {
                                if (questionToCheck.Type == "1")
                                {
                                    List<QuestionResultType1> answers = JsonConvert.DeserializeObject<List<QuestionResultType1>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType1 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "2")
                                {
                                    List<QuestionResultType2> answers = JsonConvert.DeserializeObject<List<QuestionResultType2>>(qrs.AnswerJsonString);
                                    List<List<int>> rightAnswers = JsonConvert.DeserializeObject<List<List<int>>>(questionToCheck.TrueAnswers);

                                    bool questionCheck = false;

                                    int resultCheck = 0;
                                    foreach (QuestionResultType2 qrsType2 in answers)
                                    {
                                        foreach (List<int> trueAnswers in rightAnswers)
                                        {
                                            if (trueAnswers[0] == qrsType2.Answer.Left && trueAnswers[1] == qrsType2.Answer.Right)
                                            {
                                                resultCheck += 1;
                                                break;
                                            }
                                        }
                                    }
                                    if (resultCheck == rightAnswers.Count())
                                    {
                                        questionCheck = true;
                                    }
                                    if (questionCheck == true)
                                    {

                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "3")
                                {
                                    List<QuestionResultType3> answers = JsonConvert.DeserializeObject<List<QuestionResultType3>>(qrs.AnswerJsonString);
                                    bool questionCheck = true;
                                    foreach (QuestionResultType3 qrsType3 in answers)
                                    {
                                        Answer answer = _db.Answer.SingleOrDefault(a => a.AnswerId == qrsType3.IdAnswer);
                                        if (answer.Content.ToLower() != qrsType3.Answer)
                                        {
                                            questionCheck = false;
                                            break;
                                        }
                                    }

                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                                else if (questionToCheck.Type == "4")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "5")
                                {
                                    history.Status = 1;
                                }
                                else if (questionToCheck.Type == "6")
                                {
                                    List<QuestionResultType6> answers = JsonConvert.DeserializeObject<List<QuestionResultType6>>(qrs.AnswerJsonString);
                                    List<string> rightAnswers = questionToCheck.TrueAnswers.Split(",").Where(a => !string.IsNullOrEmpty(a)).ToList();
                                    List<string> rightAnswersResult = new List<string>();
                                    foreach (QuestionResultType6 qrsType1 in answers)
                                    {
                                        if (qrsType1.Answer == true)
                                        {
                                            rightAnswersResult.Add(qrsType1.IdAnswer.ToString());
                                        }
                                    }
                                    bool questionCheck = true;
                                    if (rightAnswersResult.Count() == 0)
                                    {
                                        questionCheck = false;
                                    }
                                    else
                                    {
                                        foreach (string idAnswer in rightAnswersResult)
                                        {
                                            if (rightAnswers.IndexOf(idAnswer) == -1)
                                            {
                                                questionCheck = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (questionCheck == true)
                                    {
                                        history.Total += 1;
                                    }
                                }
                            }
                        }
                    }
                }
                history.StudentResult = newResult;
                history.EndDate = DateTime.Now;
                if (history.Status != 1)
                {
                    history.Status = 2;
                }
                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                return Json(JsonConvert.SerializeObject(new { success = 1 }));
            }
            return NotFound();

        }
        [Authorize(Roles = "Student")]
        [HttpGet]
        public IActionResult GetDocumentInCourseLevel3ByKey(int IdCourseLv3, string SearchKey)
        {
            if (IdCourseLv3 == 0)
            {
                return NotFound();
            }
            CourseLevel3 courseLvl3 = _db.CourseLevel3.FirstOrDefault(crs => crs.CourseId3 == IdCourseLv3);
            if (string.IsNullOrEmpty(courseLvl3.DocumentsId))
            {
                return Json(JsonConvert.SerializeObject(new List<Document>()));
            }
            List<string> listDoc = courseLvl3.DocumentsId.Split(",").ToList<string>();
            List<Document> queryDoc = _db.Document.ToList();
            List<Document> resultSearch = new List<Document>();
            if (SearchKey == null)
            {
                return Json(JsonConvert.SerializeObject(queryDoc));
            }
            queryDoc.ForEach(doc =>
            {
                if (listDoc.IndexOf(doc.DocId.ToString()) != -1 && doc.DocName.ToLower().IndexOf(SearchKey.ToLower()) != -1)
                {
                    resultSearch.Add(doc);
                }
            });
            return Json(JsonConvert.SerializeObject(resultSearch));
        }
        public IActionResult StreamFile(int FileId)
        {
            Document doc = _db.Document.Where(doc => doc.DocId == FileId).FirstOrDefault();
            if (doc != null)
            {
                string fileName = doc.Path;
                string filePath = "wwwroot/media/" + fileName;
                if (!System.IO.File.Exists(filePath))
                {

                    return NotFound();
                }
                byte[] video = System.IO.File.ReadAllBytes(filePath);
                long length = video.Length;
                long fSize = length;
                long startbyte = 0;
                long endbyte = fSize - 1;
                int statusCode = 200;
                if (!string.IsNullOrEmpty(Request.Headers["Range"].ToString()))
                {
                    //Get the actual byte range from the range header string, and set the starting byte.
                    string[] range = Request.Headers["Range"].ToString().Split(new char[] { '=', '-' });
                    startbyte = Convert.ToInt64(range[1]);
                    if (range.Length > 2 && range[2] != "") endbyte = Convert.ToInt64(range[2]);
                    //If the start byte is not equal to zero, that means the user is requesting partial content.
                    if (startbyte != 0 || endbyte != fSize - 1 || range.Length > 2 && range[2] == "")
                    { statusCode = 206; }//Set the status code of the response to 206 (Partial Content) and add a content range header.                                    
                }
                long desSize = endbyte - startbyte + 1;
                //Headers
                Response.StatusCode = statusCode;

                Response.ContentType = _db.Document.Where(doc => doc.DocId == FileId).FirstOrDefault().Type;
                Response.Headers.Add("Content-Accept", Response.ContentType);
                Response.Headers.Add("Content-Length", desSize.ToString());
                Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", startbyte, endbyte, fSize));
                //Data
                var stream = new MemoryStream(video, (int)startbyte, (int)desSize);
                return new FileStreamResult(stream, Response.ContentType);
            }
            return NotFound();
        }

        [Authorize(Roles = "Student")]
        public IActionResult History(int IdHistory)
        {
            if (IdHistory != 0)
            {
                TestHistory th = _db.TestHistory.SingleOrDefault(tsh => tsh.HistoryId == IdHistory);
                if (th != null)
                {
                    if (th.StudentId.ToString() != User.FindFirst(ClaimTypes.Authentication).Value)
                    {
                        Redirect("/");
                    }
                }
            }

            ViewData["idHistory"] = IdHistory.ToString();
            var tables = new StudentHistoryViewModel
            {
                Tests = _db.Test.ToList(),
                Students = _db_live.Students.ToList(),
                TestHistorys = _db.TestHistory.OrderByDescending(tsh => tsh.StartDate).ToList(),
                Questions = _db.Question.ToList(),
                Answers = _db.Answer.ToList()
            };
            return View(tables);
        }
        [Authorize(Roles = "Student")]
        public IActionResult Exam(int IdTest, int Part)
        {
            var tables = new StudentExamViewModel
            {
                Tests = _db.Test.ToList(),
                Questions = _db.Question.ToList(),
                Answers = _db.Answer.ToList(),
                Courses = _db.Course.ToList()
            };
            if (IdTest != 0)
            {
                Test test = _db.Test.SingleOrDefault(t => t.TestId == IdTest);
                if (test != null && test.Status != null && test.Status != 0)
                {
                    //check test in class?
                    bool isStudentInClass = false;
                    if (!string.IsNullOrEmpty(test.CourseIds))
                    {
                        List<string> listCourseId = test.CourseIds.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                        foreach (string cId in listCourseId)
                        {
                            Course course = _db.Course.SingleOrDefault(c => c.CourseId.ToString() == cId);
                            if (course != null)
                            {
                                if (course.StudentIds != null)
                                {
                                    List<string> listStudentId = course.StudentIds.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    if (listStudentId.IndexOf(User.FindFirst(ClaimTypes.Authentication).Value) != -1)
                                    {
                                        isStudentInClass = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isStudentInClass == true)
                        {
                            List<ExamQuestionModel> examQuestions = JsonConvert.DeserializeObject<List<ExamQuestionModel>>(test.TestQuestions);
                            if (examQuestions.Count > 0)
                            {
                                if (examQuestions.Count < Part + 1)
                                {
                                    ViewData["Error"] = "Bài thi không tồn tại phần này";
                                }
                                else
                                {
                                    //Lấy lịch sử làm bài gần nhất
                                    long idStudent = _db_live.Students.SingleOrDefault(st => st.UserName == User.FindFirst(ClaimTypes.NameIdentifier).Value).Id;
                                    TestHistory historyCheck = _db.TestHistory.OrderByDescending(tsh => tsh.StartDate).Where(tsh => tsh.StudentId == idStudent && tsh.TestId == test.TestId).ToList().FirstOrDefault();
                                    if (historyCheck != null)
                                    {
                                        //Nếu tồn tại lịch sử làm bài gần nhất
                                        //Hiệu của thời gian hiện tại trừ thời gian bắt đầu
                                        TimeSpan dateCheck = DateTime.Now.Subtract(historyCheck.StartDate.GetValueOrDefault(DateTime.Now.AddDays(1)));
                                        //Nếu tính từ thời gian làm bài gần nhất đến bây giờ vượt quá thời gian làm bài thi =>  Được làm bài thi tiếp theo
                                        if (dateCheck.TotalMilliseconds > (test.Time * 60000) + 30000 && dateCheck.TotalMilliseconds > 0)
                                        {
                                            //getTotal questions
                                            int totalQuestion = 0;
                                            double totalScore = 0;
                                            List<object> DetailWritingMark = new List<object>();
                                            foreach (ExamQuestionModel eqm in examQuestions)
                                            {
                                                foreach (QuestionGroupForExam qg in eqm.questions)
                                                {
                                                    totalQuestion += qg.questionIds.Count();
                                                    foreach (QuestionDataObj qdobj in qg.questionsData)
                                                    {
                                                        totalScore += qdobj.point;
                                                        if (qdobj.type == "4" || qdobj.type == "5")
                                                        {
                                                            DetailWritingMark.Add(new { QuestionId = qdobj.id, mark = 0.0, maxScore = qdobj.point });
                                                        }
                                                    }
                                                }
                                            }
                                            TestHistory newHistory = new TestHistory
                                            {
                                                StartDate = DateTime.Now,
                                                TestId = IdTest,
                                                StudentId = idStudent,
                                                TotalQuestions = totalQuestion,
                                                Status = 0,
                                                TotalScore = totalScore,
                                                DetailWritingMark = JsonConvert.SerializeObject(DetailWritingMark)
                                            };
                                            _db.TestHistory.Add(newHistory);
                                            try
                                            {
                                                _db.SaveChanges();
                                            }
                                            catch (Exception ex)
                                            {
                                                return NotFound(ex.Message);
                                            }
                                            tables.historyId = (int)newHistory.HistoryId;
                                            tables.examQuestions = examQuestions;

                                            ViewData["IdTest"] = IdTest.ToString();
                                            ViewData["Part"] = "0";
                                        }
                                        //Nếu chưa quá thời gian làm bài
                                        else
                                        {
                                            var examForCheck = examQuestions;
                                            if (!string.IsNullOrEmpty(historyCheck.StudentResult))
                                            {
                                                //Kiểm tra xem lịch sử làm bài gần nhất có phần "Part" trên url chưa
                                                var historyForCheck = JsonConvert.DeserializeObject<List<JsonResultModel>>(historyCheck.StudentResult);
                                                //Kiểm tra đề bài có phần "Part" trên url không/Kiểm tra người dùng đã làm phần liền kề trước đó chưa - Tính thời gian làm bài còn lại đủ để làm phần này không

                                                //Nếu chưa tồn tại phần bài làm này trong lịch sử thi gần nhất
                                                if (historyForCheck.FindIndex(i => i.Part == Part) == -1)
                                                {
                                                    //Kiểm tra phần liền kề trước đó có chưa
                                                    if (Part == 0)
                                                    {
                                                        ViewData["Error"] = $"Bạn đã làm phần bài thi này rồi. Bạn cách lần làm bài kế tiếp còn {string.Format("{0,6:##0.00}", (test.Time * 60000 - dateCheck.TotalMilliseconds) / 60000)} phút! phút!";
                                                    }
                                                    else
                                                    {
                                                        //Nếu không có dữ liệu của phần liền kề phía trước
                                                        if (historyForCheck.FindIndex(i => i.Part == Part - 1) == -1)
                                                        {
                                                            ViewData["Error"] = $"Không có dữ liệu bài làm của phần liền kề trước trong lịch sử làm bài thi này gần nhất của bạn. Bạn cách lần làm bài kế tiếp còn {string.Format("{0,6:##0.00}", (test.Time * 60000 - dateCheck.TotalMilliseconds) / 60000)} phút!";
                                                        }
                                                        else
                                                        {
                                                            //Kiểm tra xem thời gian còn lại có đủ thời gian làm phần thi này không
                                                            var restTime = (test.Time * 60000 - dateCheck.TotalMilliseconds) + 30000;
                                                            if (restTime > examForCheck[Part].totalTime * 60000)
                                                            {
                                                                tables.historyId = (int)historyCheck.HistoryId;
                                                                tables.examQuestions = examQuestions;
                                                                ViewData["IdTest"] = IdTest.ToString();
                                                                ViewData["Part"] = Part.ToString();
                                                            }
                                                            else
                                                            {
                                                                ViewData["Error"] = $"Thời gian làm bài thi còn lại({string.Format("{0,6:##0.00}", restTime / 60000)} phút), không đủ để làm phần thi này({examForCheck[Part].totalTime} phút). Bạn cách lần làm bài kế tiếp còn {string.Format("{0,6:##0.00}", (test.Time * 60000 - dateCheck.TotalMilliseconds + 30000) / 60000)} phút!";
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    ViewData["Error"] = $"Bạn đã làm phần bài thi này rồi. Bạn cách lần làm bài kế tiếp còn {string.Format("{0,6:##0.00}", (test.Time * 60000 - dateCheck.TotalMilliseconds + 30000) / 60000)} phút!";
                                                }
                                            }
                                            else
                                            {
                                                //Kiểm tra xem thời gian còn lại có đủ thời gian làm phần thi này không
                                                var restTime = (test.Time * 60000 - dateCheck.TotalMilliseconds) + 30000;
                                                if (restTime > examForCheck[0].totalTime * 60000)
                                                {
                                                    tables.historyId = (int)historyCheck.HistoryId;
                                                    tables.examQuestions = examQuestions;
                                                    ViewData["IdTest"] = IdTest.ToString();
                                                    ViewData["Part"] = "0";
                                                }
                                                else
                                                {
                                                    ViewData["Error"] = $"Thời gian làm bài thi còn lại({string.Format("{0,6:##0.00}", restTime / 60000)} phút), không đủ để làm phần thi này({examForCheck[Part].totalTime} phút). Bạn cách lần làm bài kế tiếp còn {string.Format("{0,6:##0.00}", (test.Time * 60000 - dateCheck.TotalMilliseconds + 30000) / 60000) } phút!";
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //getTotal questions
                                        int totalQuestion = 0;
                                        double totalScore = 0;
                                        List<object> DetailWritingMark = new List<object>();
                                        foreach (ExamQuestionModel eqm in examQuestions)
                                        {
                                            foreach (QuestionGroupForExam qg in eqm.questions)
                                            {
                                                totalQuestion += qg.questionIds.Count();
                                                foreach (QuestionDataObj qdobj in qg.questionsData)
                                                {
                                                    totalScore += qdobj.point;
                                                    if (qdobj.type == "4" || qdobj.type == "5")
                                                    {
                                                        DetailWritingMark.Add(new { QuestionId = qdobj.id, mark = 0.0, maxScore = qdobj.point });
                                                    }
                                                }
                                            }
                                        }
                                        TestHistory newHistory = new TestHistory
                                        {
                                            StartDate = DateTime.Now,
                                            TestId = IdTest,
                                            StudentId = idStudent,
                                            TotalQuestions = totalQuestion,
                                            Status = 0,
                                            TotalScore = totalScore,
                                            DetailWritingMark = JsonConvert.SerializeObject(DetailWritingMark)
                                        };
                                        _db.TestHistory.Add(newHistory);
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            return NotFound(ex.Message);
                                        }
                                        tables.examQuestions = examQuestions;
                                        tables.historyId = (int)newHistory.HistoryId;

                                        ViewData["IdTest"] = IdTest.ToString();
                                        ViewData["Part"] = "0";
                                    }
                                }

                            }
                            else
                            {
                                ViewData["Error"] = "Bài thi này không có câu hỏi nào!";
                            }
                        }
                        else
                        {
                            ViewData["Error"] = "Học viên không thuộc lớp có bài thi này!";
                        }
                    }
                    else
                    {
                        ViewData["Error"] = "Học viên không thuộc lớp có bài thi này!";
                    }
                }
                if (test.Status == 0 || test.Status == null)
                {
                    ViewData["Error"] = "Bài thi chưa được mở khóa!";
                }
            }
            return View(tables);
        }
    }
}
