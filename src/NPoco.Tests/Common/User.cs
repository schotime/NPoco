using System;

namespace NPoco.Tests.Common
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal Savings { get; set; }
        public bool IsMale { get; set; }
    }

    public class Admin : User
    {
    }

    public class Supervisor : User
    {
        public bool IsSupervisor { get; set; }
    }

    public class UserWithExtraInfo : User
    {
        public ExtraUserInfo ExtraUserInfo { get; set; }
    }
}
