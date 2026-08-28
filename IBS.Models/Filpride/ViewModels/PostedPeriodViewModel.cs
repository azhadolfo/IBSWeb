namespace IBS.Models.Filpride.ViewModels
{
    public class PostedPeriodViewModel
    {
        public List<ModuleSelectItem> AvailableModules { get; set; } = [];
        public List<PostedPeriod> PostedPeriods { get; set; } = [];
        public PostPeriodRequest PostRequest { get; set; } = new();
    }

    public class ModuleSelectItem
    {
        public string Value { get; set; } = null!;
        public string Text { get; set; } = null!;
    }

    public class PostPeriodRequest
    {
        public List<string> SelectedModules { get; set; } = [];
        public int Month { get; set; } = DateTime.Now.Month;
        public int Year { get; set; } = DateTime.Now.Year;
    }
}
