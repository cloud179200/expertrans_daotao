using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Teachers
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public long? UserId { get; set; }
        public string Identity { get; set; }
        public string IdentityImg { get; set; }
        public string IdentityImg2 { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public DateTime? BeginDate { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsDelete { get; set; }
        public int? TeacherType { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
