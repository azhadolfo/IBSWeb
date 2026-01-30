using IBS.Models.Enums;

namespace IBS.DTOs
{
    public class SubAccountInfoDto
    {
        public SubAccountType Type { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
