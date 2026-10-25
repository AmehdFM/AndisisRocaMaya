using System.Windows;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            var user = SettingsViewModel.Instance.FindActiveUser(UsernameBox.Text);
            if (user?.Rol is null)
            {
                MessageBox.Show(this, "Escribe un usuario activo: Administrador o Empleado.",
                    "Usuario no encontrado", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!SettingsViewModel.Instance.VerifyDemoPassword(user, PasswordInput.Password))
            {
                MessageBox.Show(this, "La contraseña no es válida para ese usuario.",
                    "Credenciales incorrectas", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordInput.Clear();
                return;
            }

            SettingsViewModel.Instance.SetCurrentUser(user);
            new ShellWindow(user).Show();
            Close();
        }
    }
}
