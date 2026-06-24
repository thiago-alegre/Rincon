namespace Rincon.Models.ViewModels
{
    public class ScreenHelpVM
    {
        public string Title { get; set; } = "Ayuda de pantalla";

        public string Description { get; set; } = string.Empty;

        public IEnumerable<string> Items { get; set; } = new List<string>();
    }
}
