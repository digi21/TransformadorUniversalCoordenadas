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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Digi21.OpenGis.CoordinateTransformations;
using System.Globalization;
using System.Diagnostics;
using Digi21.OpenGis.CoordinateSystems;
using Digi21.OpenGis.Epsg;

namespace TransformadorUniversalCoordenadas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Campos privados
        private CoordinateSystemFactory fábricaSrc = new CoordinateSystemFactory();
        private CoordinateTransformationFactory fábricaTransformaciones = new CoordinateTransformationFactory();

        CoordinateSystem _origen;
        private CoordinateSystem origen
        {
            get
            {
                return _origen;
            }
            set
            {
                _origen = value;
                NombreSrcOrigen = value.Name;
            }
        }

        CoordinateSystem _destino;
        private CoordinateSystem destino
        {
            get
            {
                return _destino;
            }
            set
            {
                _destino = value;
                NombreSrcDestino = value.Name;
            }
        }
        private ICoordinateTransformation transformación;
        #endregion

        #region Propiedades de dependencia
        public string NombreSrcOrigen
        {
            get { return (string)GetValue(NombreSrcOrigenProperty); }
            set { SetValue(NombreSrcOrigenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NombreSrcOrigen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NombreSrcOrigenProperty =
            DependencyProperty.Register("NombreSrcOrigen", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        public string NombreSrcDestino
        {
            get { return (string)GetValue(NombreSrcDestinoProperty); }
            set { SetValue(NombreSrcDestinoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NombreSrcDestino.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NombreSrcDestinoProperty =
            DependencyProperty.Register("NombreSrcDestino", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        public string CoordenadasDestino
        {
            get { return (string)GetValue(CoordenadasDestinoProperty); }
            set { SetValue(CoordenadasDestinoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CoordenadasDestino.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordenadasDestinoProperty =
            DependencyProperty.Register("CoordenadasDestino", typeof(string), typeof(MainWindow), new UIPropertyMetadata(""));

        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            origen = fábricaSrc.CreateFromWkt(@"COMPD_CS[""ETRS89 / UTM zone 30N + EGM08_REDNAP Península"", PROJCS[""ETRS89 / UTM zone 30N"", GEOGCS[""ETRS89"", DATUM[""European Terrestrial Reference System 1989"", SPHEROID[""GRS 1980"", 6378137, 298.257222101, AUTHORITY[""EPSG"", ""7019""]], AUTHORITY[""EPSG"", ""6258""]], PRIMEM[""Greenwich"", 0, AUTHORITY[""EPSG"", ""8901""]], UNIT[""°"", 0.01745329251994328, AUTHORITY[""EPSG"", ""9122""]], AXIS[""Lat"", North], AXIS[""Long"", East], AUTHORITY[""EPSG"", ""4258""]], PROJECTION[""Transverse_Mercator""], PARAMETER[""latitude_of_origin"", 0], PARAMETER[""central_meridian"", -2.999999999999997], PARAMETER[""scale_factor"", 0.9996], PARAMETER[""false_easting"", 500000], PARAMETER[""false_northing"", 0], PARAMETER[""semi_major"", 6378137], PARAMETER[""semi_minor"", 6356752.314140356], UNIT[""metros"", 1, AUTHORITY[""EPSG"", ""9001""]], AXIS[""E"", East], AXIS[""N"", North], AUTHORITY[""EPSG"", ""25830""]], VERT_CS[""EGM08_REDNAP Península"", VERT_DATUM[""EGM2008 geoid"", 2005, AUTHORITY[""EPSG"", ""1027""]], UNIT[""metros"", 1, AUTHORITY[""EPSG"", ""9001""]], AXIS[""H"", Up], AUTHORITY[""EPSG"", ""69036406""]]]");
            destino = origen;

            AsignaTransformación(origen, destino);
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
            var wktOrigen = EpsgManager.DialogSelectCrs("Selecciona el sistema de referencia de coordenadas origen", origen);

            var nuevoOrigen = fábricaSrc.CreateFromWkt(wktOrigen);

            if (AsignaTransformación(nuevoOrigen, destino))
            {
                origen = nuevoOrigen;
                TransformaPuntos();
            }
        }

        private void BotonLocalizarSrcDestino_Click(object sender, RoutedEventArgs e)
        {
            var wktDestino = EpsgManager.DialogSelectCrs("Selecciona el sistema de referencia de coordenadas destino", destino);

            var nuevoDestino = fábricaSrc.CreateFromWkt(wktDestino);

            if (AsignaTransformación(origen, nuevoDestino))
            {
                destino = nuevoDestino;
                TransformaPuntos();
            }
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
        private bool AsignaTransformación(CoordinateSystem origen, CoordinateSystem destino)
        {
            try
            {
                transformación = fábricaTransformaciones.CreateFromCoordinateSystems(
                    origen,
                    destino,
                    SelectTransformationHelper.DialogSelectTransformation,
                    CreateVerticalTransformationHelper.DialogCreateVerticalTransformation);
                return true;
            }
            catch (Exception e)
            {
                MostrarExcepción dlg = new MostrarExcepción
                {
                    Origen = origen.Name,
                    Destino = destino.Name,
                    Mensaje = e.Message
                };

                dlg.ShowDialog();
                return false;
            }
        }

        private void TransformaPuntos()
        {
            string resultado = string.Empty;

            try
            {
                var líneas = CoordenadasOrigen.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var línea in líneas)
                {
                    var columnas = línea.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
            catch (Exception)
            {

            }

            CoordenadasDestino = resultado;
        }
        #endregion
    }
}
