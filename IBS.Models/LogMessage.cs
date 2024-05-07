﻿using System.ComponentModel.DataAnnotations;

namespace IBS.Models
{
    public class LogMessage
    {
        [Key]
        public Guid LogId { get; set; }

        //Time of log
        public DateTime TimeStamp { get; set; }

        //Severity level: Information, Warning, Error and etc.
        public string LogLevel { get; set; }

        //Name where the log comes
        public string LoggerName { get; set; }

        //Log description
        public string Message { get; set; }


        public LogMessage(string logLevel, string loggerName, string message)
        {
            TimeStamp = DateTime.Now;
            LogLevel = logLevel;
            LoggerName = loggerName;
            Message = message;
        }

    }
}
