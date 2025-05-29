using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace GrafoElSalvador
{
    public partial class Form1 : Form
    {
        private GMapControl gMap;
        private GMapOverlay nodesOverlay;
        private GMapOverlay edgesOverlay;
        private Graph graph;
        private RouteService routeService;

        public Form1()
        {

            this.Text = "Grafo de El Salvador";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeMap();

            CreateGraph();

            routeService = new RouteService("tu_api_key_aqui", "rutas.json");

            _ = DrawGraphAsync();
        }

        private void InitializeMap()
        {
            // Igual que antes, solo que aquí
            gMap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.GoogleMap,
                Position = new PointLatLng(13.7942, -88.8965),
                MinZoom = 7,
                MaxZoom = 18,
                Zoom = 10,
                DragButton = MouseButtons.Left
            };

            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            nodesOverlay = new GMapOverlay("nodes");
            edgesOverlay = new GMapOverlay("edges");

            gMap.Overlays.Add(edgesOverlay);
            gMap.Overlays.Add(nodesOverlay);

            Controls.Add(gMap);
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
                new CityNode("Cuscatlán", 13.7333, -89.0500),
                new CityNode("La Paz", 13.507316, -88.870206),
                new CityNode("Cabañas", 13.721740, -88.934541),
                new CityNode("San Vicente", 13.6333, -88.7833),
                new CityNode("Usulután", 13.3500, -88.4500),
                new CityNode("Morazán", 13.695215, -88.106073),
                new CityNode("La Unión", 13.5000, -87.8833)
        };

            foreach (var city in cities)
                graph.AddNode(city);

            for (int i = 0; i < cities.Count; i++)
                for (int j = i + 1; j < cities.Count; j++)
                    graph.AddEdge(cities[i], cities[j]);
        }

        private async Task DrawGraphAsync()
        {
            foreach (var node in graph.Nodes)
            {
                var marker = new GMarkerGoogle(new PointLatLng(node.Latitude, node.Longitude), GMarkerGoogleType.red_dot)
                {
                    ToolTipText = node.Name
                };
                nodesOverlay.Markers.Add(marker);
            }

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
                            Stroke = new Pen(Color.Blue, 2)
                        };
                        edgesOverlay.Routes.Add(route);
                    }
                }
            }

            gMap.Refresh();

            MessageBox.Show("Todas las rutas se han cargado correctamente.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

}
