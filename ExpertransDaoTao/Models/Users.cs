using System;
using System.Collections.Generic;

namespace ExpertransDaoTao.Models
{
    public partial class Users
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
        public DateTime? BeginDate { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public long? GroupUserId { get; set; }
        public string Permission { get; set; }
        public string Note { get; set; }
        public bool? IsLock { get; set; }
        public bool? IsPublic { get; set; }
        public bool? IsDelete { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
