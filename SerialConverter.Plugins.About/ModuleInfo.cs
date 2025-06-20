using Avalonia.Controls;
using SerialConverter.Core;
namespace SerialConverter.Plugins.About
{
    public class ModuleInfo : IView
    {
        public Control GetView()
        {
            return MainUI.Instance;
        }
        public MetadataExtensionAttribute GetMetaData()
        {
            return new MetadataExtensionAttribute() { Guid = "4DC27B0C-10BD-36B4-A8E4-86F41C3DAD1F", Name = "关于", LongName = "关于", Image = "InformationOutline" };
        }
    }
}
