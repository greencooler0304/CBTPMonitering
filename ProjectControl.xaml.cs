using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CBTPTempMoistMoni.Control
{
    /// <summary>
    /// ProjectControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProjectControl : UserControl
    {
        public ProjectControl()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as Project)?.start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as Project)?.stop();
        }
    }

    public class Project : BaseModel
    {

    }
}
