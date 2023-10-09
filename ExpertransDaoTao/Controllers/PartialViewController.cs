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

namespace ExpertransDaoTao.Controllers
{
    public class PartialViewController : Controller
    {
        private readonly Models.expertrans_liveContext _liveDb;
        private readonly Models.expertrans_educateContext _educateDb;

        public PartialViewController(Models.expertrans_liveContext liveDb, Models.expertrans_educateContext educateDb)
        {
            _liveDb = liveDb;
            _educateDb = educateDb;
        }
        public PartialViewResult ListStudent()
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
                else
                {
                    courseName = "Không có dữ liệu";
                }

                obj.Id = student.Id;
                obj.UserName = student.UserName;
                obj.Name = student.Name;
                obj.CourseName = courseName;
                obj.Email = student.Email;
                obj.Mobile = student.Mobile;

                objList.Add(obj);
            }
            return PartialView("_ListStudent", objList);
        }

        public PartialViewResult ListTeacher()
        {
            var teachers = (from t in _liveDb.Teachers
                            select t).ToList();

            return PartialView("_ListTeacher", teachers);
        }
    }
}
