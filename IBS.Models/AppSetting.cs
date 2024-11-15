using System.ComponentModel.DataAnnotations;

namespace IBS.Models
{
    public class AppSetting
    {
        [Key]
        public string SettingKey { get; set; }

        public string Value { get; set; }
    }
}