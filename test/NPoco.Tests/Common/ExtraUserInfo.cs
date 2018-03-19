namespace NPoco.Tests.Common
{
    [TableName("ExtraUserInfos")]
    public class ExtraUserInfo
    {
        public int ExtraUserInfoId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public int Children { get; set; }
    }
}