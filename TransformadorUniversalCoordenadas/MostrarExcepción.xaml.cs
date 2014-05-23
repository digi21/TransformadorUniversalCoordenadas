using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TransformadorUniversalCoordenadas
{
    /// <summary>
    /// Interaction logic for MostrarExcepción.xaml
    /// </summary>
    public partial class MostrarExcepción : Window
    {
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Mensaje { get; set; }

        public MostrarExcepción()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
