using BootstrapperUI.ViewModels;
using System.Windows;

namespace BootstrapperUI.Views
{
    /// <summary>
    /// Interaction logic for InstallView.xaml
    /// </summary>
    public partial class InstallView : Window
    {
        public InstallView(InstallViewModel installViewModel)
        {
            this.InitializeComponent();
            this.DataContext = installViewModel;

            //fired when the close button is clicked
            this.Closed += (sender, e) => installViewModel.CancelCommand.Execute(this);
        }
    }
}
