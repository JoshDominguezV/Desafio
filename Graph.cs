using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GrafoElSalvador
{
    public class CityNode
    {
        public string Name { get; }
        public double Latitude { get; }
        public double Longitude { get; }

        public CityNode(string name, double lat, double lng)
        {
            Name = name;
            Latitude = lat;
            Longitude = lng;
        }
    }

    public class Edge
    {
        public CityNode From { get; }
        public CityNode To { get; }
        public double Weight { get; }

        public Edge(CityNode from, CityNode to)
        {
            From = from;
            To = to;
            Weight = CalculateDistance(from, to);
        }

        private double CalculateDistance(CityNode a, CityNode b)
        {
            var dLat = (b.Latitude - a.Latitude) * Math.PI / 180.0;
            var dLon = (b.Longitude - a.Longitude) * Math.PI / 180.0;
            var lat1 = a.Latitude * Math.PI / 180.0;
            var lat2 = b.Latitude * Math.PI / 180.0;

            var aHarv = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(aHarv), Math.Sqrt(1 - aHarv));

            return 6371 * c; // Distancia en km
        }
    }


    public class Graph
    {
        public List<CityNode> Nodes { get; } = new();
        public List<Edge> Edges { get; } = new();

        public void AddNode(CityNode node)
        {
            Nodes.Add(node);
        }

        public void AddEdge(CityNode from, CityNode to)
        {
            Edges.Add(new Edge(from, to));
        }

        public Dictionary<CityNode, double> Dijkstra(CityNode start, out Dictionary<CityNode, CityNode> previous)
        {
            var distances = new Dictionary<CityNode, double>();
            previous = new Dictionary<CityNode, CityNode>();
            var queue = new PriorityQueue<CityNode, double>();

            foreach (var node in Nodes)
                distances[node] = double.MaxValue;

            distances[start] = 0;
            queue.Enqueue(start, 0);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                var neighbors = Edges.Where(e => e.From == current || e.To == current);

                foreach (var edge in neighbors)
                {
                    var neighbor = edge.From == current ? edge.To : edge.From;
                    var newDist = distances[current] + edge.Weight;

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
        public CityNode GetNodeByName(string name)
        {
            return Nodes.FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }


        public List<CityNode> GetShortestPath(CityNode start, CityNode end)
        {
            var distances = Dijkstra(start, out var previous);

            var path = new List<CityNode>();
            var current = end;

            while (current != null && previous.ContainsKey(current))
            {
                path.Insert(0, current);
                current = previous[current];
            }

            if (path.Count == 0 || path[0] != start)
                path.Insert(0, start); // Asegura que empiece desde el inicio

            return path;
        }



        public List<Edge> GetMinimumSpanningTree()
        {
            var result = new List<Edge>();
            var disjointSet = new DisjointSet<CityNode>(Nodes);

            var sortedEdges = Edges.OrderBy(e => e.Weight);

            foreach (var edge in sortedEdges)
            {
                if (disjointSet.Find(edge.From) != disjointSet.Find(edge.To))
                {
                    disjointSet.Union(edge.From, edge.To);
                    result.Add(edge);
                }
            }

            return result;
        }


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

        public List<CityNode> DFS(CityNode start)
        {
            var visited = new HashSet<CityNode>();
            var recorrido = new List<CityNode>();

            void DFSVisit(CityNode node)
            {
                visited.Add(node);
                recorrido.Add(node);

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

    public class DisjointSet<T>
    {
        private readonly Dictionary<T, T> parent = new();

        public DisjointSet(IEnumerable<T> elements)
        {
            foreach (var elem in elements)
                parent[elem] = elem;
        }

        public T Find(T item)
        {
            if (!parent.ContainsKey(item))
                parent[item] = item;

            if (!EqualityComparer<T>.Default.Equals(parent[item], item))
                parent[item] = Find(parent[item]);

            return parent[item];
        }

        public void Union(T item1, T item2)
        {
            var root1 = Find(item1);
            var root2 = Find(item2);

            if (!EqualityComparer<T>.Default.Equals(root1, root2))
                parent[root1] = root2;
        }
    }

}
