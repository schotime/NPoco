using System;
using System.Collections.Generic;

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

    [TableName("TEST_Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecoratedWithExtraInfoAsList : UserDecorated
    {
        public List<ExtraUserInfoDecorated> ExtraUserInfo { get; set; }
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class UserFieldDecorated
    {
        [Column("UserId")] private int UserId;

        [Column("Name")] public string Name;

        [Column("Age")] public int Age;

        [Column("DateOfBirth")] public DateTime DateOfBirth;

        [Column("Savings")] public decimal Savings;

        [Column("is_male")] public bool IsMale;
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserReadOnlyFieldDecorated
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Name")] public readonly string Name;
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class UserWithNullableId
    {
        public long? UserId { get; set; }
        public UserIdEnum? UserId2 { get; set; }
        public NameEnum? NameEnum { get; set; }
        public Days Days { get; set; }
    }

    public enum UserIdEnum
    {
        UserIdFalse = 0,
        UserIdTrue = 1
    }

    public enum NameEnum
    {
        Bobby,
        Bill
    }

    public enum Days : byte { Sat, Sun, Mon, Tue, Wed, Thu, Fri };
}
