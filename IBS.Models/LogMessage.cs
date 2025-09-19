using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models
{
    public class LogMessage
    {
        [Key]
        public Guid LogId { get; set; }

        //Time of log
        [Column(TypeName = "timestamp without time zone")]
        public DateTime TimeStamp { get; set; }

        //Severity level: Information, Warning, Error and etc.
        public string LogLevel { get; set; }

        //Name where the log comes
        public string LoggerName { get; set; }

        //Log description
        public string Message { get; set; }


        public LogMessage(string logLevel, string loggerName, string message)
        {
            TimeStamp = DateTimeHelper.GetCurrentPhilippineTime();
            LogLevel = logLevel;
            LoggerName = loggerName;
            Message = message;
        }

    }
}
