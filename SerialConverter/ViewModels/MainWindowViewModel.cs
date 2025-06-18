using SerialConverter.Model;
using System.Collections.ObjectModel;

namespace SerialConverter.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<MenuModel>? MenuQueue { get; set; }
        public MainWindowViewModel()
        {
            MenuQueue = new ObservableCollection<MenuModel>();
        }
    }
}
