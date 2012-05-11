using System;
using System.Linq;
using System.Text;

namespace NPoco.Tests
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal Savings { get; set; }
    }
}
