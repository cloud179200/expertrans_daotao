using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExpertransDaoTao.Models
{
    public class User
    {
        public int Id { set; get; }
        public string UserName { set; get; }
        public string Name { set; get; }
        public string Email { set; get; }
        public string Mobile { set; get; }
        public string Address { set; get; }
        public string Role { set; get; }
    }
}
