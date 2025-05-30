using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace GrafoElSalvador
{
    public partial class Form1 : MaterialForm
    {
        // Controles para el mapa y sus capas
        private GMapControl gMap; // Control principal del mapa
        private GMapOverlay nodesOverlay; // Capa para los nodos (ciudades)
        private GMapOverlay edgesOverlay; // Capa para las aristas (rutas)

        // Modelo de datos
        private Graph graph; // Grafo que representa las ciudades y conexiones
        private RouteService routeService; // Servicio para obtener rutas de la API

        // Controles de interfaz
        private MaterialComboBox cbOrigen,
            cbDestino; // Combos para selección de ciudades
        private MaterialButton btnCalcularRuta,
            btnVerMST; // Botones principales
        private MaterialButton btnRecorridoAnchura,
            btnRecorridoProfundidad; // Botones de recorridos

        // Estados de la aplicación
        private bool mostrandoMST = false; // Indica si se está mostrando el árbol de expansión mínima
        private bool rutasVisibles = false; // Controla la visibilidad de todas las rutas

        private bool recorridoVisible = false; // Controla si hay un recorrido visible
        private ToolTip routeToolTip = new ToolTip();


        public Form1()
        {
            // Configuración inicial de la ventana principal
            Text = "Grafo de El Salvador";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;

            // Configuración de bordes y botones de la ventana
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Sizable = true; // Permite redimensionar
            this.MaximizeBox = true; // Habilita botón maximizar
            this.MinimizeBox = true; // Habilita botón minimizar
            this.ControlBox = true; // Muestra botones de control

            // Configuración del tema Material Design
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800,
                Primary.BlueGrey900,
                Primary.BlueGrey500,
                Accent.LightBlue200,
                TextShade.WHITE
            );

            // Inicialización de componentes
            InitializeMap(); // Configura el control del mapa
            InitializeControls(); // Configura los controles de la interfaz
            CreateGraph(); // Construye el grafo de ciudades

            gMap.OnRouteEnter += GMap_OnRouteEnter;
            gMap.OnRouteLeave += GMap_OnRouteLeave;
            gMap.MouseMove += GMap_MouseMove;



            // Inicializa el servicio de rutas con la API key y archivo de cache
            routeService = new RouteService(
                "5b3ce3597851110001cf62487ffec6b526ef4d20934af4719c0f9834",
                "rutas.json"
            );

            // Dibuja el grafo de manera asíncrona
            _ = DrawGraphAsync();
        }

        /// <summary>
        /// Inicializa el control del mapa con configuración básica
        /// </summary>
        private void InitializeMap()
        {
            gMap = new GMapControl
            {
                Dock = DockStyle.Fill, // Ocupa todo el espacio disponible
                MapProvider = GMapProviders.GoogleMap, // Usa Google Maps como proveedor
                Position = new PointLatLng(13.7942, -88.8965), // Centro de El Salvador
                MinZoom = 7, // Zoom mínimo permitido
                MaxZoom = 18, // Zoom máximo permitido
                Zoom = 10, // Zoom inicial
                DragButton = MouseButtons.Left, // Botón para arrastrar el mapa
            };

            // Configura el modo de acceso al mapa (servidor y cache local)
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Crea las capas para nodos y aristas
            nodesOverlay = new GMapOverlay("nodes");
            edgesOverlay = new GMapOverlay("edges");

            // Agrega las capas al mapa
            gMap.Overlays.Add(edgesOverlay);
            gMap.Overlays.Add(nodesOverlay);

            // Agrega el mapa al formulario
            Controls.Add(gMap);
        }

        /// <summary>
        /// Configura todos los controles de la interfaz de usuario
        /// </summary>
        private void InitializeControls()
        {
            // ComboBox para selección de ciudad origen
            cbOrigen = new MaterialComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, // No permite texto libre
                Width = 180,
                Left = 15,
                Top = 70,
                Hint = "Inicio", // Texto de placeholder
                ForeColor = SystemColors.GrayText, // Color del texto de hint
            };

            // ComboBox para selección de ciudad destino
            cbDestino = new MaterialComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Left = 210,
                Top = 70,
                Hint = "Destino",
                ForeColor = SystemColors.GrayText,
            };

            // Botón para calcular ruta más corta
            btnCalcularRuta = new MaterialButton
            {
                Text = "Calcular Ruta Más Corta",
                Width = 200,
                Height = 40,
                Left = 410,
                Top = 65,
            };
            btnCalcularRuta.Click += BtnCalcularRuta_Click;

            // Botón para mostrar/ocultar todas las rutas
            btnVerMST = new MaterialButton
            {
                Text = " Ver todas las rutas",
                Width = 340, // Más ancho que los demás
                Height = 40,
                Left = 1050,
                Top = 65,
            };
            btnVerMST.Click += BtnVerMST_Click;

            // Agrega controles al formulario
            Controls.Add(cbOrigen);
            Controls.Add(cbDestino);
            Controls.Add(btnCalcularRuta);
            Controls.Add(btnVerMST);

            // Asegura que los controles estén al frente
            cbOrigen.BringToFront();
            cbDestino.BringToFront();
            btnCalcularRuta.BringToFront();
            btnVerMST.BringToFront();

            // Botón para recorrido en anchura (BFS)
            btnRecorridoAnchura = new MaterialButton
            {
                Text = "Recorrido Anchura",
                Width = 150,
                Height = 40,
                Left = 650,
                Top = 65,
            };
            btnRecorridoAnchura.Click += BtnRecorridoAnchura_Click;

            // Botón para recorrido en profundidad (DFS)
            btnRecorridoProfundidad = new MaterialButton
            {
                Text = "Recorrido Profundidad",
                Width = 150,
                Height = 40,
                Left = 825,
                Top = 65,
            };
            btnRecorridoProfundidad.Click += BtnRecorridoProfundidad_Click;

            // Agrega botones de recorrido al formulario
            Controls.Add(btnRecorridoAnchura);
            Controls.Add(btnRecorridoProfundidad);

            // Asegura visibilidad
            btnRecorridoAnchura.BringToFront();
            btnRecorridoProfundidad.BringToFront();
        }

        /// <summary>
        /// Construye el grafo con las ciudades y sus conexiones
        /// </summary>
        private void CreateGraph()
        {
            graph = new Graph();

            // Lista de ciudades principales de El Salvador con sus coordenadas
            var cities = new List<CityNode>
            {
                new CityNode("San Salvador", 13.6929, -89.2182),
                new CityNode("Santa Ana", 13.9946, -89.5597),
                new CityNode("San Miguel", 13.4833, -88.1833),
                new CityNode("Santa Tecla (Nueva San Salvador)", 13.6769, -89.2797),
                new CityNode("Sonsonate", 13.7186, -89.7245),
                new CityNode("Ahuachapán", 13.9214, -89.8450),
                new CityNode("Chalatenango", 14.0333, -88.9333),
                new CityNode("Sensuntepeque", 13.876184, -88.628492),
                new CityNode("Zacatecoluca", 13.507316, -88.870206),
                new CityNode("Cojutepeque", 13.721740, -88.934541),
                new CityNode("San Vicente", 13.6333, -88.7833),
                new CityNode("Usulután", 13.3500, -88.4500),
                new CityNode("San Francisco Gotera", 13.695215, -88.106073),
                new CityNode("La Unión", 13.5000, -87.8833),
            };

            // Agrega todas las ciudades como nodos al grafo
            foreach (var city in cities)
                graph.AddNode(city);

            // Define las conexiones entre ciudades (aristas del grafo)
            var connections = new Dictionary<string, List<string>>
            {
                {
                    "Ahuachapán",
                    new List<string> { "Santa Ana", "Sonsonate" }
                },
                {
                    "Sensuntepeque",
                    new List<string> { "San Vicente", "San Francisco Gotera"}
                },
                {
                    "Chalatenango",
                    new List<string> { "Santa Ana", "Cojutepeque", "San Salvador", "Sensuntepeque" }
                },
                {
                    "Cojutepeque",
                    new List<string> { "San Vicente","San Salvador", }
                },
                {
                    "Santa Tecla (Nueva San Salvador)",
                    new List<string> { "San Salvador", "Sonsonate", "Santa Ana" }
                },
                {
                    "Zacatecoluca",
                    new List<string> { "San Vicente", "San Salvador", "Usulután" }
                },
                {
                    "La Unión",
                    new List<string> { "San Francisco Gotera", "San Miguel" }
                },
                {
                    "San Miguel",
                    new List<string> { "San Francisco Gotera", "Usulután", "San Vicente" }
                },
                {
                    "Santa Ana",
                    new List<string> { "Sonsonate" }
                },
            };

            // Crea las aristas del grafo basadas en las conexiones definidas
            foreach (var fromCity in connections)
            {
                var fromNode = graph.GetNodeByName(fromCity.Key);
                foreach (var toCityName in fromCity.Value)
                {
                    var toNode = graph.GetNodeByName(toCityName);

                    // Evita duplicados y conexiones nulas
                    if (
                        fromNode != null
                        && toNode != null
                        && !graph.Edges.Any(e =>
                            (e.From == fromNode && e.To == toNode)
                            || (e.From == toNode && e.To == fromNode)
                        )
                    )
                    {
                        graph.AddEdge(fromNode, toNode);
                    }
                }
            }

            // Llena los ComboBox con los nombres de las ciudades
            cbOrigen.Items.AddRange(cities.Select(c => c.Name).ToArray());
            cbDestino.Items.AddRange(cities.Select(c => c.Name).ToArray());
        }

        private void GMap_MouseMove(object sender, MouseEventArgs e)
        {
            var latLng = gMap.FromLocalToLatLng(e.X, e.Y);
            const double tolerance = 0.001;

            List<string> rutasCercanas = new List<string>();

            foreach (var route in edgesOverlay.Routes)
            {
                foreach (var point in route.Points)
                {
                    double distanceLat = Math.Abs(point.Lat - latLng.Lat);
                    double distanceLng = Math.Abs(point.Lng - latLng.Lng);

                    if (distanceLat < tolerance && distanceLng < tolerance)
                    {
                        if (route.Tag is string textoRuta && !rutasCercanas.Contains(textoRuta))
                        {
                            rutasCercanas.Add(textoRuta);
                            break; 
                        }
                    }
                }
            }

            if (rutasCercanas.Count > 0)
            {
                var textoTooltip = string.Join("\n", rutasCercanas);
                var mousePos = gMap.PointToClient(Cursor.Position);
                routeToolTip.Show(textoTooltip, gMap, mousePos.X + 10, mousePos.Y + 10, 2000);
            }
            else
            {
                routeToolTip.Hide(gMap);
            }
        }


        private void GMap_OnRouteEnter(GMapRoute route)
        {
            if (route.Tag is string textoRuta)
            {
                var mousePos = gMap.PointToClient(Cursor.Position);
                routeToolTip.Show(textoRuta, gMap, mousePos.X + 10, mousePos.Y + 10, 3000);
            }
        }

        private void GMap_OnRouteLeave(GMapRoute route)
        {
            routeToolTip.Hide(gMap);
        }

        /// <summary>
        /// Dibuja el grafo en el mapa de manera asíncrona
        /// </summary>
        private async Task DrawGraphAsync()
        {
            // Limpia marcadores y rutas existentes
            nodesOverlay.Markers.Clear();
            edgesOverlay.Routes.Clear();

            // Agrega marcadores para cada ciudad
            foreach (var node in graph.Nodes)
            {
                var marker = new GMarkerGoogle(
                    new PointLatLng(node.Latitude, node.Longitude),
                    GMarkerGoogleType.red_dot
                )
                {
                    ToolTipText = node.Name, // Muestra el nombre al pasar el mouse
                };
                nodesOverlay.Markers.Add(marker);
            }

            // Si no se está mostrando el MST, dibuja todas las rutas
            if (!mostrandoMST)
            {
                foreach (var edge in graph.Edges)
                {
                    // Evita dibujar rutas duplicadas (A-B y B-A)
                    if (
                        graph.Edges.Any(e =>
                            e.From == edge.To
                            && e.To == edge.From
                            && graph.Edges.IndexOf(e) < graph.Edges.IndexOf(edge)
                        )
                    )
                        continue;

                    // Obtiene los puntos de la ruta desde el servicio
                    var points = await routeService.GetRouteAsync(edge.From, edge.To);
                    if (points != null)
                    {
                        var route = new GMapRoute(points, $"{edge.From.Name}-{edge.To.Name}")
                        {
                            Stroke = new Pen(Color.LightBlue, 1), // Estilo de la línea

                            Tag = $"{edge.From.Name} ↔ {edge.To.Name}",

                        };
                        edgesOverlay.Routes.Add(route);
                    }


                }
            }

            // Refresca el mapa para mostrar los cambios
            gMap.Refresh();
        }

        /// <summary>
        /// Maneja el evento de calcular ruta más corta
        /// </summary>
        private async void BtnCalcularRuta_Click(object sender, EventArgs e)
        {
            mostrandoMST = false;

            // Validación de selección
            if (cbOrigen.SelectedItem == null || cbDestino.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un origen y un destino.");
                return;
            }

            // Obtiene los nodos seleccionados
            var origen = graph.GetNodeByName(cbOrigen.SelectedItem.ToString());
            var destino = graph.GetNodeByName(cbDestino.SelectedItem.ToString());

            if (origen == null || destino == null)
            {
                MessageBox.Show("No se encontraron los nodos seleccionados.");
                return;
            }

            // Limpia rutas anteriores
            edgesOverlay.Routes.Clear();

            // Calcula la ruta más corta usando Dijkstra
            var path = graph.GetShortestPath(origen, destino);
            if (path == null || path.Count < 2)
            {
                MessageBox.Show("No se encontró una ruta.");
                return;
            }

            double totalDistancia = 0;

            // Dibuja cada segmento de la ruta
            for (int i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];

                var points = await routeService.GetRouteAsync(from, to);
                if (points != null)
                {
                    var route = new GMapRoute(points, $"{from.Name}-{to.Name}")
                    {
                        Stroke = new Pen(Color.Red, 3), // Ruta en rojo y más gruesa
                        Tag = $"{from.Name} ↔ {to.Name}",
                    };
                    edgesOverlay.Routes.Add(route);
                    totalDistancia += routeService.GetDistance(from, to);
                }
            }

            gMap.Refresh();
            MessageBox.Show(
                $"Ruta más corta calculada.\nDistancia total: {totalDistancia:F2} km.",
                "Ruta por Escalas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        /// <summary>
        /// Dibuja un recorrido (BFS o DFS) en el mapa con animación
        /// </summary>
        private async Task PintarRecorridoAsync(List<CityNode> recorrido)
        {
            if (recorrido == null || recorrido.Count < 2)
                return;

            // Elimina cualquier recorrido anterior
            var recorridoOverlay = gMap.Overlays.FirstOrDefault(o => o.Id == "rutas_recorrido");
            if (recorridoOverlay != null)
                gMap.Overlays.Remove(recorridoOverlay);

            // Crea una nueva capa para el recorrido
            var routeOverlay = new GMapOverlay("rutas_recorrido");
            gMap.Overlays.Add(routeOverlay);

            // Dibuja cada segmento del recorrido con animación
            for (int i = 0; i < recorrido.Count - 1; i++)
            {
                var from = recorrido[i];
                var to = recorrido[i + 1];

                var puntosRuta = await routeService.GetRouteAsync(from, to);

                if (puntosRuta != null)
                {
                    var ruta = new GMapRoute(puntosRuta, $"{from.Name}-{to.Name}")
                    {
                        Stroke = new Pen(Color.OrangeRed, 3), // Color distintivo para recorridos
                    };
                    routeOverlay.Routes.Add(ruta);

                    gMap.Refresh();
                    await Task.Delay(500); // Retraso para efecto de animación
                }
            }
        }

        /// <summary>
        /// Obtiene el nodo seleccionado en el ComboBox de origen
        /// </summary>
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

        

        /// <summary>
        /// Maneja el evento de recorrido en anchura (BFS)
        /// </summary>
        private async void BtnRecorridoAnchura_Click(object sender, EventArgs e)
        {
            var nodoInicial = ObtenerNodoSeleccionado();
            if (nodoInicial == null)
                return;

            // Comportamiento de toggle: si ya está visible, lo oculta
            if (recorridoVisible)
            {
                var recorridoOverlay = gMap.Overlays.FirstOrDefault(o => o.Id == "rutas_recorrido");
                if (recorridoOverlay != null)
                {
                    gMap.Overlays.Remove(recorridoOverlay);
                    gMap.Refresh();
                    Tag = $"Nodo Inicial {nodoInicial.Name}";
                }
                recorridoVisible = false;
                return;
            }

            // Si no está visible, calcula y muestra el recorrido BFS
            var recorrido = graph.BFS(nodoInicial);
            await PintarRecorridoAsync(recorrido);
            recorridoVisible = true;
        }

        /// <summary>
        /// Maneja el evento de recorrido en profundidad (DFS)
        /// </summary>
        private async void BtnRecorridoProfundidad_Click(object sender, EventArgs e)
        {
            var nodoInicial = ObtenerNodoSeleccionado();
            if (nodoInicial == null)
                return;

            // Comportamiento de toggle: si ya está visible, lo oculta
            if (recorridoVisible)
            {
                var recorridoOverlay = gMap.Overlays.FirstOrDefault(o => o.Id == "rutas_recorrido");
                if (recorridoOverlay != null)
                {
                    gMap.Overlays.Remove(recorridoOverlay);
                    gMap.Refresh();
                    Tag = $"Nodo Inicial {nodoInicial.Name}";
                }
                recorridoVisible = false;
                return;
            }

            // Si no está visible, calcula y muestra el recorrido DFS
            var recorrido = graph.DFS(nodoInicial);
            await PintarRecorridoAsync(recorrido);
            recorridoVisible = true;
        }

        /// <summary>
        /// Muestra un recorrido en un MessageBox (usado para depuración)
        /// </summary>
        private void MostrarRecorrido(List<string> recorrido, string titulo)
        {
            string mensaje = string.Join(" ↔ ", recorrido);
            MessageBox.Show(mensaje, titulo);
        }

        /// <summary>
        /// Maneja el evento de mostrar/ocultar todas las rutas con animación
        /// </summary>
        private async void BtnVerMST_Click(object sender, EventArgs e)
        {
            mostrandoMST = true;

            if (rutasVisibles)
            {
                // Animación para ocultar rutas una por una
                btnVerMST.Enabled = false; // Deshabilita el botón durante la animación

                while (edgesOverlay.Routes.Count > 0)
                {
                    edgesOverlay.Routes.RemoveAt(0);
                    gMap.Refresh();
                    await Task.Delay(100); // Retraso entre cada remoción
                }

                rutasVisibles = false;
                btnVerMST.Text = "Ver todas las rutas";
                btnVerMST.Enabled = true; // Vuelve a habilitar el botón
            }
            else
            {
                // Animación para mostrar rutas una por una
                btnVerMST.Enabled = false;
                edgesOverlay.Routes.Clear();

                var connections = new Dictionary<string, List<string>>
            {
                {
                    "Ahuachapán",
                    new List<string> { "Santa Ana", "Sonsonate" }
                },
                {
                    "Sensuntepeque",
                    new List<string> { "San Vicente", "San Francisco Gotera"}
                },
                {
                    "Chalatenango",
                    new List<string> { "Santa Ana", "Cojutepeque", "San Salvador", "Sensuntepeque" }
                },
                {
                    "Cojutepeque",
                    new List<string> { "San Vicente","San Salvador", }
                },
                {
                    "Santa Tecla (Nueva San Salvador)",
                    new List<string> { "San Salvador", "Sonsonate", "Santa Ana" }
                },
                {
                    "Zacatecoluca",
                    new List<string> { "San Vicente", "San Salvador", "Usulután" }
                },
                {
                    "La Unión",
                    new List<string> { "San Francisco Gotera", "San Miguel" }
                },
                {
                    "San Miguel",
                    new List<string> { "San Francisco Gotera", "Usulután", "San Vicente" }
                },
                {
                    "Santa Ana",
                    new List<string> { "Sonsonate" }
                },
            };

                // Dibuja cada conexión con retraso para efecto de animación
                foreach (var connection in connections)
                {
                    var fromNode = graph.GetNodeByName(connection.Key);
                    if (fromNode == null)
                        continue;

                    foreach (var toCity in connection.Value)
                    {
                        var toNode = graph.GetNodeByName(toCity);
                        if (toNode == null)
                            continue;

                        var points = await routeService.GetRouteAsync(fromNode, toNode);
                        if (points != null)
                        {
                            var route = new GMapRoute(points, $"{fromNode.Name}-{toNode.Name}")
                            {
                                Stroke = new Pen(Color.Blue, 3),
                                Tag = $"{fromNode.Name} ↔ {toNode.Name}", // Mostrar en el tooltip
                            };
                            edgesOverlay.Routes.Add(route);
                            gMap.Refresh();
                            await Task.Delay(200); // Retraso entre cada ruta
                        }
                    }
                }

                rutasVisibles = true;
                btnVerMST.Text = "Ocultar rutas";
                btnVerMST.Enabled = true;
            }
        }
    }
}
