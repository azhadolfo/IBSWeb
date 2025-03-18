namespace IBS.Dtos
{
    public class LogMessageDto
    {
        public string StationName { get; set; }

        public string Message { get; set; }

        public string CsvStatus { get; set; }

        public string OpeningFileStatus { get; set; }

        public string HowManyImported { get; set; }

        public string Error { get; set; }
    }
}
