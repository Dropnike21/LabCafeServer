using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace LabCafeServer
{
    public partial class UserManagementView : UserControl
    {
        public ObservableCollection<StudentEntry> BatchStudents { get; set; }

        // This dynamically points to wherever you set the Master Workspace
        private string GetDatabasePath() => Path.Combine(TxtWorkspacePath.Text.Trim(), "StudentDatabase.json");

        public UserManagementView()
        {
            InitializeComponent();
            BatchStudents = new ObservableCollection<StudentEntry>();
            GridBatchStudents.ItemsSource = BatchStudents;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select the Master Workspace Directory" };
            if (dialog.ShowDialog() == true)
            {
                TxtWorkspacePath.Text = dialog.FolderName;
            }
        }

        // --- BATCH PROVISION: WRITE TO JSON ---
        private void BtnProvisionBatch_Click(object sender, RoutedEventArgs e)
        {
            string section = TxtSection.Text.Trim();
            string masterWorkspace = TxtWorkspacePath.Text.Trim();

            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(masterWorkspace))
            {
                TxtBatchStatus.Text = "WARNING: Section and Workspace Path are required.";
                return;
            }

            if (!Directory.Exists(masterWorkspace)) Directory.CreateDirectory(masterWorkspace);

            string dbFilePath = GetDatabasePath();
            List<StudentEntry> masterDatabase = new List<StudentEntry>();

            // Load existing database if it exists
            if (File.Exists(dbFilePath))
            {
                string existingJson = File.ReadAllText(dbFilePath);
                masterDatabase = JsonSerializer.Deserialize<List<StudentEntry>>(existingJson) ?? new List<StudentEntry>();
            }

            int successCount = 0;
            int errorCount = 0;

            foreach (var student in BatchStudents)
            {
                if (string.IsNullOrEmpty(student.Username)) continue;

                // Prevent duplicates
                if (masterDatabase.Any(s => s.Username == student.Username))
                {
                    errorCount++;
                    continue;
                }

                // Add to JSON Database
                student.Section = section; // Save the section
                masterDatabase.Add(student);

                // Create their physical workspace folder for the future Z: Drive
                string studentFolder = Path.Combine(masterWorkspace, student.Username);
                if (!Directory.Exists(studentFolder)) Directory.CreateDirectory(studentFolder);

                successCount++;
            }

            // Save the updated JSON file
            string updatedJson = JsonSerializer.Serialize(masterDatabase, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dbFilePath, updatedJson);

            TxtBatchStatus.Text = $"Database Updated: {successCount} Added, {errorCount} Skipped (Duplicates).";
            if (successCount > 0) BatchStudents.Clear();
        }

        // --- READ: LOAD FROM JSON ---
        private void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dbFilePath = GetDatabasePath();
                if (File.Exists(dbFilePath))
                {
                    string json = File.ReadAllText(dbFilePath);
                    var users = JsonSerializer.Deserialize<List<StudentEntry>>(json) ?? new List<StudentEntry>();
                    GridUsers.ItemsSource = users;
                    TxtDirectoryStatus.Text = $"Success: Loaded {users.Count} students from JSON.";
                }
                else
                {
                    TxtDirectoryStatus.Text = "Database not found. Provision students first.";
                }
            }
            catch (Exception ex)
            {
                TxtDirectoryStatus.Text = $"READ ERROR: {ex.Message}";
            }
        }

        // --- UPDATE: RESET PASSWORD IN JSON ---
        private void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (GridUsers.SelectedItem == null) return;
            var selectedStudent = (StudentEntry)GridUsers.SelectedItem;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Reset password for '{selectedStudent.Username}' to '@Mcs1234'?",
                "Confirm Password Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmation == MessageBoxResult.Yes)
            {
                string dbFilePath = GetDatabasePath();
                string json = File.ReadAllText(dbFilePath);
                var masterDatabase = JsonSerializer.Deserialize<List<StudentEntry>>(json);

                var userToModify = masterDatabase.FirstOrDefault(s => s.Username == selectedStudent.Username);
                if (userToModify != null)
                {
                    userToModify.Password = "@Mcs1234";
                    File.WriteAllText(dbFilePath, JsonSerializer.Serialize(masterDatabase, new JsonSerializerOptions { WriteIndented = true }));
                    TxtDirectoryStatus.Text = $"Success: '{selectedStudent.Username}' password reset.";
                    BtnRefreshUsers_Click(null, null); // Refresh grid
                }
            }
        }

        // --- DELETE: REMOVE FROM JSON ---
        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (GridUsers.SelectedItem == null) return;
            var selectedStudent = (StudentEntry)GridUsers.SelectedItem;

            MessageBoxResult confirmation = MessageBox.Show(
                $"Delete student '{selectedStudent.Username}' from the database?\n(Their files will remain on the hard drive)",
                "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmation == MessageBoxResult.Yes)
            {
                string dbFilePath = GetDatabasePath();
                string json = File.ReadAllText(dbFilePath);
                var masterDatabase = JsonSerializer.Deserialize<List<StudentEntry>>(json);

                masterDatabase.RemoveAll(s => s.Username == selectedStudent.Username);
                File.WriteAllText(dbFilePath, JsonSerializer.Serialize(masterDatabase, new JsonSerializerOptions { WriteIndented = true }));

                TxtDirectoryStatus.Text = $"Success: '{selectedStudent.Username}' purged from database.";
                BtnRefreshUsers_Click(null, null); // Refresh grid
            }
        }
    }

    // ====================================================================
    // DATA MODEL FOR THE JSON DATABASE
    // ====================================================================
    public class StudentEntry : INotifyPropertyChanged
    {
        private string _id = "";
        private string _lastName = "";
        private string _firstName = "";
        private string _username = "";

        public string ID
        {
            get => _id;
            set { _id = value; UpdateUsername(); NotifyPropertyChanged(nameof(ID)); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; UpdateUsername(); NotifyPropertyChanged(nameof(LastName)); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; UpdateUsername(); NotifyPropertyChanged(nameof(FirstName)); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; NotifyPropertyChanged(nameof(Username)); }
        }

        public string Section { get; set; } = "";

        // This setter is explicitly public now so we can modify it in the database
        public string Password { get; set; } = "@Mcs1234";

        private void UpdateUsername()
        {
            Username = $"{ID}{LastName}{FirstName}".Replace(" ", "").ToLower();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}