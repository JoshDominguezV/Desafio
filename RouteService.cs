using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GMap.NET;

namespace GrafoElSalvador
{
    /// <summary>
    /// Servicio para obtener y gestionar rutas entre ciudades usando la API de OpenRouteService
    /// </summary>
    public class RouteService
    {
        private readonly string apiKey; // Clave API para OpenRouteService
        private readonly string archivoRutas; // Ruta del archivo JSON para cache local
        private Dictionary<string, List<PointLatLng>> rutasCache; // Cache en memoria de rutas

        /// <summary>
        /// Constructor del servicio de rutas
        /// </summary>
        /// <param name="apiKey">Clave de API para OpenRouteService</param>
        /// <param name="archivoRutas">Ruta del archivo JSON para persistencia local</param>
        public RouteService(string apiKey, string archivoRutas)
        {
            this.apiKey = apiKey;
            this.archivoRutas = archivoRutas;
            rutasCache = new Dictionary<string, List<PointLatLng>>();
            LoadRoutesFromFile(); // Carga rutas desde archivo al iniciar
        }

        /// <summary>
        /// Carga las rutas almacenadas en el archivo JSON al cache en memoria
        /// </summary>
        private void LoadRoutesFromFile()
        {
            if (!File.Exists(archivoRutas))
                return;

            try
            {
                // Verifica si el archivo está vacío
                if (new FileInfo(archivoRutas).Length == 0)
                {
                    File.Delete(archivoRutas);
                    return;
                }

                var json = File.ReadAllText(archivoRutas);

                // Verifica contenido nulo o vacío
                if (string.IsNullOrWhiteSpace(json))
                {
                    File.Delete(archivoRutas);
                    return;
                }

                // Deserializa el JSON a un diccionario
                var rawData = JsonSerializer.Deserialize<Dictionary<string, List<double[]>>>(json);

                // Valida datos deserializados
                if (rawData == null || rawData.Count == 0)
                {
                    File.Delete(archivoRutas);
                    return;
                }

                // Convierte y carga los datos al cache
                foreach (var kv in rawData)
                {
                    rutasCache[kv.Key] = kv.Value.Select(c => new PointLatLng(c[0], c[1])).ToList();
                }
            }
            catch (JsonException) // Maneja errores de formato JSON
            {
                File.Delete(archivoRutas);
            }
            catch (Exception ex) // Maneja otros errores inesperados
            {
                // En producción debería loguearse este error
                File.Delete(archivoRutas);
            }
        }

        /// <summary>
        /// Guarda una ruta en el archivo JSON de cache
        /// </summary>
        /// <param name="key">Identificador de la ruta (origen-destino)</param>
        /// <param name="points">Puntos que forman la ruta</param>
        private void SaveRouteToFile(string key, List<PointLatLng> points)
        {
            var data = new Dictionary<string, List<double[]>>();

            // Carga datos existentes si el archivo existe
            if (File.Exists(archivoRutas))
            {
                var content = File.ReadAllText(archivoRutas);
                data =
                    JsonSerializer.Deserialize<Dictionary<string, List<double[]>>>(content)
                    ?? new();
            }

            // Convierte puntos a formato serializable y los agrega
            data[key] = points.Select(p => new double[] { p.Lat, p.Lng }).ToList();

            // Serializa y guarda todo el diccionario
            var json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            );
            File.WriteAllText(archivoRutas, json);
        }

        /// <summary>
        /// Calcula la distancia entre dos nodos usando la fórmula del haversine
        /// </summary>
        /// <returns>Distancia en kilómetros</returns>
        public double GetDistance(CityNode from, CityNode to)
        {
            const double earthRadiusKm = 6371; // Radio de la Tierra en km

            // Conversión a radianes
            double dLat = DegreesToRadians(to.Latitude - from.Latitude);
            double dLon = DegreesToRadians(to.Longitude - from.Longitude);
            double lat1 = DegreesToRadians(from.Latitude);
            double lat2 = DegreesToRadians(to.Latitude);

            // Fórmula del haversine
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        /// <summary>
        /// Convierte grados a radianes
        /// </summary>
        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// Obtiene la ruta entre dos ciudades, usando cache local si está disponible
        /// </summary>
        /// <returns>Lista de puntos geográficos que forman la ruta o null si hay error</returns>
        public async Task<List<PointLatLng>> GetRouteAsync(CityNode start, CityNode end)
        {
            string key = $"{start.Name}-{end.Name}";

            // Primero verifica en cache
            if (rutasCache.ContainsKey(key))
                return rutasCache[key];

            try
            {
                using HttpClient client = new();
                // Construye URL para la API de OpenRouteService
                string url =
                    $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={apiKey}&start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        $"Error obteniendo ruta {start.Name} -> {end.Name}: {response.ReasonPhrase}"
                    );
                    return null;
                }

                // Procesa la respuesta JSON
                var json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

                // Extrae las coordenadas del JSON
                var coords = doc
                    .RootElement.GetProperty("features")[0]
                    .GetProperty("geometry")
                    .GetProperty("coordinates");

                var points = new List<PointLatLng>();

                // Convierte coordenadas a puntos geográficos
                foreach (var coord in coords.EnumerateArray())
                {
                    double lng = coord[0].GetDouble();
                    double lat = coord[1].GetDouble();
                    points.Add(new PointLatLng(lat, lng));
                }

                // Almacena en cache y guarda en archivo
                rutasCache[key] = points;
                SaveRouteToFile(key, points);

                return points;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error obteniendo ruta: {ex.Message}");
                return null;
            }
        }
    }
}
