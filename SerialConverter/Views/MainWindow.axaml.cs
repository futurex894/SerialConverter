using SerialConverter.Core;
using SerialConverter.Model;
using SerialConverter.ViewModels;
using SukiUI.Controls;
using System;
using System.Collections.Generic;

namespace SerialConverter.Views
{
    public partial class MainWindow : SukiWindow
    {
        private MainWindowViewModel vm { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            SukiToastHosts.Manager = ToastHost.Manager;
            SukiDialogHosts.Manager = DialogHost.Manager;
            vm = new MainWindowViewModel();
            this.DataContext = vm;
            Object[] Views = new Object[]
            {
                new Plugins.VirtualSerial.ModuleInfo(),
                new Plugins.About.ModuleInfo(),
            };
            CreateViewList(Views);
        }
        private void CreateViewList(Object[] Views)
        {
            List<MenuModel> Menus = new List<MenuModel>();
            foreach (Object View in Views)
            {
                MetadataExtensionAttribute? attribute = ((IView)View).GetMetaData();
                if (attribute is not null)
                {
                    Menus.Add(new MenuModel() { Guid = attribute.Guid, Name = attribute.Name, Image = attribute.Image, View = (IView)View });
                }
            }
            foreach (MenuModel Menu in Menus) { vm.MenuQueue?.Add(Menu); }
        }
    }
}