using System;

namespace NPoco.Tests.Common
{
    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecorated
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Age")]
        public int Age { get; set; }

        [Column("DateOfBirth")]
        public DateTime DateOfBirth { get; set; }

        [Column("Savings")]
        public decimal Savings { get; set; }

        [Column("is_male")]
        public bool IsMale { get; set; }
    }

    [TableName("TEST_Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class AdminDecorated : UserDecorated
    {
    }

    [TableName("TEST_Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecoratedWithExtraInfo : UserDecorated
    {
        public ExtraUserInfoDecorated ExtraUserInfo { get; set; }
    }
}
