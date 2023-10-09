using ExpertransDaoTao.Models;
using ExpertransDaoTao.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewComponents
{
    public class ShowCourses : ViewComponent
    {
        private readonly expertrans_educateContext _db;

        public ShowCourses(expertrans_educateContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var tables = new CourseViewModel
            {
                Courses = _db.Course.ToList(),
                CoursesLevel2 = _db.CourseLevel2.ToList()
            };
            await Task.Delay(1);
            return View("Default", tables);
        }
    }
}
