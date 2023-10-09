using ExpertransDaoTao.Models;
using ExpertransDaoTao.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Security.Claims;


namespace ExpertransDaoTao.Controllers
{
    public class TeacherController : Controller
    {
        private readonly Models.expertrans_liveContext _liveDb;
        private readonly Models.expertrans_educateContext _educateDb;

        public TeacherController(Models.expertrans_liveContext liveDb, Models.expertrans_educateContext educateDb)
        {
            _liveDb = liveDb;
            _educateDb = educateDb;
        }
        [Authorize(Roles = "Teacher")]

        public IActionResult Index()
        {
            //Xóa tài liệu không được xác minh đã được thêm từ 6 tiếng trước trở lên
            List<Document> listDocToDelete = _educateDb.Document.Where(d => d.Confirm == 0).ToList();
            foreach (Document doc in listDocToDelete)
            {
                TimeSpan dateCheck = DateTime.Now.Subtract(doc.CreatedDate.GetValueOrDefault());
                if (dateCheck.TotalHours >= 6)
                {
                    string path = "wwwroot/media/" + doc.Path;
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    _educateDb.Document.Remove(doc);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                }
            }
            ////
            IEnumerable<Teachers> teacherInfo = _liveDb.Teachers.Where(t => t.UserName == User.FindFirst(ClaimTypes.NameIdentifier).Value).ToList();
            return View(teacherInfo);
        }

        #region Course

        [Authorize(Roles = "Teacher")]
        public IActionResult Course(long teacherId)
        {
            if (teacherId == 0)
            {
                teacherId = long.Parse(User.FindFirst(ClaimTypes.Authentication).Value);
            }
            List<AdminCourseShow> result = new List<AdminCourseShow>();
            List<Course> courseList = new List<Course>();
            var course = (from c in _educateDb.Course
                          select c).ToList();
            foreach (var c in course)
            {
                if (!String.IsNullOrEmpty(c.TeacherIds))
                {
                    string[] teacherIds = c.TeacherIds.Split(",");
                    if (teacherIds.Contains(teacherId.ToString()))
                    {
                        courseList.Add(c);
                    }
                }
            }
            foreach (var c in courseList)
            {
                var course2 = (from c2 in _educateDb.CourseLevel2
                               where c2.CourseId == c.CourseId
                               select c2).ToList();
                AdminCourseShow data = new AdminCourseShow()
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    CourseDescription = c.CourseDescription,
                    Course2 = course2
                };

                result.Add(data);
            }
            return View(result);
        }

        [Authorize(Roles = "Teacher")]
        [Route("[controller]/lop-hoc/{courseId}")]
        public IActionResult Detail(int courseId)
        {
            AdminCourseDetail result = new AdminCourseDetail();
            List<CourseData> courseData = new List<CourseData>();
            List<StudentData> studentData = new List<StudentData>();
            List<TeacherData> teacherData = new List<TeacherData>();

            var course = (from c in _educateDb.Course
                          where c.CourseId == courseId
                          select c).SingleOrDefault();

            var course2 = (from c2 in _educateDb.CourseLevel2
                           where c2.CourseId == courseId
                           orderby c2.Order
                           select c2).ToList();

            foreach (var cl2 in course2)
            {
                int id2 = cl2.CourseId2;
                string name2 = cl2.Name;
                int order2 = cl2.Order.Value;
                var course3 = (from c3 in _educateDb.CourseLevel3
                               where c3.CourseId2 == id2
                               select c3).ToList();
                CourseData data = new CourseData()
                {
                    CourseId2 = id2,
                    Course2Name = name2,
                    Course2Order = order2,
                    Course3Data = course3
                };
                courseData.Add(data);
            }

            if (!String.IsNullOrEmpty(course.StudentIds))
            {
                string[] studentIds = course.StudentIds.Split(",");
                foreach (var sId in studentIds)
                {
                    StudentData sd = new StudentData();
                    var query = (from s in _liveDb.Students
                                 where s.Id == long.Parse(sId)
                                 select s).SingleOrDefault();
                    sd.Id = query.Id;
                    sd.Name = query.Name;
                    sd.UserName = query.UserName;
                    sd.Email = query.Email;
                    sd.Mobile = query.Mobile;

                    studentData.Add(sd);
                }
            }

            if (!String.IsNullOrEmpty(course.TeacherIds))
            {
                string[] teacherIds = course.TeacherIds.Split(",");
                foreach (var tId in teacherIds)
                {
                    TeacherData td = new TeacherData();
                    var query = (from t in _liveDb.Teachers
                                 where t.Id == long.Parse(tId)
                                 select t).SingleOrDefault();
                    td.Id = query.Id;
                    td.Name = query.Name;
                    td.UserName = query.UserName;
                    td.Email = query.Email;
                    td.Mobile = query.Mobile;

                    teacherData.Add(td);
                }
            }

            result.CourseId = course.CourseId;
            result.CourseName = course.CourseName;
            result.CourseDescription = course.CourseDescription;
            result.StudentIds = course.StudentIds;
            result.TeacherIds = course.TeacherIds;
            result.Data = courseData;
            result.StudentRecord = studentData;
            result.TeacherRecord = teacherData;

            ViewData["courseId"] = courseId;
            return View(result);
        }

        [Authorize(Roles = "Teacher")]
        //Course level 2 handlers
        [HttpPost]
        public IActionResult AddNewCourseLevel2(int courseIdLevel1, string courseName, string courseDescription)
        {
            int count = (from c2 in _educateDb.CourseLevel2
                         where c2.CourseId == courseIdLevel1
                         select c2).Count();

            CourseLevel2 course = new CourseLevel2()
            {
                Name = courseName,
                Description = courseDescription,
                CourseId = courseIdLevel1,
                Order = count + 1
            };

            _educateDb.CourseLevel2.Add(course);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            string json = JsonConvert.SerializeObject(course);

            return Json(json);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourseLevel2Order(int courseId, string[] courseId2)
        {
            string[] courseIdArray = courseId2[0].Split(",".ToArray());
            for (int i = 0; i < courseIdArray.Length; i++)
            {
                var query = (from c2 in _educateDb.CourseLevel2
                             where c2.CourseId2 == int.Parse(courseIdArray[i])
                             select c2).SingleOrDefault();
                query.Order = i + 1;
            }

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            var result = (from c2 in _educateDb.CourseLevel2
                          where c2.CourseId == courseId
                          orderby c2.Order
                          select c2).ToList();

            return Json(JsonConvert.SerializeObject(result));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult RemoveCourseLevel2ById(int courseId, int? courseIdLevel2)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            if (courseIdLevel2 != null)
            {
                //Remove course 3
                var course3 = (from c3 in _educateDb.CourseLevel3
                               where c3.CourseId2 == courseIdLevel2
                               select c3).ToList();
                foreach (var c in course3)
                {
                    //Check null

                    //Remove documents
                    if (!String.IsNullOrEmpty(c.DocumentsId))
                    {
                        string[] course3doc = c.DocumentsId.Split(",");
                        foreach (string doc in course3doc)
                        {
                            var document = (from d in _educateDb.Document
                                            where d.DocId == Int32.Parse(doc)
                                            select d).SingleOrDefault();
                            _educateDb.Document.Remove(document);
                        }
                    }
                    //Remove content documents
                    if (!String.IsNullOrEmpty(c.DocIdsContent))
                    {
                        string[] contentDoc = c.DocIdsContent.Split(",");
                        foreach (string cdoc in contentDoc)
                        {
                            var c_document = (from d in _educateDb.Document
                                              where d.DocId == Int32.Parse(cdoc)
                                              select d).SingleOrDefault();
                            _educateDb.Document.Remove(c_document);
                        }
                    }
                    _educateDb.CourseLevel3.Remove(c);
                }
                //Get course 2 by id
                var query = (from c2 in _educateDb.CourseLevel2
                             where c2.CourseId2 == courseIdLevel2
                             select c2).SingleOrDefault();
                int? order = query.Order;
                //Remove course 2
                _educateDb.CourseLevel2.Remove(query);

                //Reorder
                var query2 = (from c in _educateDb.CourseLevel2
                              where c.CourseId == courseId
                              orderby c.Order ascending
                              select c).ToList();
                foreach (var course in query2)
                {
                    if (course.Order == (order + 1))
                    {
                        course.Order = order;
                        order += 1;
                    }
                }

                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }

                return Json(JsonConvert.SerializeObject(query, settings));
            }
            else
            {
                return Json(JsonConvert.SerializeObject(new { status = 0 }, settings));
            }
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetCourse(int courseId)
        {
            string json = "";
            var query = (from c in _educateDb.Course
                         where c.CourseId == courseId
                         select c).ToList();
            if (query != null)
            {
                json = JsonConvert.SerializeObject(query);
            }
            return Json(json);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetCourseLevel2(int courseId2)
        {
            var query = (from c in _educateDb.CourseLevel2
                         where c.CourseId2 == courseId2
                         select c).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetCourseLevel3(int courseIdLevel2)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId2 == courseIdLevel2
                         orderby c3.Order
                         select c3).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourseLevel2Info(int courseIdLevel2, string courseName, string courseDescription)
        {
            var query = (from c2 in _educateDb.CourseLevel2
                         where c2.CourseId2 == courseIdLevel2
                         select c2).SingleOrDefault();

            query.Name = courseName;
            query.Description = courseDescription;

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(JsonConvert.SerializeObject(query));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourseLevel3Info(int courseIdLevel3, string courseName, string courseDescription)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).SingleOrDefault();

            query.Name = courseName;
            query.Description = courseDescription;

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(JsonConvert.SerializeObject(query));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult RemoveCourseLevel3ById(int courseIdLevel2, int courseIdLevel3)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).SingleOrDefault();
            //Remove homeworks
            if (!String.IsNullOrEmpty(query.HomeworkIds))
            {
                string[] Homeworks = query.HomeworkIds.Split(",");
                foreach (string homework in Homeworks)
                {
                    var hw = _educateDb.Homework.SingleOrDefault(h => h.HomeworkId == long.Parse(homework));
                    //Set homework history
                    var homeworkHistories = (from h in _educateDb.HomeworkHistory where h.HomeworkId == long.Parse(homework) select h).ToList();
                    foreach (var history in homeworkHistories)
                    {
                        history.HomeworkId = null;
                    }
                    _educateDb.Homework.Remove(hw);
                }
            }

            //Remove documents
            if (!String.IsNullOrEmpty(query.DocumentsId))
            {
                string[] course3doc = query.DocumentsId.Split(",");
                foreach (string doc in course3doc)
                {
                    var document = (from d in _educateDb.Document
                                    where d.DocId == Int32.Parse(doc)
                                    select d).SingleOrDefault();
                    _educateDb.Document.Remove(document);
                }
            }
            //Remove content documents
            if (!String.IsNullOrEmpty(query.DocIdsContent))
            {
                string[] contentDoc = query.DocIdsContent.Split(",");
                foreach (string cdoc in contentDoc)
                {
                    var c_document = (from d in _educateDb.Document
                                      where d.DocId == Int32.Parse(cdoc)
                                      select d).SingleOrDefault();
                    _educateDb.Document.Remove(c_document);
                }
            }
            //Remove course 3
            _educateDb.CourseLevel3.Remove(query);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            var query2 = (from c3 in _educateDb.CourseLevel3
                          where c3.CourseId2 == courseIdLevel2
                          orderby c3.Order ascending
                          select c3).ToList();
            for (int i = 0; i < query2.Count; i++)
            {
                query2[i].Order = i + 1;
            }

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(JsonConvert.SerializeObject(query2));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult AddNewCourseLevel3(int courseIdLevel2, string courseName, string courseDescription)
        {
            int count = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId2 == courseIdLevel2
                         select c3).Count();
            CourseLevel3 course = new CourseLevel3()
            {
                Name = courseName,
                Description = courseDescription,
                CourseId2 = courseIdLevel2,
                Content = "[[]]",
                Questions = "[[]]",
                Order = count + 1
            };

            _educateDb.CourseLevel3.Add(course);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            string json = JsonConvert.SerializeObject(course);

            return Json(json);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourseLevel3Order(int courseIdLevel2, string[] courseId3)
        {
            string[] courseIdArray = courseId3[0].Split(",".ToArray());
            for (int i = 0; i < courseIdArray.Length; i++)
            {
                var query = (from c3 in _educateDb.CourseLevel3
                             where c3.CourseId3 == int.Parse(courseIdArray[i])
                             select c3).SingleOrDefault();
                query.Order = i + 1;
            }

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            var result = (from c3 in _educateDb.CourseLevel3
                          where c3.CourseId2 == courseIdLevel2
                          orderby c3.Order ascending
                          select c3).ToList();

            return Json(JsonConvert.SerializeObject(result));
        }

        [Authorize(Roles = "Teacher")]
        [Route("[controller]/bai-tap/{id}"),]
        [HttpGet]
        public IActionResult Course3Detail(int id) //Course level 3 detail
        {
            if (id != 0)
            {
                Course3DetailViewModel result = new Course3DetailViewModel();
                List<DocumentData> docResult = new List<DocumentData>();
                List<HomeworkData> hwResult = new List<HomeworkData>();
                var course3 = (from c3 in _educateDb.CourseLevel3
                               where c3.CourseId3 == id
                               select c3).SingleOrDefault();

                string course2Name = (from c2 in _educateDb.CourseLevel2
                                      where c2.CourseId2 == course3.CourseId2
                                      select c2.Name).SingleOrDefault();

                //Get documents
                if (!String.IsNullOrEmpty(course3.DocumentsId))
                {
                    string[] documents = course3.DocumentsId.Split(",");
                    foreach (var document in documents)
                    {
                        var query = (from d in _educateDb.Document
                                     where d.DocId == int.Parse(document)
                                     select d).SingleOrDefault();
                        if (query != null)
                        {
                            DocumentData docData = new DocumentData()
                            {
                                DocId = query.DocId,
                                DocName = query.DocName,
                                Path = query.Path,
                                Type = query.Type,
                                CreatedDate = query.CreatedDate
                            };
                            docResult.Add(docData);

                        }
                    }
                }

                //Get homeworks
                if (!String.IsNullOrEmpty(course3.HomeworkIds))
                {
                    string[] hwArray = course3.HomeworkIds.Split(",");
                    foreach (var hw in hwArray)
                    {
                        var query2 = (from h in _educateDb.Homework
                                      where h.HomeworkId == long.Parse(hw)
                                      select h).SingleOrDefault();
                        if(query2 != null)
                        {
                            HomeworkData hwData = new HomeworkData()
                            {
                                HomeworkId = query2.HomeworkId,
                                HomeworkName = query2.HomeworkName
                            };
                            hwResult.Add(hwData);
                        }
                    }
                }

                result.CourseId3 = course3.CourseId3;
                result.Name = course3.Name;
                result.Description = course3.Description;
                result.CourseId2 = course3.CourseId2;
                result.Course2Name = course2Name;
                result.DocumentsId = course3.DocumentsId;
                result.Questions = course3.Questions;
                result.Content = course3.Content;
                result.HomeworkIds = course3.HomeworkIds;
                result.Documents = docResult;
                result.Homeworks = hwResult;

                return View(result);
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourseLevel3Doc(int courseId3, string newDocs)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseId3
                         select c3).SingleOrDefault();

            if (!String.IsNullOrEmpty(query.DocumentsId) && !String.IsNullOrEmpty(newDocs))//If content doc not empty
            {
                string[] currentContentDocs = query.DocumentsId.Split(",");
                string[] docId = newDocs.Split(",");

                //Remove old if not in new doc
                foreach (string d1 in currentContentDocs)
                {
                    if (!docId.Contains(d1))
                    {
                        var document = (from doc in _educateDb.Document
                                        where doc.DocId == Int32.Parse(d1)
                                        select doc).SingleOrDefault();
                        document.Confirm = 0;
                    }
                }

                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }
            }
            else if (String.IsNullOrEmpty(query.DocumentsId) && !String.IsNullOrEmpty(newDocs))
            {
                string[] docId = newDocs.Split(",");
                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }

            }
            else if (!String.IsNullOrEmpty(query.DocumentsId) && String.IsNullOrEmpty(newDocs))
            {
                string[] currentContentDocs = query.DocumentsId.Split(",");
                //Remove all doc
                foreach (string d1 in currentContentDocs)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d1)
                                    select doc).SingleOrDefault();
                    document.Confirm = 0;
                }
            }

            query.DocumentsId = newDocs;

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(JsonConvert.SerializeObject(query));
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetQuestionById(int questionId)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            if (questionId != 0)
            {
                var question = (from q in _educateDb.Question
                                where q.QuestionId == questionId
                                select q).SingleOrDefault();
                var answer = (from a in _educateDb.Answer
                              where a.QuestionId == questionId
                              select a).ToList();

                Question result = new Question
                {
                    QuestionId = question.QuestionId,
                    Content = question.Content,
                    DocIdsContent = question.DocIdsContent,
                    TrueAnswers = question.TrueAnswers,
                    Type = question.Type,
                    Tag = question.Tag,
                    Answer = answer
                };
                return Json(JsonConvert.SerializeObject(result, settings));
            }
            else
            {
                return Json(JsonConvert.SerializeObject(new { status = 0 }, settings));
            }

        }


        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateCourse3(int courseIdLevel3, string content, string contentDoc, string questions)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).SingleOrDefault();

            if (!String.IsNullOrEmpty(query.DocIdsContent) && !String.IsNullOrEmpty(contentDoc))//If content doc not empty
            {
                string[] currentContentDocs = query.DocIdsContent.Split(",");
                string[] docId = contentDoc.Split(",");

                //Remove old if not in new doc
                foreach (string d1 in currentContentDocs)
                {
                    if (!docId.Contains(d1))
                    {
                        var document = (from doc in _educateDb.Document
                                        where doc.DocId == Int32.Parse(d1)
                                        select doc).SingleOrDefault();
                        document.Confirm = 0;
                    }
                }

                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }
            }
            else if (String.IsNullOrEmpty(query.DocIdsContent) && !String.IsNullOrEmpty(contentDoc))
            {
                string[] docId = contentDoc.Split(",");
                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }

            }
            else if (!String.IsNullOrEmpty(query.DocIdsContent) && String.IsNullOrEmpty(contentDoc))
            {
                string[] currentContentDocs = query.DocIdsContent.Split(",");
                //Remove all doc
                foreach (string d1 in currentContentDocs)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d1)
                                    select doc).SingleOrDefault();
                    document.Confirm = 0;
                }
            }

            query.Content = content;
            query.DocIdsContent = contentDoc;
            query.Questions = questions;
            _educateDb.SaveChanges();

            return Json(query);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetQuestionsData(string questionIds)
        {
            List<Question> result = new List<Question>();
            if (!String.IsNullOrEmpty(questionIds))
            {
                string[] data = questionIds.Split(",");
                foreach (var item in data)
                {
                    int id = Int32.Parse(item);
                    var question = _educateDb.Question.SingleOrDefault(q => q.QuestionId == id);
                    if (question != null)
                    {
                        result.Add(question);
                    }
                }
            }
            return Json(result);
        }



        #endregion

        #region Questions
        [Authorize(Roles = "Teacher")]


        public IActionResult Question()
        {

            //Hiển thị chính
            var tables = new QuestionViewModel
            {
                Questions = _educateDb.Question.Where(q => q.TeacherId == int.Parse(User.FindFirst(ClaimTypes.Authentication).Value)).ToList(),
                Answers = _educateDb.Answer.ToList(),
                Documents = _educateDb.Document.ToList()
            };
            return View(tables);
        }
        [Authorize(Roles = "Teacher")]

        [HttpPost]
        public IActionResult CreateQuestion(Question DataToCreate)
        {
            //Thêm câu hỏi mới
            Question newQuestion = new Question
            {
                Content = DataToCreate.Content,
                Type = DataToCreate.Type,
                DocIdsContent = DataToCreate.DocIdsContent,
                Tag = DataToCreate.Tag,
                TeacherId = int.Parse(User.FindFirst(ClaimTypes.Authentication).Value)
            };
            _educateDb.Question.Add(newQuestion);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            int newQuestionId = newQuestion.QuestionId;

            //Xác minh các tài liệu được sử dụng
            if (DataToCreate.DocIdsContent != null)
            {
                foreach (string s in DataToCreate.DocIdsContent.Split(",").Where(str => str != "").ToList())
                {
                    int docId = int.Parse(s);
                    Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == docId);
                    if (d != null)
                    {
                        d.Confirm = 1;
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
            }
            //Thêm câu trả lời
            if (DataToCreate.Type == "1" || DataToCreate.Type == "6")
            {
                foreach (Answer answer in DataToCreate.Answer)
                {
                    //Xác minh các tài liệu được sử dụng
                    Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                    string typeDoc = "text";
                    if (d != null)
                    {
                        typeDoc = d.Type;
                        d.Confirm = 1;
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    };

                    //Thêm câu trả lời
                    Answer newAnswer = new Answer
                    {
                        Content = answer.Content,
                        DocId = answer.DocId,
                        QuestionId = newQuestionId,
                        Type = typeDoc
                    };
                    _educateDb.Answer.Add(newAnswer);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                    int newAnswerId = newAnswer.AnswerId;

                    //Cập nhật câu trả lời đúng
                    var listTrueAnswers = DataToCreate.TrueAnswers.Split(",").Where(s => s != "").ToList();
                    if (listTrueAnswers.IndexOf(answer.AnswerId.ToString()) != -1)
                    {
                        Question questionToUpdate = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == newQuestionId);
                        if (questionToUpdate.TrueAnswers != null)
                        {
                            var oldTrueAnswers = questionToUpdate.TrueAnswers.Split(",").Where(ot => ot != newAnswerId.ToString()).ToList();
                            oldTrueAnswers.Add(newAnswerId.ToString());
                            string newTrueAnswers = String.Join(",", oldTrueAnswers);
                            questionToUpdate.TrueAnswers = newTrueAnswers;
                        }
                        else
                        {
                            string newTrueAnswers = newAnswerId.ToString();
                            questionToUpdate.TrueAnswers = newTrueAnswers;
                        }

                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
            }
            else if (DataToCreate.Type == "2")
            {
                var count = 0;
                List<List<int>> trueAnswers = new List<List<int>>();
                List<int> itemTrueAnswers = new List<int>();
                foreach (Answer answer in DataToCreate.Answer)
                {
                    //tăng biến đếm để xác định cặp
                    count += 1;

                    //Xác minh các tài liệu được sử dụng
                    Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                    string typeAns = "text";
                    if (d != null)
                    {
                        typeAns = d.Type;
                        d.Confirm = 1;
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    };

                    //Thêm câu trả lời
                    Answer newAnswer = new Answer
                    {
                        Content = answer.Content,
                        DocId = answer.DocId,
                        QuestionId = newQuestionId,
                        Type = typeAns
                    };
                    _educateDb.Answer.Add(newAnswer);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                    int newAnswerId = newAnswer.AnswerId;

                    //Cập nhật câu trả lời đúng
                    if (count == 1)
                    {
                        itemTrueAnswers.Add(newAnswerId);
                    }
                    if (count == 2)
                    {
                        itemTrueAnswers.Add(newAnswerId);
                        trueAnswers.Add(itemTrueAnswers);
                        itemTrueAnswers = new List<int>();
                        count = 0;
                    }
                }
                Question questionToUpdate = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == newQuestionId);
                questionToUpdate.TrueAnswers = JsonConvert.SerializeObject(trueAnswers);
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
            }
            else if (DataToCreate.Type == "3")
            {
                foreach (Answer answer in DataToCreate.Answer)
                {
                    //Xác minh các tài liệu được sử dụng
                    Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                    string typeAns = "text";
                    if (d != null)
                    {
                        typeAns = d.Type;
                        d.Confirm = 1;
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    };

                    //Thêm câu trả lời
                    Answer newAnswer = new Answer
                    {
                        Content = answer.Content,
                        DocId = answer.DocId,
                        QuestionId = newQuestionId,
                        Type = typeAns
                    };
                    _educateDb.Answer.Add(newAnswer);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                    int newAnswerId = newAnswer.AnswerId;

                    //Cập nhật câu trả lời đúng
                    Question questionToUpdate = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == newQuestionId);
                    questionToUpdate.Content = System.Text.RegularExpressions.Regex.Replace(questionToUpdate.Content, @"\[" + DataToCreate.Answer.ToList().IndexOf(answer).ToString() + @"\]", $"[{newAnswerId}]");
                    if (questionToUpdate.TrueAnswers != null)
                    {
                        var oldTrueAnswers = questionToUpdate.TrueAnswers.Split(",").Where(ot => ot != newAnswerId.ToString() && !string.IsNullOrEmpty(ot)).ToList();
                        oldTrueAnswers.Add(newAnswerId.ToString());
                        string newTrueAnswers = String.Join(",", oldTrueAnswers);
                        questionToUpdate.TrueAnswers = newTrueAnswers;
                    }
                    else
                    {
                        string newTrueAnswers = newAnswerId.ToString();
                        questionToUpdate.TrueAnswers = newTrueAnswers;
                    }
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                }
            }
            else if (DataToCreate.Type == "4")
            {

            }
            else if (DataToCreate.Type == "5")
            {

            }
            return Json(JsonConvert.SerializeObject(new { success = 1 }));
        }
        [Authorize(Roles = "Teacher")]

        [HttpPost]
        public IActionResult UpdateQuestion(Question DataToUpdate)
        {

            Question questionToUpdate = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == DataToUpdate.QuestionId);
            if (questionToUpdate != null)
            {
                if (questionToUpdate.TeacherId != int.Parse(User.FindFirst(ClaimTypes.Authentication).Value))
                {
                    return NotFound();
                }
                //Xóa câu trả lời đúng cũ
                questionToUpdate.TrueAnswers = null;
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                //Xóa tài liệu không sử dụng nữa              
                if (DataToUpdate.DocIdsContent == null)
                {
                    if (questionToUpdate.DocIdsContent != null)
                    {
                        var oldListDocToUse = questionToUpdate.DocIdsContent.Split(",").Where(s => !string.IsNullOrEmpty(s));

                        foreach (string oldDocIdToUse in oldListDocToUse)
                        {

                            int dId = int.Parse(oldDocIdToUse);
                            Document oldDocToDelete = _educateDb.Document.SingleOrDefault(d => d.DocId == dId);
                            if (oldDocToDelete != null)
                            {
                                string path = "wwwroot/media/" + oldDocToDelete.Path;
                                if (System.IO.File.Exists(path))
                                {
                                    System.IO.File.Delete(path);
                                }
                                _educateDb.Document.Remove(oldDocToDelete);
                                try
                                {
                                    _educateDb.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    return NotFound(ex.Message);
                                }
                            }
                        }
                    }

                }
                else
                {
                    if (questionToUpdate.DocIdsContent != null)
                    {
                        var oldListDocToUse = questionToUpdate.DocIdsContent.Split(",").Where(s => !string.IsNullOrEmpty(s));
                        var newListDocToUse = DataToUpdate.DocIdsContent.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();


                        foreach (string oldDocIdToUse in oldListDocToUse)
                        {
                            if (newListDocToUse.IndexOf(oldDocIdToUse) == -1)
                            {
                                int dId = int.Parse(oldDocIdToUse);
                                Document oldDocToDelete = _educateDb.Document.SingleOrDefault(d => d.DocId == dId);
                                if (oldDocToDelete != null)
                                {
                                    string path = "wwwroot/media/" + oldDocToDelete.Path;
                                    if (System.IO.File.Exists(path))
                                    {
                                        System.IO.File.Delete(path);
                                    }
                                    _educateDb.Document.Remove(oldDocToDelete);
                                    try
                                    {
                                        _educateDb.SaveChanges();
                                    }
                                    catch (Exception ex)
                                    {
                                        return NotFound(ex.Message);
                                    }
                                }
                            }
                        }
                        foreach (string newDocIdToUse in newListDocToUse)
                        {
                            int dId = int.Parse(newDocIdToUse);
                            Document newDocToUse = _educateDb.Document.SingleOrDefault(d => d.DocId == dId);
                            newDocToUse.Confirm = 1;
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        }
                    }
                }
                //Cập nhật câu hỏi mới
                var oldListAnswerToUse = _educateDb.Answer.Where(ans => ans.QuestionId == questionToUpdate.QuestionId).ToList();
                var newListAnswerToUse = DataToUpdate.Answer.ToList();
                foreach (Answer oldAnswer in oldListAnswerToUse)
                {
                    int idNewQuestionTakeDoc = newListAnswerToUse.FindIndex(ans => ans.DocId == oldAnswer.DocId);
                    if (idNewQuestionTakeDoc == -1)
                    {
                        //Xóa tài liệu thuộc câu trả lời
                        if (oldAnswer.DocId != null)
                        {
                            Document docAnsToDelete = _educateDb.Document.SingleOrDefault(doc => doc.DocId == oldAnswer.DocId);
                            if (docAnsToDelete != null)
                            {
                                var path = "wwwroot/media/" + docAnsToDelete.Path;
                                if (System.IO.File.Exists(path))
                                {
                                    System.IO.File.Delete(path);
                                }
                                _educateDb.Document.Remove(docAnsToDelete);
                                try
                                {
                                    _educateDb.SaveChanges();
                                }
                                catch (Exception ex)
                                {
                                    return NotFound(ex.Message);
                                }
                            }

                        }
                    }

                    _educateDb.Answer.Remove(oldAnswer);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                }
                //Cập nhật nội dung mới của câu hỏi
                questionToUpdate.Content = DataToUpdate.Content;
                questionToUpdate.Type = DataToUpdate.Type;
                questionToUpdate.DocIdsContent = DataToUpdate.DocIdsContent;
                questionToUpdate.Tag = DataToUpdate.Tag;
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                //Cập nhật câu trả lời mới
                //Thêm câu trả lời
                if (DataToUpdate.Type == "1" || DataToUpdate.Type == "6")
                {
                    foreach (Answer answer in DataToUpdate.Answer)
                    {
                        //Xác minh các tài liệu được sử dụng
                        Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                        string typeDoc = "text";
                        if (d != null)
                        {
                            typeDoc = d.Type;
                            d.Confirm = 1;
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        };

                        //Thêm câu trả lời
                        Answer newAnswer = new Answer
                        {
                            Content = answer.Content,
                            DocId = answer.DocId,
                            QuestionId = DataToUpdate.QuestionId,
                            Type = typeDoc
                        };
                        _educateDb.Answer.Add(newAnswer);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                        int newAnswerId = newAnswer.AnswerId;

                        //Cập nhật câu trả lời đúng
                        var listTrueAnswers = DataToUpdate.TrueAnswers.Split(",").Where(s => s != "").ToList();
                        if (listTrueAnswers.IndexOf(answer.AnswerId.ToString()) != -1)
                        {
                            Question questionToUpdateDetail = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == DataToUpdate.QuestionId);
                            if (questionToUpdateDetail.TrueAnswers != null)
                            {
                                var oldTrueAnswers = questionToUpdateDetail.TrueAnswers.Split(",").Where(ot => ot != newAnswerId.ToString()).ToList();
                                oldTrueAnswers.Add(newAnswerId.ToString());
                                string newTrueAnswers = String.Join(",", oldTrueAnswers);
                                questionToUpdateDetail.TrueAnswers = newTrueAnswers;
                            }
                            else
                            {
                                string newTrueAnswers = newAnswerId.ToString();
                                questionToUpdateDetail.TrueAnswers = newTrueAnswers;
                            }

                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        }
                    }
                }
                else if (DataToUpdate.Type == "2")
                {
                    var count = 0;
                    List<List<int>> trueAnswers = new List<List<int>>();
                    List<int> itemTrueAnswers = new List<int>();
                    foreach (Answer answer in DataToUpdate.Answer)
                    {
                        //tăng biến đếm để xác định cặp
                        count += 1;

                        //Xác minh các tài liệu được sử dụng
                        Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                        string typeAns = "text";
                        if (d != null)
                        {
                            typeAns = d.Type;
                            d.Confirm = 1;
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        };

                        //Thêm câu trả lời
                        Answer newAnswer = new Answer
                        {
                            Content = answer.Content,
                            DocId = answer.DocId,
                            QuestionId = DataToUpdate.QuestionId,
                            Type = typeAns
                        };
                        _educateDb.Answer.Add(newAnswer);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                        int newAnswerId = newAnswer.AnswerId;

                        //Cập nhật câu trả lời đúng
                        if (count == 1)
                        {
                            itemTrueAnswers.Add(newAnswerId);
                        }
                        if (count == 2)
                        {
                            itemTrueAnswers.Add(newAnswerId);
                            trueAnswers.Add(itemTrueAnswers);
                            itemTrueAnswers = new List<int>();
                            count = 0;
                        }
                    }
                    Question questionToUpdateDetail = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == DataToUpdate.QuestionId);
                    questionToUpdateDetail.TrueAnswers = JsonConvert.SerializeObject(trueAnswers);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                }
                else if (DataToUpdate.Type == "3")
                {
                    foreach (Answer answer in DataToUpdate.Answer)
                    {
                        //Xác minh các tài liệu được sử dụng
                        Document d = _educateDb.Document.SingleOrDefault(doc => doc.DocId == answer.DocId);
                        string typeAns = "text";
                        if (d != null)
                        {
                            typeAns = d.Type;
                            d.Confirm = 1;
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        };

                        //Thêm câu trả lời
                        Answer newAnswer = new Answer
                        {
                            Content = answer.Content,
                            DocId = answer.DocId,
                            QuestionId = DataToUpdate.QuestionId,
                            Type = typeAns
                        };
                        _educateDb.Answer.Add(newAnswer);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                        int newAnswerId = newAnswer.AnswerId;

                        //Cập nhật câu trả lời đúng
                        Question questionToUpdateDetail = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == DataToUpdate.QuestionId);
                        questionToUpdate.Content = System.Text.RegularExpressions.Regex.Replace(questionToUpdate.Content, @"\[" + DataToUpdate.Answer.ToList().IndexOf(answer).ToString() + @"\]", $"[{newAnswerId}]");
                        if (questionToUpdateDetail.TrueAnswers != null)
                        {
                            var oldTrueAnswers = questionToUpdateDetail.TrueAnswers.Split(",").Where(ot => ot != newAnswerId.ToString() && !string.IsNullOrEmpty(ot)).ToList();
                            oldTrueAnswers.Add(newAnswerId.ToString());
                            string newTrueAnswers = String.Join(",", oldTrueAnswers);
                            questionToUpdateDetail.TrueAnswers = newTrueAnswers;
                        }
                        else
                        {
                            string newTrueAnswers = newAnswerId.ToString();
                            questionToUpdateDetail.TrueAnswers = newTrueAnswers;
                        }
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
                else if (DataToUpdate.Type == "4")
                {

                }
                else if (DataToUpdate.Type == "5")
                {

                }
            }
            else
            {
                return NotFound();
            }
            return Json(JsonConvert.SerializeObject(new { success = 1 }));
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetQuestion(string Key, int NumberRecord, bool? All)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            if (All == null || All == false)
            {
                if (NumberRecord == 0)
                {
                    NumberRecord = 15;
                }
                List<Question> questions = _educateDb.Question.Where(q => q.TeacherId == int.Parse(User.FindFirst(ClaimTypes.Authentication).Value)).ToList<Question>();
                int countRecord = questions.Count;
                //If dont have Key return all question with number record
                if (string.IsNullOrEmpty(Key))
                {
                    foreach (Question ques in questions)
                    {
                        ques.Answer = _educateDb.Answer.Where(ans => ans.QuestionId == ques.QuestionId).ToList();
                    }
                    //Return question which have Key in name
                    return Json(JsonConvert.SerializeObject(new { questions = questions.Take(NumberRecord), more = questions.Count > NumberRecord }, settings));
                }

                List<Question> result = new List<Question>();
                foreach (Question ques in questions)
                {
                    //Check question name have Key
                    string contentForCompare = ques.Content;
                    contentForCompare = System.Text.RegularExpressions.Regex.Replace(contentForCompare, "<.*?>", String.Empty);
                    contentForCompare = System.Text.RegularExpressions.Regex.Replace(contentForCompare, @"\[(\S+)\]", "...............");
                    contentForCompare = System.Text.RegularExpressions.Regex.Replace(contentForCompare, @"&[^\s]*;", String.Empty);
                    List<string> listTag = new List<string>();
                    if (ques.Tag != null)
                    {
                        listTag = ques.Tag.Split(",").Select(s => s.ToLower()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }
                    var checkTag = false;
                    foreach (string s in listTag)
                    {
                        if (s.ToLower().IndexOf(Key.ToLower()) != -1)
                        {
                            checkTag = true;
                            break;
                        }
                    }
                    if (contentForCompare.ToLower().IndexOf(Key.ToLower()) != -1 || checkTag == true)
                    {
                        Question quesToAdd = ques;
                        quesToAdd.Answer = _educateDb.Answer.Where(ans => ans.QuestionId == quesToAdd.QuestionId).ToList();
                        result.Add(quesToAdd);
                    }
                }
                //Return question which have Key in name
                return Json(JsonConvert.SerializeObject(new { questions = result.Take(NumberRecord), more = result.Count > NumberRecord }, settings));
            }
            return Json(JsonConvert.SerializeObject(new { questions = _educateDb.Question.Where(q => q.TeacherId.ToString() == User.FindFirst(ClaimTypes.Authentication).Value).ToList<Question>() }, settings));
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult DeleteQuestion(int IdQuestion)
        {
            Question questionToDelete = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == IdQuestion);
            if (questionToDelete != null)
            {
                if (questionToDelete.TeacherId != int.Parse(User.FindFirst(ClaimTypes.Authentication).Value))
                {
                    return NotFound();
                }
                //Xóa câu hỏi khỏi bảng Test
                foreach (Test t in _educateDb.Test.ToList())
                {
                    if (t.TestQuestions != null)
                    {
                        List<ExamQuestionModel> newTestQuestions = new List<ExamQuestionModel>();
                        List<ExamQuestionModel> oldTestQuestions = JsonConvert.DeserializeObject<List<ExamQuestionModel>>(t.TestQuestions);
                        foreach (ExamQuestionModel eqm in oldTestQuestions)
                        {
                            ExamQuestionModel newObj = new ExamQuestionModel();
                            newObj.totalTime = eqm.totalTime;

                            List<QuestionGroupForExam> newQuestionGroups = new List<QuestionGroupForExam>();
                            foreach (QuestionGroupForExam qgfe in eqm.questions)
                            {
                                QuestionGroupForExam questionGroupForExam = new QuestionGroupForExam();
                                questionGroupForExam.docid = qgfe.docid;
                                questionGroupForExam.listenAgain = qgfe.listenAgain;

                                var newQuestionIds = qgfe.questionIds;
                                newQuestionIds.Remove(questionToDelete.QuestionId);
                                questionGroupForExam.questionIds = newQuestionIds;

                                if (qgfe.questionsData != null)
                                {
                                    var newQuestionsData = qgfe.questionsData.Where(data => data.id != questionToDelete.QuestionId).ToList();
                                    questionGroupForExam.questionsData = newQuestionsData;
                                }

                                if (!string.IsNullOrEmpty(qgfe.content))
                                {
                                    var newContent = qgfe.content;
                                    newContent = System.Text.RegularExpressions.Regex.Replace(newContent, $@"<br><\s*a[^>]*>question-{questionToDelete.QuestionId}<\s*/\s*a>", "");
                                    newContent = System.Text.RegularExpressions.Regex.Replace(newContent, $@"<\s*a[^>]*>question-{questionToDelete.QuestionId}<\s*/\s*a>", "");
                                    questionGroupForExam.content = newContent;
                                }

                                newQuestionGroups.Add(questionGroupForExam);
                            }
                            newObj.questions = newQuestionGroups;

                            newTestQuestions.Add(newObj);
                        }

                        t.TestQuestions = JsonConvert.SerializeObject(newTestQuestions);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
                //Xóa câu hỏi khỏi bảng Homework
                foreach (Homework hw in _educateDb.Homework.ToList())
                {
                    if (hw.HomeworkQuestions != null)
                    {
                        List<HomeworkQuestionModel> newHomeworkQuestions = new List<HomeworkQuestionModel>();
                        List<HomeworkQuestionModel> oldHomeworkQuestions = JsonConvert.DeserializeObject<List<HomeworkQuestionModel>>(hw.HomeworkQuestions);
                        foreach (HomeworkQuestionModel hwqm in oldHomeworkQuestions)
                        {
                            HomeworkQuestionModel newObj = new HomeworkQuestionModel();
                            List<QuestionGroupForExam> newQuestionGroups = new List<QuestionGroupForExam>();
                            foreach (QuestionGroupForExam qgfe in hwqm.questions)
                            {
                                QuestionGroupForExam questionGroupForExam = new QuestionGroupForExam();
                                questionGroupForExam.docid = qgfe.docid;
                                questionGroupForExam.listenAgain = qgfe.listenAgain;

                                var newQuestionIds = qgfe.questionIds;
                                newQuestionIds.Remove(questionToDelete.QuestionId);
                                questionGroupForExam.questionIds = newQuestionIds;

                                if (qgfe.questionsData != null)
                                {
                                    var newQuestionsData = qgfe.questionsData.Where(data => data.id != questionToDelete.QuestionId).ToList();
                                    questionGroupForExam.questionsData = newQuestionsData;
                                }

                                if (!string.IsNullOrEmpty(qgfe.content))
                                {
                                    var newContent = qgfe.content;
                                    newContent = System.Text.RegularExpressions.Regex.Replace(newContent, $@"<br><\s*a[^>]*>question-{questionToDelete.QuestionId}<\s*/\s*a>", "");
                                    newContent = System.Text.RegularExpressions.Regex.Replace(newContent, $@"<\s*a[^>]*>question-{questionToDelete.QuestionId}<\s*/\s*a>", "");
                                    questionGroupForExam.content = newContent;
                                }

                                newQuestionGroups.Add(questionGroupForExam);
                            }
                            newObj.questions = newQuestionGroups;

                            newHomeworkQuestions.Add(newObj);
                        }

                        hw.HomeworkQuestions = JsonConvert.SerializeObject(newHomeworkQuestions);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
                //Xóa câu hỏi khỏi bảng CourseLevel3
                foreach (CourseLevel3 clv3 in _educateDb.CourseLevel3.ToList())
                {
                    if (!string.IsNullOrEmpty(clv3.Questions))
                    {
                        var courseLevel3Questions = JsonConvert.DeserializeObject<List<List<int>>>(clv3.Questions);
                        var newCourseLevel3Questions = new List<List<int>>();
                        foreach (List<int> listQuestionId in courseLevel3Questions)
                        {
                            newCourseLevel3Questions.Add(listQuestionId.Where(qid => qid != questionToDelete.QuestionId).ToList());
                        }
                        clv3.Questions = JsonConvert.SerializeObject(newCourseLevel3Questions);
                        try
                        {
                            _educateDb.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            return NotFound(ex.Message);
                        }
                    }
                }
                //Xóa câu trả lời thuộc câu hỏi
                List<Answer> answersToDelete = _educateDb.Answer.Where(ans => ans.QuestionId == questionToDelete.QuestionId).ToList();

                foreach (Answer ans in answersToDelete)
                {
                    int ansDocId = ans.DocId.GetValueOrDefault(0);
                    _educateDb.Answer.Remove(ans);
                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }
                    //Xóa tài liệu thuộc câu trả lời
                    if (ansDocId != 0)
                    {
                        Document docAnsToDelete = _educateDb.Document.SingleOrDefault(doc => doc.DocId == ans.DocId);
                        if (docAnsToDelete != null)
                        {
                            var path = "wwwroot/media/" + docAnsToDelete.Path;
                            if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Delete(path);
                            }
                            _educateDb.Document.Remove(docAnsToDelete);
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        }
                    }

                };

                //Xóa Tài liệu thuộc câu hỏi
                if (questionToDelete.DocIdsContent != null)
                {
                    List<string> docIdToDelete = questionToDelete.DocIdsContent.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    foreach (string docId in docIdToDelete)
                    {
                        int dId = int.Parse(docId);
                        Document docQuesToDelete = _educateDb.Document.SingleOrDefault(doc => doc.DocId == dId);
                        if (docQuesToDelete != null)
                        {
                            var path = "wwwroot/media/" + docQuesToDelete.Path;
                            if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Delete(path);
                            }
                            _educateDb.Document.Remove(docQuesToDelete);
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        }
                    }
                    //Xóa câu hỏi thuộc đề thi
                    foreach (Test t in _educateDb.Test.ToList())
                    {
                        if (!string.IsNullOrEmpty(t.TestQuestions))
                        {
                            var examQuestions = JsonConvert.DeserializeObject<List<ExamQuestionModel>>(t.TestQuestions);
                            var newExamQuestion = new List<ExamQuestionModel>();
                            foreach (var examQuestion in examQuestions)
                            {
                                ExamQuestionModel examQuestionToAdd = new ExamQuestionModel();
                                examQuestionToAdd.totalTime = examQuestion.totalTime;
                                examQuestionToAdd.questions = new List<QuestionGroupForExam>();
                                foreach (QuestionGroupForExam questionGroup in examQuestion.questions)
                                {
                                    QuestionGroupForExam questionGroupToAdd = new QuestionGroupForExam();
                                    questionGroupToAdd.content = questionGroup.content;
                                    questionGroupToAdd.docid = questionGroup.docid;
                                    questionGroupToAdd.listenAgain = questionGroup.listenAgain;
                                    questionGroupToAdd.questionIds = questionGroup.questionIds.Where(qId => qId != questionToDelete.QuestionId).ToList();
                                    examQuestionToAdd.questions.Add(questionGroupToAdd);
                                }
                                newExamQuestion.Add(examQuestionToAdd);
                            }
                            t.TestQuestions = JsonConvert.SerializeObject(newExamQuestion);
                            try
                            {
                                _educateDb.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                return NotFound(ex.Message);
                            }
                        }
                    }
                }
                _educateDb.Question.Remove(questionToDelete);
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }

            }

            return Json(JsonConvert.SerializeObject(new { success = 1 }));
        }

        #endregion
        #region Test
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public IActionResult GetTestHistory(int historyId)
        {
            if (historyId == 0)
            {
                return NotFound();
            }
            TestHistory testHistory = _educateDb.TestHistory.SingleOrDefault(th => th.HistoryId == historyId);
            if (testHistory != null)
            {
                return Json(JsonConvert.SerializeObject(new { testHistory.DetailWritingMark }));
            }
            return NotFound();
        }
        [Authorize(Roles = "Teacher")]

        [HttpPost]
        public IActionResult UpdateMarkTestHistory(int historyId, string DetailWritingMark, double WritingMark)
        {
            if (historyId == 0 || string.IsNullOrEmpty(DetailWritingMark))
            {
                return NotFound();
            }
            TestHistory testHistory = _educateDb.TestHistory.SingleOrDefault(th => th.HistoryId == historyId);
            if (testHistory != null)
            {
                testHistory.WritingScore = WritingMark;
                testHistory.DetailWritingMark = DetailWritingMark;
                testHistory.Marker = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                testHistory.Status = 3;

                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                return Json(JsonConvert.SerializeObject(new { success = 1 }));
            }
            return NotFound();
        }
        [Authorize(Roles = "Teacher")]

        [HttpGet]
        public IActionResult GetHomeworkHistory(int historyId)
        {
            if (historyId == 0)
            {
                return NotFound();
            }
            HomeworkHistory homeworkHistory = _educateDb.HomeworkHistory.SingleOrDefault(th => th.HistoryId == historyId);
            if (homeworkHistory != null)
            {
                return Json(JsonConvert.SerializeObject(new { homeworkHistory.DetailWritingMark }));
            }
            return NotFound();
        }
        [Authorize(Roles = "Teacher")]

        [HttpPost]
        public IActionResult UpdateMarkHomeworkHistory(int historyId, string DetailWritingMark, double WritingMark)
        {
            if (historyId == 0 || string.IsNullOrEmpty(DetailWritingMark))
            {
                return NotFound();
            }
            HomeworkHistory homeworkHistory = _educateDb.HomeworkHistory.SingleOrDefault(th => th.HistoryId == historyId);
            if (homeworkHistory != null)
            {
                homeworkHistory.WritingScore = WritingMark;
                homeworkHistory.DetailWritingMark = DetailWritingMark;
                homeworkHistory.Marker = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                homeworkHistory.Status = 3;
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                return Json(JsonConvert.SerializeObject(new { success = 1 }));
            }
            return NotFound();
        }
        [Authorize(Roles = "Teacher")]
        public IActionResult TestHistory(int IdHistory)
        {
            if (IdHistory != 0)
            {
                TestHistory history = _educateDb.TestHistory.SingleOrDefault(h => h.HistoryId == IdHistory);
                if (history != null)
                {
                    bool checkTeacherPermission = false;
                    Test test = _educateDb.Test.SingleOrDefault(t => t.TestId == history.TestId);

                    if (test != null)
                    {
                        string teacherId = test.TeacherIds.Split(",").FirstOrDefault(teacherId => teacherId == User.FindFirst(ClaimTypes.Authentication).Value.ToString());
                        if (teacherId != null) checkTeacherPermission = true;
                    }

                    if (checkTeacherPermission == true)
                    {
                        ViewData["idHistory"] = IdHistory.ToString();
                    }
                    else
                    {
                        ViewData["Error"] = "Giáo viên không có quyền truy cập vào bản ghi lịch sử này";
                    }

                }
                else
                {
                    ViewData["Error"] = "Không tồn tại bản ghi lịch sử";
                }
            }
            var tables = new StudentHistoryViewModel
            {
                CourseLevel3s = _educateDb.CourseLevel3.ToList(),
                CourseLevel2s = _educateDb.CourseLevel2.ToList(),
                Courses = _educateDb.Course.ToList(),
                Tests = _educateDb.Test.ToList(),
                Students = _liveDb.Students.ToList(),
                Teachers = _liveDb.Teachers.ToList(),
                Users = _liveDb.Users.ToList(),
                TestHistorys = _educateDb.TestHistory.OrderByDescending(tsh => tsh.StartDate).ToList(),
                Questions = _educateDb.Question.ToList(),
                Answers = _educateDb.Answer.ToList()
            };
            return View(tables);
        }
        [Authorize(Roles = "Teacher")]

        #endregion

        #region Homework
        [Authorize(Roles = "Teacher")]
        public IActionResult RemoveHomework(int courseId3, long homeworkId, string hwStr)
        {
            var query = _educateDb.CourseLevel3.SingleOrDefault(c3 => c3.CourseId3 == courseId3);

            var homework = _educateDb.Homework.SingleOrDefault(hw => hw.HomeworkId == homeworkId);
            if (!String.IsNullOrEmpty(homework.DocumentIds))
            {
                string[] documents = homework.DocumentIds.Split(",");
                foreach (var doc in documents)
                {
                    var currentDoc = _educateDb.Document.SingleOrDefault(d => d.DocId == int.Parse(doc));
                    currentDoc.Confirm = 0;
                }
            }

            query.HomeworkIds = hwStr;
            _educateDb.Homework.Remove(homework);

            _educateDb.SaveChanges();

            return Json(query);
        }

        [Authorize(Roles = "Teacher")]
        public IActionResult AddHomework(int courseId3, string homeworkName)
        {
            var query = _educateDb.CourseLevel3.SingleOrDefault(c3 => c3.CourseId3 == courseId3);
            Homework hw = new Homework()
            {
                HomeworkName = homeworkName,
                HomeworkQuestions = "[]"
            };

            _educateDb.Homework.Add(hw);
            _educateDb.SaveChanges();

            if (!String.IsNullOrEmpty(query.HomeworkIds))
            {
                string[] hwIds = query.HomeworkIds.Split(",");
                List<string> temp = hwIds.ToList();
                temp.Add(hw.HomeworkId.ToString());
                hwIds = temp.ToArray();
                query.HomeworkIds = string.Join(",", hwIds);
            }
            else
            {
                query.HomeworkIds = hw.HomeworkId.ToString();
            }

            _educateDb.SaveChanges();
            return Json(hw);
        }

        [Authorize(Roles = "Teacher")]
        [Route("[controller]/homework/{id}")]
        [Route("[controller]/bai-tap/homework/{id}")]
        public IActionResult HomeworkDetail(long id)
        {
            var query = _educateDb.Homework.SingleOrDefault(hw => hw.HomeworkId == id);
            return View(query);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public IActionResult UpdateHomeworkData(int homeworkId, string homeworkName, string homeworkExpiredDate, string content, string contentDoc)
        {
            var query = (from h in _educateDb.Homework
                         where h.HomeworkId == homeworkId
                         select h).SingleOrDefault();
            DateTime expiredDate = DateTime.Parse(homeworkExpiredDate);
            if (!String.IsNullOrEmpty(query.DocumentIds) && !String.IsNullOrEmpty(contentDoc))//If content doc not empty
            {
                string[] currentContentDocs = query.DocumentIds.Split(",");
                string[] docId = contentDoc.Split(",");

                //Remove old if not in new doc
                foreach (string d1 in currentContentDocs)
                {
                    if (!docId.Contains(d1))
                    {
                        var document = (from doc in _educateDb.Document
                                        where doc.DocId == Int32.Parse(d1)
                                        select doc).SingleOrDefault();
                        document.Confirm = 0;
                    }
                }

                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }
            }
            else if (String.IsNullOrEmpty(query.DocumentIds) && !String.IsNullOrEmpty(contentDoc))
            {
                string[] docId = contentDoc.Split(",");
                //Update new docs
                foreach (string d2 in docId)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d2)
                                    select doc).SingleOrDefault();
                    document.Confirm = 1;
                }

            }
            else if (!String.IsNullOrEmpty(query.DocumentIds) && String.IsNullOrEmpty(contentDoc))
            {
                string[] currentContentDocs = query.DocumentIds.Split(",");
                //Remove all doc
                foreach (string d1 in currentContentDocs)
                {
                    var document = (from doc in _educateDb.Document
                                    where doc.DocId == Int32.Parse(d1)
                                    select doc).SingleOrDefault();
                    document.Confirm = 0;
                }
            }
            query.ExpiredDate = expiredDate;
            query.HomeworkName = homeworkName;
            query.HomeworkQuestions = content;
            query.DocumentIds = contentDoc;
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(JsonConvert.SerializeObject(query));
        }

        [Authorize(Roles = "Teacher")]
        public IActionResult HomeworkHistory(int IdHistory)
        {
            if (IdHistory != 0)
            {
                HomeworkHistory history = _educateDb.HomeworkHistory.SingleOrDefault(h => h.HistoryId == IdHistory);
                if (history != null)
                {
                    bool checkTeacherInClass = false;
                    Homework homework = _educateDb.Homework.SingleOrDefault(t => t.HomeworkId == history.HomeworkId);
                    foreach (CourseLevel3 c3 in _educateDb.CourseLevel3.ToList())
                    {
                        if (!string.IsNullOrEmpty(c3.HomeworkIds))
                        {
                            List<string> listHomework = c3.HomeworkIds.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                            if (listHomework.IndexOf(history.HomeworkId.ToString()) != -1)
                            {
                                CourseLevel2 c2 = _educateDb.CourseLevel2.SingleOrDefault(clv2 => clv2.CourseId2 == c3.CourseId2);
                                Course c = _educateDb.Course.SingleOrDefault(clv1 => clv1.CourseId == c2.CourseId);
                                if (!string.IsNullOrEmpty(c.TeacherIds) && !string.IsNullOrEmpty(c.StudentIds))
                                {
                                    List<string> listTeacher = c.TeacherIds.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    List<string> listStudent = c.StudentIds.Split(",").Where(s => !string.IsNullOrEmpty(s)).ToList();
                                    if (listTeacher.IndexOf(User.FindFirst(ClaimTypes.Authentication).Value) != -1 && listStudent.IndexOf(history.StudentId.ToString()) != -1)
                                    {
                                        checkTeacherInClass = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (checkTeacherInClass == true)
                    {
                        ViewData["idHistory"] = IdHistory.ToString();

                    }
                    else
                    {
                        ViewData["Error"] = "Giáo viên không có quyền truy cập vào bản ghi lịch sử này";

                    }

                }
                else
                {
                    ViewData["Error"] = "Không tồn tại bản ghi lịch sử";
                }
            }
            var tables = new StudentHomeworkHistoryViewModel
            {
                CourseLevel3s = _educateDb.CourseLevel3.ToList(),
                CourseLevel2s = _educateDb.CourseLevel2.ToList(),
                Courses = _educateDb.Course.ToList(),
                Homeworks = _educateDb.Homework.ToList(),
                Students = _liveDb.Students.ToList(),
                Teachers = _liveDb.Teachers.ToList(),
                Users = _liveDb.Users.ToList(),
                HomeworkHistorys = _educateDb.HomeworkHistory.OrderByDescending(tsh => tsh.StartDate).ToList(),
                Questions = _educateDb.Question.ToList(),
                Answers = _educateDb.Answer.ToList()
            };
            return View(tables);
        }
    }
    #endregion
}
