using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExpertransDaoTao.Models;
using ExpertransDaoTao.ViewModel;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Security.Claims;

namespace ExpertransDaoTao.Controllers
{
    public class AdminController : Controller
    {
        private readonly Models.expertrans_liveContext _liveDb;
        private readonly Models.expertrans_educateContext _educateDb;
        private readonly IWebHostEnvironment _webHostEnviroment;

        public AdminController(Models.expertrans_liveContext liveDb, Models.expertrans_educateContext educateDb, IWebHostEnvironment webHostEnviroment)
        {
            _liveDb = liveDb;
            _educateDb = educateDb;
            _webHostEnviroment = webHostEnviroment;
        }

        [Authorize(Roles = "Admin")]
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
            int courseCount = (from s in _educateDb.Course select s).Count();
            ViewData["courseCount"] = courseCount;
            int studentCount = (from a in _liveDb.Students select a).Count();
            ViewData["studentCount"] = studentCount;
            return View();
        }

        //Course handlers
        [Authorize(Roles = "Admin")]
        public IActionResult Course()
        {
            List<AdminCourseShow> result = new List<AdminCourseShow>();
            var course = (from c in _educateDb.Course
                          select c).ToList();
            foreach (var c in course)
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddCourse(string courseName, string courseDescription, string courseTag)
        {

            Course course = new Course()
            {
                CourseName = courseName,
                CourseDescription = courseDescription,
                Tag = courseTag
            };

            _educateDb.Course.Add(course);
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateCourse(int courseId, string courseName, string courseDescription, string courseTag)
        {
            var query = (from c in _educateDb.Course
                         where c.CourseId == courseId
                         select c).SingleOrDefault();
            query.CourseName = courseName;
            query.CourseDescription = courseDescription;
            query.Tag = courseTag;

            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult RemoveCourse(int? courseId)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            if (courseId != null)
            {
                //Get course
                var query2 = (from c in _educateDb.Course
                              where c.CourseId == courseId
                              select c).SingleOrDefault();
                //Remove from Test
                var Tests = (from t in _educateDb.Test
                             select t).ToList();
                foreach (var test in Tests)
                {
                    if (!String.IsNullOrEmpty(test.CourseIds))
                    {
                        string[] id = test.CourseIds.Split(",");
                        if (id.Contains(courseId.ToString()))
                        {
                            id = id.Where(e => e != courseId.ToString()).ToArray();
                            test.CourseIds = id.ToString();
                        }
                    }
                }
                //Get course level 2
                var query = (from c2 in _educateDb.CourseLevel2
                             where c2.CourseId == courseId
                             select c2).ToList();
                foreach (var c2 in query)
                {
                    var course3 = (from c3 in _educateDb.CourseLevel3
                                   where c3.CourseId2 == c2.CourseId2
                                   select c3).ToList();
                    foreach (var c3 in course3)
                    {
                        //Remove course 3 documents
                        if (!String.IsNullOrEmpty(c3.DocumentsId))//If document not null
                        {
                            string[] course3doc = c3.DocumentsId.Split(",");
                            foreach (string doc in course3doc)
                            {
                                var document = (from d in _educateDb.Document
                                                where d.DocId == Int32.Parse(doc)
                                                select d).SingleOrDefault();
                                _educateDb.Document.Remove(document);
                            }
                        }
                        //Remove content documents
                        if (!String.IsNullOrEmpty(c3.DocIdsContent))
                        {
                            string[] contentDoc = c3.DocIdsContent.Split(",");
                            foreach (string cdoc in contentDoc)
                            {
                                var c_document = (from d in _educateDb.Document
                                                  where d.DocId == Int32.Parse(cdoc)
                                                  select d).SingleOrDefault();
                                _educateDb.Document.Remove(c_document);
                            }
                        }
                        //Remove course 3
                        _educateDb.CourseLevel3.Remove(c3);
                    }
                    //Remove course 2
                    _educateDb.CourseLevel2.Remove(c2);
                }


                _educateDb.Course.Remove(query2);
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                return Json(JsonConvert.SerializeObject(query2, settings));
            }
            else
            {
                return Json(JsonConvert.SerializeObject(new { status = 0 }, settings));
            }

        }

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAllCourseLevel2(bool valid)
        {
            string json = "";
            if (valid)
            {
                var query = (from c2 in _educateDb.CourseLevel2
                             join c in _educateDb.Course
                             on c2.CourseId equals c.CourseId
                             select new { c2, c.CourseId, c.CourseName }).ToList();
                json = JsonConvert.SerializeObject(query);
            }
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetCourseLevel2(int courseId2)
        {
            var query = (from c in _educateDb.CourseLevel2
                         where c.CourseId2 == courseId2
                         select c).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetCourseLevel2ByCourseId(int courseId)
        {
            var query = (from c in _educateDb.CourseLevel2
                         where c.CourseId == courseId
                         orderby c.Order ascending
                         select c).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        //Course level 3 handlers

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
                    foreach(var history in homeworkHistories)
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateCourseLevel3QuestionsOrder(int courseIdLevel3, string questions)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).SingleOrDefault();
            query.Questions = questions;

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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetCourseLevel3ById(int courseIdLevel3)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddQuestionToCourseLevel3(int questionId, int courseIdLevel3)
        {
            bool checkQuestion = false;
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseIdLevel3
                         select c3).SingleOrDefault();
            if (query.Questions != null)
            {
                checkQuestion = query.Questions.Contains(questionId.ToString());
            }
            var query2 = (from a in _educateDb.Answer
                          where a.QuestionId == questionId
                          select a).ToList();
            if (!checkQuestion)
            {
                if (query2.Count > 1)// Check total answer
                {

                    //If there are no question existed
                    if (query.Questions == null)
                    {
                        query.Questions += questionId.ToString();
                    }
                    else//if there are question, then append string 
                    {
                        query.Questions += "," + questionId.ToString();
                    }

                    try
                    {
                        _educateDb.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        return NotFound(ex.Message);
                    }

                    return Json(query);

                }
                else
                {
                    return Json(new { status = "inValid" });
                }
            }
            else
            {
                return Json(new { status = "existed" });
            }

        }

        [Authorize(Roles = "Admin")]
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
                    document.Trace = "course-3-" + courseId3 + "-doc";
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
                    document.Trace = "course-3-" + courseId3 + "-doc";
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateCourseLevel3Homework(int courseId3, string homeworkIds)
        {
            var query = (from c3 in _educateDb.CourseLevel3
                         where c3.CourseId3 == courseId3
                         select c3).SingleOrDefault();

            query.HomeworkIds = homeworkIds;

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


        [Authorize(Roles = "Admin")]
        [Route("[controller]/bai-tap/{id}"),]
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

        [Authorize(Roles = "Admin")]
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

        //Việt Anh

        [Authorize(Roles = "Admin")]
        public IActionResult Question()
        {

            //Hiển thị chính
            var tables = new QuestionViewModel
            {
                Questions = _educateDb.Question.ToList(),
                Answers = _educateDb.Answer.ToList(),
                Documents = _educateDb.Document.ToList()
            };
            return View(tables);
        }
        [Authorize(Roles = "Admin,Student,Teacher")]
        [Route("[controller]/[action]/{FileId}")]
        public IActionResult File(int FileId)
        {
            Document doc = _educateDb.Document.Where(doc => doc.DocId == FileId).FirstOrDefault();
            if (doc != null)
            {
                string fileName = _educateDb.Document.Where(doc => doc.DocId == FileId).FirstOrDefault().Path;
                var net = new WebClient();
                if (!System.IO.File.Exists("wwwroot/media/" + fileName))
                {
                    return NotFound();
                }
                var data = net.DownloadData("wwwroot/media/" + fileName);
                var content = new MemoryStream(data);
                var contentType = "APPLICATION/octet-stream";
                return File(content, contentType, fileName);
            }
            return NotFound();
        }
        [Authorize(Roles = "Admin,Student,Teacher")]
        [Route("[controller]/[action]/{FileId}")]
        public IActionResult StreamFile(int FileId)
        {
            Document doc = _educateDb.Document.Where(doc => doc.DocId == FileId).FirstOrDefault();
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

                Response.ContentType = _educateDb.Document.Where(doc => doc.DocId == FileId).FirstOrDefault().Type;
                Response.Headers.Add("Content-Accept", Response.ContentType);
                Response.Headers.Add("Content-Length", desSize.ToString());
                Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", startbyte, endbyte, fSize));
                //Data
                var stream = new MemoryStream(video, (int)startbyte, (int)desSize);
                return new FileStreamResult(stream, Response.ContentType);
            }
            return NotFound();
        }

        //upload file doc
        [Authorize(Roles = "Admin,Teacher")]
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> UploadDoc(string Filename)
        {
            //Get file from request
            var DocFile = Request.Form.Files[0];
            //Check enough param
            if (DocFile == null)
            {
                return NotFound();
            }

            //Add file
            string uploadsFolder = Path.Combine(_webHostEnviroment.WebRootPath, "media");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string tail = DocFile.FileName.Split(".").ToList<string>().Last();
            string fileName = Guid.NewGuid().ToString() + "." + tail;
            string filePath = Path.Combine(uploadsFolder, fileName);
            FileStream output = new FileStream(filePath, FileMode.Create);
            await DocFile.CopyToAsync(output);
            await output.DisposeAsync();
            output.Close();
            //Create new record for file
            Document newDoc = new Document
            {
                Path = fileName,
                CreatedDate = DateTime.Now,
                Type = DocFile.ContentType,
                DocName = "Tài liệu " + DocFile.ContentType,
                Confirm = 0
            };
            if (!string.IsNullOrEmpty(Filename))
            {
                newDoc.DocName = Filename;
            }
            _educateDb.Document.Add(newDoc);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            //Return id from new record added
            return Json(JsonConvert.SerializeObject(newDoc));
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CreateQuestion(Question DataToCreate)
        {
            //Thêm câu hỏi mới
            Question newQuestion = new Question
            {
                Content = DataToCreate.Content,
                Type = DataToCreate.Type,
                DocIdsContent = DataToCreate.DocIdsContent,
                Tag = DataToCreate.Tag
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
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateQuestion(Question DataToUpdate)
        {

            Question questionToUpdate = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == DataToUpdate.QuestionId);
            if (questionToUpdate != null)
            {
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

        [Authorize(Roles = "Admin")]
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
                List<Question> questions = _educateDb.Question.ToList<Question>();
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
            return Json(JsonConvert.SerializeObject(new { questions = _educateDb.Question.ToList<Question>() }, settings));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteQuestion(int IdQuestion)
        {
            Question questionToDelete = _educateDb.Question.SingleOrDefault(ques => ques.QuestionId == IdQuestion);
            if (questionToDelete != null)
            {
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
                            newObj.partName = eqm.partName;

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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteAnswer(int answerId)
        {

            var query = (from a in _educateDb.Answer
                         where a.AnswerId == answerId
                         select a).SingleOrDefault();
            _educateDb.Answer.Remove(query);
            _educateDb.SaveChanges();
            return Json(new { status = 1, message = "Answer removed" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAllQuestions(string value)
        {
            List<Question> query = new List<Question>();
            List<Question> questions = new List<Question>();

            if (value == "" || value == null)
            {
                query = (from q in _educateDb.Question
                         select q).ToList();

            }
            else
            {
                query = (from q in _educateDb.Question
                         where q.Content.Contains(value)
                         select q).ToList();
            }

            foreach (var q in query)
            {
                int countAnswer = (from a in _educateDb.Answer
                                   where a.QuestionId == q.QuestionId
                                   select a).Count();
                if (countAnswer > 1)
                {
                    questions.Add(q);
                }
            }

            return Json(questions);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetAnswers(int idQuestion)
        {
            var answers = from a in _educateDb.Answer
                          join b in _educateDb.Question on a.QuestionId equals b.QuestionId
                          where a.QuestionId == idQuestion
                          select new { a, b.Content, b.TrueAnswers };
            return Json(JsonConvert.SerializeObject(answers.ToList()));
        }

        [Authorize(Roles = "Admin, Teacher")]
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

        [Authorize(Roles = "Admin,Teacher")]
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

        [Authorize(Roles = "Admin")]
        public IActionResult Student()
        {
            List<StudentCourseViewModel> objList = new List<StudentCourseViewModel>();
            Dictionary<long, string> dic = new Dictionary<long, string>();

            var classRecord = (from c in _educateDb.Course
                               select c).ToList();
            foreach (var record in classRecord)
            {
                if (!String.IsNullOrEmpty(record.StudentIds))
                {
                    string[] s = record.StudentIds.Split(",");
                    foreach (var student in s)
                    {
                        long id = long.Parse(student);
                        dic[id] = record.CourseName;
                    }
                }
            }
            var Students = from s in _liveDb.Students
                           select s;
            foreach (var student in Students)
            {
                StudentCourseViewModel obj = new StudentCourseViewModel();
                long studentId = student.Id;
                string courseName = "";

                if (dic.ContainsKey(studentId))
                {
                    courseName = dic[studentId];
                }
                obj.Id = student.Id;
                obj.UserName = student.UserName;
                obj.Name = student.Name;
                obj.CourseName = courseName;
                obj.Email = student.Email;
                obj.Mobile = student.Mobile;

                objList.Add(obj);
            }
            return View(objList);
        }

        [Authorize(Roles = "Admin")]
        public JsonResult GetStudentById(long studentId)
        {
            var query = (from s in _liveDb.Students
                         where s.Id == studentId
                         select s).SingleOrDefault();
            return Json(query);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Teacher()
        {
            List<TeacherCourseViewModel> result = new List<TeacherCourseViewModel>();
            var Courses = (from c in _educateDb.Course select c).ToList();
            var Teachers = (from t in _liveDb.Teachers select t).ToList();
            foreach (var teacher in Teachers)
            {
                List<string> courseNameList = new List<string>();
                foreach (var course in Courses)
                {
                    if (!String.IsNullOrEmpty(course.TeacherIds))
                    {
                        string[] t = course.TeacherIds.Split(",");
                        if (t.Contains(teacher.Id.ToString()))
                        {
                            courseNameList.Add(course.CourseName);
                        }
                    }
                }
                string courseName = string.Join(", ", courseNameList);
                TeacherCourseViewModel resultT = new TeacherCourseViewModel()
                {
                    Id = teacher.Id,
                    UserName = teacher.UserName,
                    Name = teacher.Name,
                    Email = teacher.Email,
                    Mobile = teacher.Mobile,
                    CourseName = courseName
                };
                result.Add(resultT);
            }
            return View(result);
        }

        [Authorize(Roles = "Admin")]
        public JsonResult GetTeacherById(long teacherId)
        {
            var query = (from t in _liveDb.Teachers
                         where t.Id == teacherId
                         select t).SingleOrDefault();
            return Json(query);
        }

        [Authorize(Roles = "Admin")]
        public JsonResult UpdateClass(int courseId, string studentIds, string teacherIds)
        {
            var query = (from c in _educateDb.Course
                         where c.CourseId == courseId
                         select c).SingleOrDefault();

            query.StudentIds = studentIds;
            query.TeacherIds = teacherIds;

            _educateDb.SaveChanges();

            return Json(new { success = true });
        }

        //Document handlers
        [Authorize(Roles = "Admin")]
        public IActionResult Document()
        {
            var objList = (from d in _educateDb.Document
                           where d.Confirm == 1
                           select d).ToList<Document>();
            return View(objList);
        }

        //Document search (not using ajax)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Document(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                var objList = (from d in _educateDb.Document
                               where d.Confirm == 1 && d.DocName.Contains(value)
                               select d).ToList<Document>();
                return View(objList);
            }
            else
            {
                var objList = (from d in _educateDb.Document
                               where d.Confirm == 1
                               select d).ToList<Document>();
                return View(objList);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Document(ExpertransDaoTao.ViewModel.AdminDocumentAdd document)
        {
            string uniqueFileName = "";
            if (document.File != null)
            {
                //Set file path 
                string uploadFolder = Path.Combine(_webHostEnviroment.WebRootPath, "media");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + document.File.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                //Add file to destination folder
                document.File.CopyTo(new FileStream(filePath, FileMode.Create));

            }

            Document newDocument = new Document
            {
                DocName = document.DocName,
                Path = uniqueFileName,
                Type = document.File.ContentType,
                CreatedDate = DateTime.Now,
            };

            _educateDb.Document.Add(newDocument);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            IEnumerable<ExpertransDaoTao.Models.Document> objList = _educateDb.Document;
            return View(objList);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetDocumentById(int docId)
        {
            var query = (from d in _educateDb.Document
                         where d.DocId == docId
                         select d).SingleOrDefault();
            if (query.Confirm == 0)
            {
                query.Confirm = 1;
                _educateDb.SaveChanges();
            }
            return Json(JsonConvert.SerializeObject(query));
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteDocument(int Id)
        {
            var document = (from d in _educateDb.Document
                            where d.DocId == Id
                            select d).SingleOrDefault();
            if (document != null)
            {
                _educateDb.Document.Remove(document);
                try
                {
                    _educateDb.SaveChanges();
                }
                catch (Exception ex)
                {
                    return NotFound(ex.Message);
                }
                string fullPath = "wwwroot/media/" + document.Path;
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            return Json(new { status = "ok" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult GetTestByTestId(int testId)
        {
            var query = (from t in _educateDb.Test
                         where t.TestId == testId
                         select t).ToList();
            string json = JsonConvert.SerializeObject(query);
            return Json(json);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddTest(string testName)
        {
            Test test = new Test
            {
                TestName = testName,
                TestQuestions = "[]",
                Time = 0,
                Status = 0,
                DocumentsId = null
            };

            _educateDb.Test.Add(test);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            string json = JsonConvert.SerializeObject(test);
            return Json(json);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateTestData(int testId, string testName, int time, string content, string contentDoc, string courseIds, string teacherIds)
        {
            var query = (from t in _educateDb.Test
                         where t.TestId == testId
                         select t).SingleOrDefault();

            if (!String.IsNullOrEmpty(query.DocumentsId) && !String.IsNullOrEmpty(contentDoc))//If content doc not empty
            {
                string[] currentContentDocs = query.DocumentsId.Split(",");
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
            else if (String.IsNullOrEmpty(query.DocumentsId) && !String.IsNullOrEmpty(contentDoc))
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
            else if (!String.IsNullOrEmpty(query.DocumentsId) && String.IsNullOrEmpty(contentDoc))
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

            query.TestName = testName;
            query.Time = time;
            query.TestQuestions = content;
            query.DocumentsId = contentDoc;
            query.CourseIds = courseIds;
            query.TeacherIds = teacherIds;
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateTestStatus(int testId)
        {
            var query = (from t in _educateDb.Test
                         where t.TestId == testId
                         select t).SingleOrDefault();
            if (query.Status == 1)
            {
                query.Status = 0;
            }
            else
            {
                query.Status = 1;
            }

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
        [Authorize(Roles = "Admin")]
        public IActionResult Test()
        {
            var query = (from t in _educateDb.Test
                         orderby t.TestId descending
                         select t).ToList();
            return View(query);
        }
        [Authorize(Roles = "Admin")]
        [Route("/test/chi-tiet/{testId}")]
        public IActionResult TestDetail(int testId)
        {
            if (testId != 0)
            {
                TestDetailViewModel result = new TestDetailViewModel();
                //Course components get
                List<Course1Data> courses = new List<Course1Data>();
                List<Select2Data> selectedCourses = new List<Select2Data>();
                //Teacher components get
                List<Teacher1Data> teachers = new List<Teacher1Data>();
                List<Select2Data> selectedTeachers = new List<Select2Data>();
                //Get test data
                var query = (from t in _educateDb.Test
                             where t.TestId == testId
                             select t).SingleOrDefault();
                //Get all course for select
                var course = (from c in _educateDb.Course
                              select c).ToList();
                foreach (var ce in course)
                {
                    Course1Data cd1 = new Course1Data()
                    {
                        CourseId = ce.CourseId,
                        CourseName = ce.CourseName
                    };
                    courses.Add(cd1);
                }

                //Get all teachers for select

                var teacher = (from t in _liveDb.Teachers select t).ToList();

                foreach (var te in teacher)
                {
                    Teacher1Data td1 = new Teacher1Data()
                    {
                        Id = te.Id,
                        Name = te.Name,
                        UserName = te.UserName
                    };
                    teachers.Add(td1);
                }

                //Check null courseIds and add to selected course data
                if (!String.IsNullOrEmpty(query.CourseIds))
                {
                    string[] selectedCourseIds = query.CourseIds.Split(",");
                    foreach (var sc in selectedCourseIds)
                    {
                        var item = (from c in _educateDb.Course
                                    where c.CourseId == Int32.Parse(sc)
                                    select c).SingleOrDefault();
                        Select2Data cd2 = new Select2Data()
                        {
                            id = item.CourseId,
                            text = item.CourseName
                        };
                        selectedCourses.Add(cd2);
                    }
                }

                //Check null teacherIds and add to selected course data
                if (!String.IsNullOrEmpty(query.TeacherIds))
                {
                    string[] selectedTeacherIds = query.TeacherIds.Split(",");
                    foreach (var st in selectedTeacherIds)
                    {
                        var teacherData = _liveDb.Teachers.SingleOrDefault(t => t.Id == long.Parse(st));
                        Select2Data td2 = new Select2Data()
                        {
                            id = teacherData.Id,
                            text = teacherData.Name
                        };
                        selectedTeachers.Add(td2);
                    }
                }

                result.TestId = query.TestId;
                result.TestName = query.TestName;
                result.Time = query.Time;
                result.TestQuestions = query.TestQuestions;
                result.Courses = courses;
                result.CourseIds = query.CourseIds;
                result.Teachers = teachers;
                result.TeacherIds = query.TeacherIds;
                result.SelectedCourses = selectedCourses;
                result.SelectedTeachers = selectedTeachers;

                return View(result);
            }
            else
            {
                return NotFound();
            }

        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult TestRemove(int testId)
        {
            var query = (from t in _educateDb.Test
                         where t.TestId == testId
                         select t).SingleOrDefault();
            string json = JsonConvert.SerializeObject(query);
            var query2 = (from h in _educateDb.TestHistory
                          where h.TestId == testId
                          select h).ToList();
            foreach (var history in query2)
            {
                history.TestId = null;
            }
            _educateDb.Test.Remove(query);
            try
            {
                _educateDb.SaveChanges();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }

            return Json(json);
        }
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public IActionResult TestHistory(int IdHistory)
        {
            if (IdHistory != 0)
            {
                TestHistory history = _educateDb.TestHistory.SingleOrDefault(h => h.HistoryId == IdHistory);
                if (history != null)
                {
                    ViewData["idHistory"] = IdHistory.ToString();
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
        [Authorize(Roles = "Admin")]
        public IActionResult HomeworkHistory(int IdHistory)
        {
            if (IdHistory != 0)
            {
                HomeworkHistory history = _educateDb.HomeworkHistory.SingleOrDefault(h => h.HistoryId == IdHistory);
                if (history != null)
                {
                    ViewData["idHistory"] = IdHistory.ToString();
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
                Students = _liveDb.Students.ToList(),
                Teachers = _liveDb.Teachers.ToList(),
                Users = _liveDb.Users.ToList(),
                HomeworkHistorys = _educateDb.HomeworkHistory.OrderByDescending(tsh => tsh.StartDate).ToList(),
                Questions = _educateDb.Question.ToList(),
                Answers = _educateDb.Answer.ToList(),
                Homeworks = _educateDb.Homework.ToList()
            };
            return View(tables);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult SearchTest(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                var test = (from t in _educateDb.Test
                            where t.TestName.Contains(value)
                            select t).ToList();
                return Json(JsonConvert.SerializeObject(test));
            }
            else
            {
                var test = (from t in _educateDb.Test
                            select t).ToList();
                return Json(JsonConvert.SerializeObject(test));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult SearchTestHistory(string value)
        {
            //Get all data
            List<TestHistoryViewModel> obj = new List<TestHistoryViewModel>();
            var query = (from th in _educateDb.TestHistory
                         join t in _educateDb.Test on th.TestId equals t.TestId
                         select new { t.TestName, th.HistoryId, th.StudentId, t.Time, th.Total, th.StartDate, th.EndDate }).ToList();
            foreach (var v in query)
            {
                string studentName = (from s in _liveDb.Students
                                      where s.Id == v.StudentId
                                      select s.Name).First().ToString();

                DateTime? end = v.EndDate;
                DateTime? start = v.StartDate;
                double diff = 0;
                if (end != null && start != null)
                {
                    DateTime startdate = start.Value;
                    DateTime enddate = end.Value;
                    diff = enddate.Subtract(startdate).TotalMinutes;
                }
                else
                {
                    diff = -1;
                }
                TestHistoryViewModel temp = new TestHistoryViewModel()
                {
                    TestName = v.TestName,
                    HistoryId = v.HistoryId,
                    Name = studentName,
                    Time = v.Time,
                    Total = v.Total,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    TotalStudentTime = diff
                };

                obj.Add(temp);
            };

            if (!String.IsNullOrEmpty(value))
            {
                obj.RemoveAll(th => !th.Name.Contains(value) && !th.TestName.Contains(value));
            }

            return Json(JsonConvert.SerializeObject(obj));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult HistoryDetail(long historyId)
        {
            var query = (from h in _educateDb.TestHistory
                         where h.HistoryId == historyId
                         select h).SingleOrDefault();

            //Get student name 
            string studentName = (from s in _liveDb.Students
                                  where s.Id == query.StudentId
                                  select s.Name).First().ToString();
            string testName = (from t in _educateDb.Test
                               where t.TestId == query.TestId
                               select t.TestName).First().ToString();
            int testId = query.TestId == null ? 0 : query.TestId.Value;
            double total = query.Total == null ? 0 : query.Total.Value;

            HistoryDetailViewModel obj = new HistoryDetailViewModel()
            {
                HistoryId = historyId,
                TestId = testId,
                TestName = testName,
                StudentName = studentName,
                StudentResult = query.StudentResult,
                Total = total,
                StartDate = query.StartDate,
                EndDate = query.EndDate
            };
            return View(obj);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateTestHistoryPoint(string historyId, int totalPoint)
        {
            long hId = long.Parse(historyId);
            var query = (from h in _educateDb.TestHistory
                         where h.HistoryId == hId
                         select h).SingleOrDefault();
            query.Total = totalPoint;

            _educateDb.SaveChanges();
            return Json(query);
        }

        [Authorize(Roles = "Admin")]
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


        [Authorize(Roles = "Admin")]
        [Route("[controller]/homework/{id}")]
        [Route("[controller]/bai-tap/homework/{id}")]
        public IActionResult HomeworkDetail(long id)
        {
            if (id != 0)
            {
                var query = _educateDb.Homework.SingleOrDefault(hw => hw.HomeworkId == id);
                return View(query);
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
            //Remove from homework history
            var homeworkHistories = (from h in _educateDb.HomeworkHistory where h.HomeworkId == homeworkId select h).ToList();
            foreach(var history in homeworkHistories)
            {
                history.HomeworkId = null;
            }
            //Remove from homework
            _educateDb.Homework.Remove(homework);

            _educateDb.SaveChanges();

            return Json(query);
        }

    }
}