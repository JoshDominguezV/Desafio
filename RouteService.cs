using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GrafoElSalvador
{
    public class RouteService
    {
        private readonly string apiKey;
        private readonly string archivoRutas;
        private Dictionary<string, List<PointLatLng>> rutasCache;

        public RouteService(string apiKey, string archivoRutas)
        {
            this.apiKey = apiKey;
            this.archivoRutas = archivoRutas;
            rutasCache = new Dictionary<string, List<PointLatLng>>();
            LoadRoutesFromFile();
        }


        private void LoadRoutesFromFile()
        {
            if (!File.Exists(archivoRutas)) return;

            try
            {
                // Verificar si el archivo está vacío
                if (new FileInfo(archivoRutas).Length == 0)
                {
                    File.Delete(archivoRutas);
                    return;
                }

                var json = File.ReadAllText(archivoRutas);

                // Verificar si el contenido es nulo o vacío
                if (string.IsNullOrWhiteSpace(json))
                {
                    File.Delete(archivoRutas);
                    return;
                }

                var rawData = JsonSerializer.Deserialize<Dictionary<string, List<double[]>>>(json);

                // Si la deserialización devuelve null (archivo JSON inválido o vacío)
                if (rawData == null)
                {
                    File.Delete(archivoRutas);
                    return;
                }

                // Si el diccionario está vacío
                if (rawData.Count == 0)
                {
                    File.Delete(archivoRutas);
                    return;
                }

                // Si todo está bien, cargar los datos
                foreach (var kv in rawData)
                {
                    rutasCache[kv.Key] = kv.Value.Select(c => new PointLatLng(c[0], c[1])).ToList();
                }
            }
            catch (JsonException) // Si hay error en el formato JSON
            {
                File.Delete(archivoRutas);
            }
            catch (Exception ex) // Otros errores inesperados
            {
                // Opcional: puedes loguear el error si quieres
                File.Delete(archivoRutas);
            }
        }
        private void SaveRouteToFile(string key, List<PointLatLng> points)
        {
            var data = new Dictionary<string, List<double[]>>();

            if (File.Exists(archivoRutas))
            {
                var content = File.ReadAllText(archivoRutas);
                data = JsonSerializer.Deserialize<Dictionary<string, List<double[]>>>(content);
            }

            data[key] = points.Select(p => new double[] { p.Lat, p.Lng }).ToList();

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(archivoRutas, json);
        }

        public double GetDistance(CityNode from, CityNode to)
        {
            var earthRadiusKm = 6371;

            double dLat = DegreesToRadians(to.Latitude - from.Latitude);
            double dLon = DegreesToRadians(to.Longitude - from.Longitude);

            double lat1 = DegreesToRadians(from.Latitude);
            double lat2 = DegreesToRadians(to.Latitude);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }


        public async Task<List<PointLatLng>> GetRouteAsync(CityNode start, CityNode end)
        {
            string key = $"{start.Name}-{end.Name}";

            if (rutasCache.ContainsKey(key))
                return rutasCache[key];

            try
            {
                using HttpClient client = new();
                string url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={apiKey}&start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error obteniendo ruta {start.Name} -> {end.Name}: {response.ReasonPhrase}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

                var coords = doc.RootElement.GetProperty("features")[0].GetProperty("geometry").GetProperty("coordinates");

                var points = new List<PointLatLng>();

                foreach (var coord in coords.EnumerateArray())
                {
                    double lng = coord[0].GetDouble();
                    double lat = coord[1].GetDouble();
                    points.Add(new PointLatLng(lat, lng));
                }

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
