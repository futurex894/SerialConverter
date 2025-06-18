using Avalonia.Controls;
using System.ComponentModel;

namespace SerialConverter.Core
{
    [Description("视图接口")]
    public interface IView
    {
        public Control GetView();
        public MetadataExtensionAttribute GetMetaData();
    }
}
