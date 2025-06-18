using Avalonia.Controls;

namespace SerialConverter.Plugins.VirtualSerial;

public partial class MainUI : UserControl
{
    private static MainUI? m_Instance;
    public static MainUI Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = new MainUI();
            return m_Instance;
        }
    }
    MainUIViewModel vm;
    public MainUI()
    {
        InitializeComponent();
        vm=new MainUIViewModel();
        this.DataContext = vm;
    }
}