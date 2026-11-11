using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Convierte los waypoints de la escena en un grafo y devuelve la ruta mas barata hasta la Base.
public sealed class PathGraphController : MonoBehaviour
{
    // El 17 es un vertice logico: su posicion se toma del Transform de la Base.
    private const int BaseNode = 17;
    private const int DefaultSpawnNode = 1;
    private const int CurrentLayoutVersion = 2;

    [Serializable]
    private struct GraphNode
    {
        // ID usado por el grafo y por WaveController para elegir el punto de salida.
        public int id;
        // Posicion de respaldo si la escena no tiene un GameObject llamado Nodo{id}.
        public Vector2 position;

        public GraphNode(int nodeId, float x, float y)
        {
            id = nodeId;
            position = new Vector2(x, y);
        }
    }

    [Serializable]
    private struct GraphEdge
    {
        // IDs de los dos extremos de una conexion. El costo se calcula con sus posiciones.
        public int from;
        public int to;

        public GraphEdge(int first, int second)
        {
            from = first;
            to = second;
        }
    }

    [Header("Diseño del grafo")]
    [Tooltip("Cada elemento es un punto del camino. Usa IDs únicos de 1 a 32.")]
    [SerializeField] private GraphNode[] nodes;
    [Tooltip("Cada elemento conecta dos nodos en ambos sentidos. Agrega conexiones para crear desvíos.")]
    [SerializeField] private GraphEdge[] edges;
    // Permite reemplazar una sola vez una configuracion vieja sin pisar cambios posteriores.
    [SerializeField, HideInInspector] private int layoutVersion;

    private readonly Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();
    private List<int> latestPath = new List<int>();
    private GrafoMA graph;
    private Transform spawn;
    private Transform goal;

    public void ConfigureEndpoints(Transform spawnPoint, Transform destination)
    {
        // WaveController pasa estos objetos de la escena para alinear el grafo con Spawn y Base.
        spawn = spawnPoint;
        goal = destination;
        BuildGraph();
    }

    public bool TryGetShortestRoute(out List<Vector3> route) => TryGetShortestRoute(DefaultSpawnNode, out route);

    public bool TryGetShortestRoute(int originNode, out List<Vector3> route)
    {
        // Se reconstruye antes de cada busqueda para usar las posiciones actuales de los waypoints.
        BuildGraph();
        route = new List<Vector3>();
        if (!AlgDijkstra.TryGetShortestPath(graph, originNode, BaseNode, out List<int> nodePath, out int cost))
            return false;

        // Dijkstra devuelve IDs; el enemigo necesita posiciones para poder moverse en la escena.
        latestPath = nodePath;
        StringBuilder description = new StringBuilder("[GRAFO + DIJKSTRA] Ruta minima: ");
        foreach (int node in nodePath)
        {
            route.Add(nodePositions[node]);
            if (description[description.Length - 1] != ' ')
                description.Append(" -> ");
            description.Append(node);
        }

        description.Append(" | costo: ").Append(cost);
        Debug.Log(description.ToString(), this);
        return true;
    }

    private void Awake() => BuildGraph();

    private void Reset() => CreateDefaultLayout();

    private void OnValidate()
    {
        // Actualiza una sola vez los datos viejos que todavía tenían una grilla de 20 nodos.
        if (layoutVersion != CurrentLayoutVersion)
        {
            CreateDefaultLayout();
            return;
        }

        if (nodes == null || nodes.Length == 0)
            CreateDefaultLayout();
    }

    private void CreateDefaultLayout()
    {
        // Estas posiciones coinciden con los waypoints Nodo1..Nodo16 de Level1.
        nodes = new[]
        {
            new GraphNode(1, -8f,  0f), new GraphNode(2, -5f,  2f), new GraphNode(3, -1f,  3f), new GraphNode(4, -1f,  0f),
            new GraphNode(5, -5f, -2f), new GraphNode(6, -1f, -3f), new GraphNode(7,  5f,  0f), new GraphNode(8,  8f,  0f),
            new GraphNode(9,  1f,  2f), new GraphNode(10, 1f, -2f), new GraphNode(11,-3f,  2f), new GraphNode(12,-3f, -2f),
            new GraphNode(13, 3f,  2f), new GraphNode(14, 3f, -2f), new GraphNode(15,-8f,  4f), new GraphNode(16,-8f, -4f),
            new GraphNode(17, 8f, 1f)
        };

        // Estas conexiones forman los caminos posibles. Para sumar un desvio, agrega un GraphEdge.
        edges = new[]
        {
            new GraphEdge(1,2), new GraphEdge(1,5), new GraphEdge(2,11), new GraphEdge(2,15),
            new GraphEdge(11,3), new GraphEdge(11,12), new GraphEdge(12,5), new GraphEdge(12,6),
            new GraphEdge(5,16), new GraphEdge(3,9), new GraphEdge(3,4), new GraphEdge(9,13),
            new GraphEdge(9,10), new GraphEdge(10,6), new GraphEdge(10,14), new GraphEdge(13,14),
            new GraphEdge(13,7), new GraphEdge(14,7), new GraphEdge(4,8), new GraphEdge(7,8),
            new GraphEdge(8,17)
        };
        layoutVersion = CurrentLayoutVersion;
    }

    // Primero registra todos los vertices; despues crea las aristas cuyos extremos existen.
    private void BuildGraph()
    {
        if (nodes == null || nodes.Length == 0)
            CreateDefaultLayout();

        nodePositions.Clear();
        graph = new GrafoMA();
        graph.InicializarGrafo();
        foreach (GraphNode node in nodes)
        {
            // Se ignoran IDs fuera del rango y duplicados para no crear vertices ambiguos.
            if (node.id < 1 || node.id > 32 || nodePositions.ContainsKey(node.id))
                continue;
            Vector3 position = GetWaypointPosition(node);
            nodePositions.Add(node.id, position);
            graph.AgregarVertice(node.id);
        }

        if (edges == null)
            return;
        foreach (GraphEdge edge in edges)
        {
            // Una conexion incompleta se omite; ambos nodos deben haberse registrado primero.
            if (!nodePositions.ContainsKey(edge.from) || !nodePositions.ContainsKey(edge.to) || edge.from == edge.to)
                continue;
            AddBidirectionalEdge(edge.from, edge.to);
        }
    }

    // La escena manda sobre la posicion de NodoN; el valor guardado sirve de respaldo.
    private Vector3 GetWaypointPosition(GraphNode node)
    {
        // La Base se toma del objeto configurado, aunque su ID logico sea 17.
        if (node.id == BaseNode && goal != null)
            return goal.position;

        // Los nodos del nivel ya existen como objetos Nodo1, Nodo2, etc.; el grafo
        // toma su posición directamente para que los puntos y las aristas coincidan.
        GameObject waypoint = GameObject.Find($"Nodo{node.id}");
        return waypoint != null
            ? waypoint.transform.position
            : new Vector3(node.position.x, node.position.y, 0f);
    }

    // Una entrada de Edges habilita el paso en ambos sentidos y usa distancia como peso.
    private void AddBidirectionalEdge(int first, int second)
    {
        int cost = GetDistanceCost(first, second);
        graph.AgregarArista(first, second, cost);
        graph.AgregarArista(second, first, cost);
    }

    private int GetDistanceCost(int from, int to) =>
        // Dijkstra trabaja con enteros: se redondea la distancia y se evita un costo cero.
        Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(nodePositions[from], nodePositions[to])));

    private bool IsSelectedEdge(int from, int to)
    {
        // Compara cada conexion con los pares consecutivos de la ultima ruta calculada.
        for (int index = 0; index < latestPath.Count - 1; index++)
        {
            int first = latestPath[index];
            int second = latestPath[index + 1];
            if ((first == from && second == to) || (first == to && second == from))
                return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        // Dibujo de ayuda del Editor: gris para conexiones disponibles, amarillo para la ultima ruta.
        BuildGraph();
        if (edges != null)
        {
            foreach (GraphEdge edge in edges)
            {
                if (nodePositions.ContainsKey(edge.from) && nodePositions.ContainsKey(edge.to))
                    DrawEdge(edge.from, edge.to);
            }
        }

        foreach (KeyValuePair<int, Vector3> node in nodePositions)
        {
            Gizmos.color = node.Key == DefaultSpawnNode ? Color.green : node.Key == BaseNode ? Color.red : Color.white;
            Gizmos.DrawSphere(node.Value, 0.18f);
#if UNITY_EDITOR
            Handles.Label(node.Value + new Vector3(0.18f, 0.18f, 0f), $"{node.Key}");
#endif
        }
    }

    private void DrawEdge(int from, int to)
    {
        Gizmos.color = IsSelectedEdge(from, to) ? Color.yellow : new Color(0.65f, 0.75f, 0.85f, 0.85f);
        Gizmos.DrawLine(nodePositions[from], nodePositions[to]);
    }
}
