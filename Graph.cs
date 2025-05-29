using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GrafoElSalvador
{
    /// <summary>
    /// Representa un nodo (ciudad) en el grafo con su nombre y coordenadas geográficas
    /// </summary>
    public class CityNode
    {
        public string Name { get; } // Nombre de la ciudad
        public double Latitude { get; } // Latitud geográfica
        public double Longitude { get; } // Longitud geográfica

        /// <summary>
        /// Constructor para crear un nuevo nodo de ciudad
        /// </summary>
        /// <param name="name">Nombre de la ciudad</param>
        /// <param name="lat">Latitud en grados decimales</param>
        /// <param name="lng">Longitud en grados decimales</param>
        public CityNode(string name, double lat, double lng)
        {
            Name = name;
            Latitude = lat;
            Longitude = lng;
        }
    }

    /// <summary>
    /// Representa una arista (conexión entre ciudades) con su peso (distancia)
    /// </summary>
    public class Edge
    {
        public CityNode From { get; } // Ciudad de origen
        public CityNode To { get; } // Ciudad de destino
        public double Weight { get; } // Distancia en kilómetros

        /// <summary>
        /// Constructor que crea una nueva arista y calcula automáticamente la distancia
        /// </summary>
        /// <param name="from">Nodo de origen</param>
        /// <param name="to">Nodo de destino</param>
        public Edge(CityNode from, CityNode to)
        {
            From = from;
            To = to;
            Weight = CalculateDistance(from, to); // Calcula la distancia al crear la arista
        }

        /// <summary>
        /// Calcula la distancia entre dos nodos usando la fórmula del haversine
        /// </summary>
        /// <returns>Distancia en kilómetros</returns>
        private double CalculateDistance(CityNode a, CityNode b)
        {
            // Conversión a radianes
            var dLat = (b.Latitude - a.Latitude) * Math.PI / 180.0;
            var dLon = (b.Longitude - a.Longitude) * Math.PI / 180.0;
            var lat1 = a.Latitude * Math.PI / 180.0;
            var lat2 = b.Latitude * Math.PI / 180.0;

            // Fórmula del haversine
            var aHarv =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(aHarv), Math.Sqrt(1 - aHarv));

            return 6371 * c; // Radio de la Tierra en km
        }
    }

    /// <summary>
    /// Representa un grafo de ciudades con operaciones para análisis de rutas
    /// </summary>
    public class Graph
    {
        public List<CityNode> Nodes { get; } = new(); // Lista de nodos (ciudades)
        public List<Edge> Edges { get; } = new(); // Lista de aristas (conexiones)

        /// <summary>
        /// Agrega un nuevo nodo (ciudad) al grafo
        /// </summary>
        public void AddNode(CityNode node)
        {
            Nodes.Add(node);
        }

        /// <summary>
        /// Agrega una nueva arista (conexión entre ciudades) al grafo
        /// </summary>
        public void AddEdge(CityNode from, CityNode to)
        {
            Edges.Add(new Edge(from, to));
        }

        /// <summary>
        /// Implementa el algoritmo de Dijkstra para encontrar caminos más cortos
        /// </summary>
        /// <param name="start">Nodo de inicio</param>
        /// <param name="previous">Diccionario para reconstruir rutas</param>
        /// <returns>Distancias más cortas desde el nodo inicial</returns>
        public Dictionary<CityNode, double> Dijkstra(
            CityNode start,
            out Dictionary<CityNode, CityNode> previous
        )
        {
            var distances = new Dictionary<CityNode, double>();
            previous = new Dictionary<CityNode, CityNode>();
            var queue = new PriorityQueue<CityNode, double>();

            // Inicialización
            foreach (var node in Nodes)
                distances[node] = double.MaxValue;

            distances[start] = 0;
            queue.Enqueue(start, 0);

            // Procesamiento de nodos
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Exploración de vecinos
                var neighbors = Edges.Where(e => e.From == current || e.To == current);

                foreach (var edge in neighbors)
                {
                    var neighbor = edge.From == current ? edge.To : edge.From;
                    var newDist = distances[current] + edge.Weight;

                    // Relajación de aristas
                    if (newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        previous[neighbor] = current;
                        queue.Enqueue(neighbor, newDist);
                    }
                }
            }

            return distances;
        }

        /// <summary>
        /// Busca un nodo por su nombre (insensible a mayúsculas/minúsculas)
        /// </summary>
        public CityNode GetNodeByName(string name)
        {
            return Nodes.FirstOrDefault(n =>
                n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Obtiene el camino más corto entre dos nodos usando Dijkstra
        /// </summary>
        public List<CityNode> GetShortestPath(CityNode start, CityNode end)
        {
            var distances = Dijkstra(start, out var previous);

            // Reconstrucción del camino desde el final
            var path = new List<CityNode>();
            var current = end;

            while (current != null && previous.ContainsKey(current))
            {
                path.Insert(0, current);
                current = previous[current];
            }

            // Asegura que el camino comience en el nodo de inicio
            if (path.Count == 0 || path[0] != start)
                path.Insert(0, start);

            return path;
        }

        /// <summary>
        /// Calcula el árbol de expansión mínima usando el algoritmo de Kruskal
        /// </summary>
        public List<Edge> GetMinimumSpanningTree()
        {
            var result = new List<Edge>();
            var disjointSet = new DisjointSet<CityNode>(Nodes);

            // Ordena aristas por peso ascendente
            var sortedEdges = Edges.OrderBy(e => e.Weight);

            foreach (var edge in sortedEdges)
            {
                // Evita ciclos verificando si están en conjuntos disjuntos
                if (disjointSet.Find(edge.From) != disjointSet.Find(edge.To))
                {
                    disjointSet.Union(edge.From, edge.To);
                    result.Add(edge);
                }
            }

            return result;
        }

        /// <summary>
        /// Realiza un recorrido en anchura (BFS) desde un nodo inicial
        /// </summary>
        public List<CityNode> BFS(CityNode start)
        {
            var visited = new HashSet<CityNode>();
            var queue = new Queue<CityNode>();
            var recorrido = new List<CityNode>();

            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                recorrido.Add(current);

                // Obtiene vecinos no visitados
                var neighbors = Edges
                    .Where(e => e.From == current || e.To == current)
                    .Select(e => e.From == current ? e.To : e.From);

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return recorrido;
        }

        /// <summary>
        /// Realiza un recorrido en profundidad (DFS) desde un nodo inicial
        /// </summary>
        public List<CityNode> DFS(CityNode start)
        {
            var visited = new HashSet<CityNode>();
            var recorrido = new List<CityNode>();

            // Función recursiva interna
            void DFSVisit(CityNode node)
            {
                visited.Add(node);
                recorrido.Add(node);

                // Explora vecinos recursivamente
                var neighbors = Edges
                    .Where(e => e.From == node || e.To == node)
                    .Select(e => e.From == node ? e.To : e.From);

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        DFSVisit(neighbor);
                    }
                }
            }

            DFSVisit(start);
            return recorrido;
        }
    }

    /// <summary>
    /// Implementa una estructura Union-Find (Conjuntos Disjuntos) para el algoritmo de Kruskal
    /// </summary>
    public class DisjointSet<T>
    {
        private readonly Dictionary<T, T> parent = new();

        /// <summary>
        /// Inicializa la estructura con elementos individuales
        /// </summary>
        public DisjointSet(IEnumerable<T> elements)
        {
            foreach (var elem in elements)
                parent[elem] = elem; // Cada elemento es su propio padre inicialmente
        }

        /// <summary>
        /// Encuentra el representante (raíz) del conjunto que contiene el elemento
        /// </summary>
        public T Find(T item)
        {
            if (!parent.ContainsKey(item))
                parent[item] = item; // Auto-inicialización

            // Compresión de camino
            if (!EqualityComparer<T>.Default.Equals(parent[item], item))
                parent[item] = Find(parent[item]);

            return parent[item];
        }

        /// <summary>
        /// Une dos conjuntos en uno solo
        /// </summary>
        public void Union(T item1, T item2)
        {
            var root1 = Find(item1);
            var root2 = Find(item2);

            if (!EqualityComparer<T>.Default.Equals(root1, root2))
                parent[root1] = root2; // Une por raíces
        }
    }
}
