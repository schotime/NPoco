using System;
using System.Collections.Generic;
using NPoco;

namespace NPoco.Tests.Common
{
    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecorated
    {
        public UserDecorated()
        {
            
        }

        public UserDecorated(int userId)
        {
            UserId = userId;
        }

        [Column("UserId")]
        public int UserId { get; private set; }

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

        [Column("HouseId")]
        public int? HouseId { get; set; }
    }

    [TableName("Houses"), PrimaryKey("HouseId")]
    public class HouseDecorated
    {
        public int HouseId { get; set; }
        public string Address { get; set; }
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserIntVersionDecorated : UserDecorated
    {
        [VersionColumn(VersionColumnType.Number)]
        public long VersionInt { get; set; }
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserTimestampVersionDecorated : UserDecorated
    {
        [VersionColumn("Version", VersionColumnType.RowVersion)]
        public byte[] Version { get; set; }
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
        [ResultColumn]
        [Reference(ReferenceType.OneToOne)]
        public ExtraUserInfoDecorated ExtraUserInfo { get; set; }
    }

    [TableName("TEST_Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecoratedWithExtraInfoAsList : UserDecorated
    {
        public UserDecoratedWithExtraInfoAsList()
        {
            ExtraUserInfo = new List<ExtraUserInfoDecorated>();
        }

        [ResultColumn, Reference(ReferenceType.Many)]
        public List<ExtraUserInfoDecorated> ExtraUserInfo { get; set; }
    }

    [TableName("Users")]
    [PrimaryKey("UserId")]
    public class UserFieldDecorated
    {
#pragma warning disable 169
        [Column("UserId")] private int UserId;
#pragma warning restore 169

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
        [ColumnType(typeof(string))]
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

    [TableName("Users")]
    [PrimaryKey("UserId")]
    [ExplicitColumns]
    public class UserDecoratedWithNullable
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("Age")]
        public int? Age { get; set; }
    }

     [TableName("Users")]
     [PrimaryKey("UserId")]
     [ExplicitColumns]
     public class UserDecoratedWithAlias
     {
         [Column("UserId")]
         public int UserId { get; set; }
 
         [Column("Name")]
         [Alias("FullName")]
         public string Name { get; set; }
 
         [Column("Age")]
         public int? Age { get; set; }
     }
}
