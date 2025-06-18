using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SerialConverter.Model;

namespace SerialConverter
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            MenuModel? model = param as MenuModel;
            if (model is not null && model.View is not null) return model.View.GetView();
            else return null;
        }
        public bool Match(object? data)
        {
            return data is MenuModel;
        }
    }
}
