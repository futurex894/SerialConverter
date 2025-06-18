using Avalonia.Controls;
using SerialConverter.Core;
namespace SerialConverter.Plugins.VirtualSerial
{
    public class ModuleInfo : IView
    {
        public Control GetView()
        {
            return MainUI.Instance;
        }
        public MetadataExtensionAttribute GetMetaData()
        {
            return new MetadataExtensionAttribute() { Guid = "9C9E94C9-EB8C-6E42-19F0-0BBCBC0602DC", Name = "虚拟串口", LongName = "虚拟串口", Image = "HdmiPort" };
        }
    }
}
