using System;

namespace NPoco.Tests.Common
{
    public class User
    {
        public User()
        {
            DateOfBirth = new DateTime(1900, 1, 1);
        }

        public int UserId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal Savings { get; set; }
        public bool IsMale { get; set; }
        public Guid? UniqueId { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public int? HouseId { get; set; }
        public int? SupervisorId { get; set; }
        public char? YorN { get; set; }
        public TestEnum TestEnum { get; set; }

        [ResultColumn]
        public House House { get; set; }

        [ResultColumn]
        public Supervisor Supervisor { get; set; }
    }

    public class House
    {
        public int HouseId { get; set; }
        public string Address { get; set; }
    }

    public enum TestEnum
    {
        All,
        None
    }

    public class Admin : User
    {
    }

    public class Supervisor : User
    {
        [ResultColumn]
        public bool IsSupervisor { get; set; }
    }

    public class UserWithExtraInfo : User
    {
        public ExtraUserInfo ExtraUserInfo { get; set; }
    }

    public class UserWithNoParamConstructor : User
    {
        public UserWithNoParamConstructor(int userId)
        {
            UserId = userId;
        }
    }

    public class UserWithPrivateParamLessConstructor : User
    {
        private UserWithPrivateParamLessConstructor()
        {
        }

        public UserWithPrivateParamLessConstructor(int userId)
        {
            UserId = userId;
        }
    }
}
