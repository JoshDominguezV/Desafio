using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Threading.Tasks;

namespace GrafoElSalvador
{
    public partial class Form1 : MaterialForm
    {
        private GMapControl gMap;
        private GMapOverlay nodesOverlay;
        private GMapOverlay edgesOverlay;

        private Graph graph;
        private RouteService routeService;

        private MaterialComboBox cbOrigen, cbDestino;
        private MaterialButton btnCalcularRuta, btnVerMST;

        private MaterialButton btnRecorridoAnchura;
        private MaterialButton btnRecorridoProfundidad;


        private bool mostrandoMST = false;
        private bool rutasVisibles = false;

        public Form1()
        {
            

            // Configurar ventana
            Text = "Grafo de El Salvador";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Sizable = true; // Permite redimensionar la ventana (habilita el botón de maximizar)
            this.MaximizeBox = true; // Muestra botón maximizar
            this.MinimizeBox = true; // Muestra botón minimizar
            this.ControlBox = true;  // Muestra el botón de cerrar




            // Configurar MaterialSkin (tema oscuro)
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800, Primary.BlueGrey900,
                Primary.BlueGrey500, Accent.LightBlue200,
                TextShade.WHITE);

            InitializeMap();
            InitializeControls();
            CreateGraph();

            // Inicializar servicio de rutas 
            routeService = new RouteService("5b3ce3597851110001cf62487ffec6b526ef4d20934af4719c0f9834", "rutas.json");

            // Dibuja el grafo completo
            _ = DrawGraphAsync();
        }

        private void InitializeMap()
        {
            gMap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.GoogleMap,
                Position = new PointLatLng(13.7942, -88.8965),
                MinZoom = 7,
                MaxZoom = 18,
                Zoom = 10,
                DragButton = MouseButtons.Left,

            };

            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            nodesOverlay = new GMapOverlay("nodes");
            edgesOverlay = new GMapOverlay("edges");

            gMap.Overlays.Add(edgesOverlay);
            gMap.Overlays.Add(nodesOverlay);

            Controls.Add(gMap);
        }

        private void InitializeControls()
        {
            // Controles Material arriba del mapa, en la parte superior izquierda
            cbOrigen = new MaterialComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Left = 15,
                Top = 70 // debajo del título y barra Material
            };
            cbDestino = new MaterialComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Left = 210,
                Top = 70
            };

            btnCalcularRuta = new MaterialButton
            {
                Text = "Calcular Ruta Más Corta",
                Width = 200,
                Height = 40,
                Left = 410,
                Top = 65
            };
            btnCalcularRuta.Click += BtnCalcularRuta_Click;

            btnVerMST = new MaterialButton
            {
                Text = " Ver todas las rutas",
                Width = 190,
                Height = 40,
                Left = 950,
                Top = 65
            };
            btnVerMST.Click += BtnVerMST_Click;

            Controls.Add(cbOrigen);
            Controls.Add(cbDestino);
            Controls.Add(btnCalcularRuta);
            Controls.Add(btnVerMST);

            
            cbOrigen.BringToFront();
            cbDestino.BringToFront();
            btnCalcularRuta.BringToFront();
            btnVerMST.BringToFront();

            // Botón para Recorrido en Anchura (BFS)
            btnRecorridoAnchura = new MaterialButton
            {
                Text = "Recorrido Anchura",
                Width = 160,
                Height = 40,
                Left = 620,
                Top = 65
            };
            btnRecorridoAnchura.Click += BtnRecorridoAnchura_Click;

            // Botón para Recorrido en Profundidad (DFS)
            btnRecorridoProfundidad = new MaterialButton
            {
                Text = "Recorrido Profundidad",
                Width = 170,
                Height = 40,
                Left = 780,
                Top = 65
            };
            btnRecorridoProfundidad.Click += BtnRecorridoProfundidad_Click;

            // Agregar los botones al formulario
            Controls.Add(btnRecorridoAnchura);
            Controls.Add(btnRecorridoProfundidad);

            // Asegurarte de que estén al frente
            btnRecorridoAnchura.BringToFront();
            btnRecorridoProfundidad.BringToFront();

        }

        private void CreateGraph()
        {
            graph = new Graph();

            var cities = new List<CityNode>
            {
                new CityNode("San Salvador", 13.6929, -89.2182),
                new CityNode("Santa Ana", 13.9946, -89.5597),
                new CityNode("San Miguel", 13.4833, -88.1833),
                new CityNode("La Libertad", 13.6769, -89.2797),
                new CityNode("Sonsonate", 13.7186, -89.7245),
                new CityNode("Ahuachapán", 13.9214, -89.8450),
                new CityNode("Chalatenango", 14.0333, -88.9333),
                new CityNode("Cuscatlán", 13.876184, -88.628492),
                new CityNode("La Paz", 13.507316, -88.870206),
                new CityNode("Cabañas", 13.721740, -88.934541),
                new CityNode("San Vicente", 13.6333, -88.7833),
                new CityNode("Usulután", 13.3500, -88.4500),
                new CityNode("Morazán", 13.695215, -88.106073),
                new CityNode("La Unión", 13.5000, -87.8833)
            };

            foreach (var city in cities)
                graph.AddNode(city);

            // Crear aristas completas entre ciudades (podrías cambiar para conectar solo las cercanas)
            for (int i = 0; i < cities.Count; i++)
                for (int j = i + 1; j < cities.Count; j++)
                    graph.AddEdge(cities[i], cities[j]);

            cbOrigen.Items.AddRange(cities.Select(c => c.Name).ToArray());
            cbDestino.Items.AddRange(cities.Select(c => c.Name).ToArray());
        }

        private async Task DrawGraphAsync()
        {
            nodesOverlay.Markers.Clear();
            edgesOverlay.Routes.Clear();

            // Añadir marcadores
            foreach (var node in graph.Nodes)
            {
                var marker = new GMarkerGoogle(new PointLatLng(node.Latitude, node.Longitude), GMarkerGoogleType.red_dot)
                {
                    ToolTipText = node.Name
                };
                nodesOverlay.Markers.Add(marker);
            }

            // Añadir todas las rutas 
            if (!mostrandoMST)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    for (int j = i + 1; j < graph.Nodes.Count; j++)
                    {
                        var from = graph.Nodes[i];
                        var to = graph.Nodes[j];

                        var points = await routeService.GetRouteAsync(from, to);
                        if (points != null)
                        {
                            var route = new GMapRoute(points, $"{from.Name}-{to.Name}")
                            {
                                Stroke = new Pen(Color.LightBlue, 1)
                            };
                            edgesOverlay.Routes.Add(route);
                        }
                    }
                }
            }

            gMap.Refresh();
        }

        private async void BtnCalcularRuta_Click(object sender, EventArgs e)
        {
            mostrandoMST = false;  

            if (cbOrigen.SelectedItem == null || cbDestino.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un origen y un destino.");
                return;
            }

            var origen = graph.GetNodeByName(cbOrigen.SelectedItem.ToString());
            var destino = graph.GetNodeByName(cbDestino.SelectedItem.ToString());

            if (origen == null || destino == null)
            {
                MessageBox.Show("No se encontraron los nodos seleccionados.");
                return;
            }

            edgesOverlay.Routes.Clear();

            var path = graph.GetShortestPath(origen, destino);
            if (path == null || path.Count < 2)
            {
                MessageBox.Show("No se encontró una ruta.");
                return;
            }

            double totalDistancia = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];

                var points = await routeService.GetRouteAsync(from, to);
                if (points != null)
                {
                    var route = new GMapRoute(points, $"{from.Name}-{to.Name}")
                    {
                        Stroke = new Pen(Color.Red, 3)
                    };
                    edgesOverlay.Routes.Add(route);

                    totalDistancia += routeService.GetDistance(from, to);
                }
            }

            gMap.Refresh();
            MessageBox.Show($"Ruta más corta calculada.\nDistancia total: {totalDistancia:F2} km.", "Ruta por Escalas", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private async Task PintarRecorridoAsync(List<CityNode> recorrido)
        {
            if (recorrido == null || recorrido.Count < 2)
                return;

            // Quitar solo overlay anterior de recorridos, sin borrar los nodos ni rutas generales
            var recorridoOverlay = gMap.Overlays.FirstOrDefault(o => o.Id == "rutas_recorrido");
            if (recorridoOverlay != null)
                gMap.Overlays.Remove(recorridoOverlay);

            var routeOverlay = new GMapOverlay("rutas_recorrido");
            gMap.Overlays.Add(routeOverlay);

            for (int i = 0; i < recorrido.Count - 1; i++)
            {
                var from = recorrido[i];
                var to = recorrido[i + 1];

                var puntosRuta = await routeService.GetRouteAsync(from, to);

                if (puntosRuta != null)
                {
                    var ruta = new GMapRoute(puntosRuta, $"{from.Name}-{to.Name}")
                    {
                        Stroke = new Pen(Color.OrangeRed, 3)
                    };
                    routeOverlay.Routes.Add(ruta);

                    gMap.Refresh();
                    await Task.Delay(500); // Delay para animación
                }
            }
        }



        private CityNode ObtenerNodoSeleccionado()
        {
            if (cbOrigen.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un nodo de inicio en el ComboBox de Origen.");
                return null;
            }

            var nombre = cbOrigen.SelectedItem.ToString();
            return graph.GetNodeByName(nombre);
        }


        private async void BtnRecorridoAnchura_Click(object sender, EventArgs e)
        {
            var nodoInicial = ObtenerNodoSeleccionado();
            if (nodoInicial == null) return;

            var recorrido = graph.BFS(nodoInicial);
            await PintarRecorridoAsync(recorrido);

        }

        private async void BtnRecorridoProfundidad_Click(object sender, EventArgs e)
        {
            var nodoInicial = ObtenerNodoSeleccionado();
            if (nodoInicial == null) return;

            var recorrido = graph.DFS(nodoInicial);
            await PintarRecorridoAsync(recorrido);

        }



        private void MostrarRecorrido(List<string> recorrido, string titulo)
        {
            string mensaje = string.Join(" → ", recorrido);
            MessageBox.Show(mensaje, titulo);
        }



        private async void BtnVerMST_Click(object sender, EventArgs e)
        {
            mostrandoMST = true;

            if (rutasVisibles)
            {
                // Si ya están visibles, las ocultamos
                edgesOverlay.Routes.Clear();
                gMap.Refresh();
                rutasVisibles = false;
                btnVerMST.Text = "Ver todas las rutas";
            }
            else
            {
                // Si no están visibles, las mostramos
                edgesOverlay.Routes.Clear();

                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    for (int j = i + 1; j < graph.Nodes.Count; j++)
                    {
                        var from = graph.Nodes[i];
                        var to = graph.Nodes[j];

                        var points = await routeService.GetRouteAsync(from, to);
                        if (points != null)
                        {
                            var route = new GMapRoute(points, $"{from.Name}-{to.Name}")
                            {
                                Stroke = new Pen(Color.Blue, 3) // Línea gruesa azul
                            };
                            edgesOverlay.Routes.Add(route);
                        }
                    }
                }

                gMap.Refresh();
                rutasVisibles = true;
                btnVerMST.Text = "Ocultar rutas";
            }
        }

    }
}
