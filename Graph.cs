using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            double dLat = (a.Latitude - b.Latitude) * Math.PI / 180.0;
            double dLng = (a.Longitude - b.Longitude) * Math.PI / 180.0;
            double lat1 = a.Latitude * Math.PI / 180.0;
            double lat2 = b.Latitude * Math.PI / 180.0;

            double aCalc = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Sin(dLng / 2) * Math.Sin(dLng / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(aCalc), Math.Sqrt(1 - aCalc));
            double earthRadiusKm = 6371;
            return earthRadiusKm * c;
        }
    }

    public class Graph
    {
        public List<CityNode> Nodes { get; } = new();
        public List<Edge> Edges { get; } = new();

        public void AddNode(CityNode node) => Nodes.Add(node);
        public void AddEdge(CityNode from, CityNode to) => Edges.Add(new Edge(from, to));

        public List<Edge> GenerateMinimumSpanningTree()
        {
            List<Edge> result = new();
            var parent = new Dictionary<CityNode, CityNode>();

            CityNode Find(CityNode node)
            {
                if (!parent.ContainsKey(node)) parent[node] = node;
                if (parent[node] == node) return node;
                parent[node] = Find(parent[node]);
                return parent[node];
            }

            void Union(CityNode a, CityNode b)
            {
                var rootA = Find(a);
                var rootB = Find(b);
                if (rootA != rootB) parent[rootB] = rootA;
            }

            Edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            foreach (var edge in Edges)
            {
                if (Find(edge.From) != Find(edge.To))
                {
                    result.Add(edge);
                    Union(edge.From, edge.To);
                }
            }

            return result;
        }
    }

}
