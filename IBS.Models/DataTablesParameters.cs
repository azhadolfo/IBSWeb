namespace IBS.Models
{
    public class DataTablesParameters
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public List<DataTablesColumn> Columns { get; set; }
        public List<DataTablesOrder>? Order { get; set; }
        public DataTablesSearch Search { get; set; }
    }

    public class DataTablesColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTablesSearch Search { get; set; }
    }

    public class DataTablesOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }

    public class DataTablesSearch
    {
        private string _value;
    
        public string Value
        {
            get => _value;
            set => _value = value?.Trim().ToLower() ?? string.Empty;
        }
        public bool Regex { get; set; }
    }
}
