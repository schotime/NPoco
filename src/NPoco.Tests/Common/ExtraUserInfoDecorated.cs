namespace NPoco.Tests.Common
{
    [TableName("ExtraUserInfos")]
    [PrimaryKey("ExtraUserInfoId")]
    [ExplicitColumns]
    public class ExtraUserInfoDecorated
    {
        [Column("ExtraUserInfoId")]
        public int ExtraUserInfoId { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        [Column("Children")]
        public int Children { get; set; }
    }
}
