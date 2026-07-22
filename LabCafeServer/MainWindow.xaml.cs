using System.Windows;

namespace LabCafeServer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            // MainContentFrame.Content = new DashboardView();
        }

        private void BtnUserManagement_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new UserManagementView();
        }

        private void BtnFileManagement_Click(object sender, RoutedEventArgs e)
        {
            // MainContentFrame.Content = new FileManagementView();
        }

        private void BtnRestrictions_Click(object sender, RoutedEventArgs e)
        {
            // MainContentFrame.Content = new RestrictionsView();
        }
    }
}