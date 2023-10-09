using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.ViewModel
{
    public class StudentCourseViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Identity { get; set; }
        public string IdentityImg { get; set; }
        public string IdentityImg2 { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public DateTime? BeginDate { get; set; }
        public string ContractNumber { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public long? LevelId { get; set; }
        public long? GroupStudentId { get; set; }
        public long? CollaboratorId { get; set; }
        public long? UserId { get; set; }
        public int? Payment { get; set; }
        public double? TotalPay { get; set; }
        public string SGuid { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsDelete { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long StudentId { get; set; }
        public long CourseId { get; set; }
        public string CourseName { get; set; }
    }
}
