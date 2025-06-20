using Avalonia.Controls;

namespace SerialConverter.Plugins.About;

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
    public MainUI()
    {
        InitializeComponent();
        this.DataContext = new MainUIViewModel();
    }

}