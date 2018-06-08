using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Globalization;
using System.Diagnostics;
using Digi21.OpenGis.CoordinateSystems;
using Digi21.OpenGis.CoordinateTransformations;
using Digi21.OpenGis.Epsg;

namespace TransformadorUniversalCoordenadas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Campos privados
        private readonly CoordinateSystemFactory fábricaSrc = new CoordinateSystemFactory();
        private readonly CoordinateTransformationFactory fábricaTransformaciones = new CoordinateTransformationFactory();

        private CoordinateSystem origen;
        private CoordinateSystem Origen
        {
            get => origen;
            set
            {
                origen = value;
                NombreSrcOrigen = value.Name;
            }
        }

        private CoordinateSystem destino;
        private CoordinateSystem Destino
        {
            get => destino;
            set
            {
                destino = value;
                NombreSrcDestino = value.Name;
            }
        }
        private ICoordinateTransformation transformación;
        #endregion

        #region Propiedades de dependencia
        public string NombreSrcOrigen
        {
            get => (string)GetValue(NombreSrcOrigenProperty);
            set => SetValue(NombreSrcOrigenProperty, value);
        }

        // Using a DependencyProperty as the backing store for NombreSrcOrigen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NombreSrcOrigenProperty =
            DependencyProperty.Register("NombreSrcOrigen", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        public string NombreSrcDestino
        {
            get => (string)GetValue(NombreSrcDestinoProperty);
            set => SetValue(NombreSrcDestinoProperty, value);
        }

        // Using a DependencyProperty as the backing store for NombreSrcDestino.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NombreSrcDestinoProperty =
            DependencyProperty.Register("NombreSrcDestino", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        public string CoordenadasDestino
        {
            get => (string)GetValue(CoordenadasDestinoProperty);
            set => SetValue(CoordenadasDestinoProperty, value);
        }

        // Using a DependencyProperty as the backing store for CoordenadasDestino.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordenadasDestinoProperty =
            DependencyProperty.Register("CoordenadasDestino", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            try
            {
                Origen = fábricaSrc.CreateFromWkt(@"GEOGCS[""WGS 84"",DATUM[""World Geodetic System 1984"",SPHEROID[""WGS 84"",6378137,298.257223563,AUTHORITY[""EPSG"",""7030""]],AUTHORITY[""EPSG"",""6326""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degrees"",0.01745329251994328,AUTHORITY[""EPSG"",""9122""]],AXIS[""Lat"",North],AXIS[""Long"",East],AXIS[""h"",Up],AUTHORITY[""EPSG"",""4979""]]");
                Destino = Origen;
                AsignaTransformación(Origen, Destino);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        #endregion

        #region Manejadores de eventos
        private void BotonCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BotonLocalizarSrcOrigen_Click(object sender, RoutedEventArgs e)
        {
            var wktOrigen = EpsgManager.DialogSelectCrs("Selecciona el sistema de referencia de coordenadas o", Origen);

            var nuevoOrigen = fábricaSrc.CreateFromWkt(wktOrigen);

            if (!AsignaTransformación(nuevoOrigen, Destino)) return;
            Origen = nuevoOrigen;
            TransformaPuntos();
        }

        private void BotonLocalizarSrcDestino_Click(object sender, RoutedEventArgs e)
        {
            var wktDestino = EpsgManager.DialogSelectCrs("Selecciona el sistema de referencia de coordenadas d", Destino);

            var nuevoDestino = fábricaSrc.CreateFromWkt(wktDestino);

            if (!AsignaTransformación(Origen, nuevoDestino)) return;
            Destino = nuevoDestino;
            TransformaPuntos();
        }

        private void coordenadasOrigen_TextChanged(object sender, TextChangedEventArgs e)
        {
            TransformaPuntos();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

        #region Implementación
        private bool AsignaTransformación(CoordinateSystem o, CoordinateSystem d)
        {
            try
            {
                transformación = fábricaTransformaciones.CreateFromCoordinateSystems(
                    o,
                    d,
                    SelectTransformationHelper.DialogSelectTransformation,
                    CreateVerticalTransformationHelper.DialogCreateVerticalTransformation);
                return true;
            }
            catch (Exception e)
            {
                var dlg = new MostrarExcepción
                {
                    Origen = o.Name,
                    Destino = d.Name,
                    Mensaje = e.Message
                };

                dlg.ShowDialog();
                return false;
            }
        }

        private void TransformaPuntos()
        {
            var resultado = string.Empty;

            try
            {
                var líneas = CoordenadasOrigen.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var línea in líneas)
                {
                    var columnas = línea.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (columnas.Length != 3)
                        throw new Exception("La línea: \"" + línea + "\" no tiene tres columnas");

                    var coordenadas = new double[3];
                    coordenadas[0] = double.Parse(columnas[0], CultureInfo.InvariantCulture);
                    coordenadas[1] = double.Parse(columnas[1], CultureInfo.InvariantCulture);
                    coordenadas[2] = double.Parse(columnas[2], CultureInfo.InvariantCulture);

                    var transformado = transformación.MathTransform.Transform(coordenadas);
                    resultado += string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} {1} {2}\r\n",
                        transformado[0],
                        transformado[1],
                        transformado[2]);
                }
            }
            catch
            {
                // ignored
            }

            CoordenadasDestino = resultado;
        }
        #endregion
    }
}
