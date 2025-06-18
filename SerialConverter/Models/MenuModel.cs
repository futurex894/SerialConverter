using SerialConverter.Core;

namespace SerialConverter.Model
{
    public class MenuModel
    {
        public string? Guid {  get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }
        public IView? View { get; set; }
    }
}
