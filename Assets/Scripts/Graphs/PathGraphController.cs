using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Une el TDA Grafo con el nivel. Cada vertice representa un punto del camino.
public sealed class PathGraphController : MonoBehaviour
{
    [Header("Nodos intermedios del camino")]
    [SerializeField] private Vector3 upperLeftNode = new Vector3(-4f, 2.5f, 0f);
    [SerializeField] private Vector3 upperRightNode = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private Vector3 middleNode = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 lowerLeftNode = new Vector3(-4f, -2.5f, 0f);
    [SerializeField] private Vector3 lowerRightNode = new Vector3(0f, -2.5f, 0f);
    [SerializeField] private Vector3 mergeNode = new Vector3(3f, 0f, 0f);

    [Header("Camino visible en el nivel")]
    [SerializeField, Min(0.1f)] private float roadWidth = 0.65f;
    [SerializeField] private Color inactiveRoadColor = new Color(0.15f, 0.26f, 0.32f, 1f);
    [SerializeField] private Color selectedRoadColor = new Color(1f, 0.72f, 0.12f, 1f);

    private readonly Dictionary<int, Vector3> nodePositions = new Dictionary<int, Vector3>();
    private readonly List<LineRenderer> roadSegments = new List<LineRenderer>();
    private GrafoMA graph;
    private Transform spawn;
    private Transform goal;
    private Transform roadRoot;
    private Material roadMaterial;

    public void ConfigureEndpoints(Transform spawnPoint, Transform destination)
    {
        spawn = spawnPoint;
        goal = destination;
        BuildGraph();
        RefreshRoadVisuals(null);
    }

    public bool TryGetShortestRoute(out List<Vector3> route)
    {
        return TryGetShortestRoute(1, out route);
    }

    // Cada oleada puede comenzar en un nodo diferente, pero todas buscan Nodo8 (Base).
    public bool TryGetShortestRoute(int originNode, out List<Vector3> route)
    {
        BuildGraph();
        route = new List<Vector3>();
        if (!AlgDijkstra.TryGetShortestPath(graph, originNode, 8, out List<int> nodePath, out int cost))
            return false;

        RefreshRoadVisuals(nodePath);

        StringBuilder description = new StringBuilder("[GRAFO + DIJKSTRA] Ruta minima: ");
        for (int index = 0; index < nodePath.Count; index++)
        {
            int node = nodePath[index];
            route.Add(nodePositions[node]);
            description.Append(node);
            if (index < nodePath.Count - 1)
                description.Append(" -> ");
        }

        description.Append(" | costo: ").Append(cost);
        Debug.Log(description.ToString(), this);
        return true;
    }

    private void Awake() => BuildGraph();

    private void BuildGraph()
    {
        // Cada vertice toma su posicion de los objetos Nodo1 a Nodo16 de Level1.
        // Asi se puede rediseñar el mapa moviendo nodos desde Unity, sin tocar Dijkstra.
        nodePositions[1] = GetNodePosition(1, spawn != null ? spawn.position : new Vector3(-7f, 0f, 0f));
        nodePositions[2] = GetNodePosition(2, upperLeftNode);
        nodePositions[3] = GetNodePosition(3, upperRightNode);
        nodePositions[4] = GetNodePosition(4, middleNode);
        nodePositions[5] = GetNodePosition(5, lowerLeftNode);
        nodePositions[6] = GetNodePosition(6, lowerRightNode);
        nodePositions[7] = GetNodePosition(7, mergeNode);
        nodePositions[8] = GetNodePosition(8, goal != null ? goal.position : new Vector3(7f, 0f, 0f));
        nodePositions[9] = GetNodePosition(9, new Vector3(-1f, 1.2f, 0f));
        nodePositions[10] = GetNodePosition(10, new Vector3(1f, -1.2f, 0f));
        nodePositions[11] = GetNodePosition(11, new Vector3(-2f, 1.6f, 0f));
        nodePositions[12] = GetNodePosition(12, new Vector3(-2f, -1.6f, 0f));
        nodePositions[13] = GetNodePosition(13, new Vector3(1.5f, 1.6f, 0f));
        nodePositions[14] = GetNodePosition(14, new Vector3(1.5f, -1.6f, 0f));
        nodePositions[15] = GetNodePosition(15, new Vector3(-7f, 3.8f, 0f));
        nodePositions[16] = GetNodePosition(16, new Vector3(-7f, -3.8f, 0f));

        graph = new GrafoMA();
        graph.InicializarGrafo();
        for (int node = 1; node <= 16; node++)
            graph.AgregarVertice(node);

        // Ruta superior: 1 -> 2 -> 3 -> 7 -> 8.
        // Cada AgregarArista representa un tramo de camino que el enemigo puede recorrer.
        graph.AgregarArista(1, 2, GetDistanceCost(1, 2));
        graph.AgregarArista(2, 3, GetDistanceCost(2, 3));
        graph.AgregarArista(3, 7, GetDistanceCost(3, 7));

        // Nodo4 es una interseccion interna, no un atajo recto de Spawn a Base.
        // Solo se llega desde las ramas superior o inferior y se sale por Nodo9 o Nodo10.

        // Ruta inferior: 1 -> 5 -> 6 -> 7 -> 8.
        graph.AgregarArista(1, 5, GetDistanceCost(1, 5));
        graph.AgregarArista(5, 6, GetDistanceCost(5, 6));
        graph.AgregarArista(6, 7, GetDistanceCost(6, 7));

        // Conexiones extra: crean alternativas entre las tres ramas.
        graph.AgregarArista(2, 9, GetDistanceCost(2, 9));
        graph.AgregarArista(9, 7, GetDistanceCost(9, 7));
        graph.AgregarArista(4, 9, GetDistanceCost(4, 9));
        graph.AgregarArista(4, 10, GetDistanceCost(4, 10));
        graph.AgregarArista(5, 10, GetDistanceCost(5, 10));
        graph.AgregarArista(10, 7, GetDistanceCost(10, 7));

        // Cuatro nodos nuevos agregan caminos diagonales entre las ramas.
        graph.AgregarArista(2, 11, GetDistanceCost(2, 11));
        graph.AgregarArista(11, 4, GetDistanceCost(11, 4));
        graph.AgregarArista(11, 9, GetDistanceCost(11, 9));
        graph.AgregarArista(5, 12, GetDistanceCost(5, 12));
        graph.AgregarArista(12, 4, GetDistanceCost(12, 4));
        graph.AgregarArista(12, 10, GetDistanceCost(12, 10));
        graph.AgregarArista(3, 13, GetDistanceCost(3, 13));
        graph.AgregarArista(9, 13, GetDistanceCost(9, 13));
        graph.AgregarArista(13, 7, GetDistanceCost(13, 7));
        graph.AgregarArista(6, 14, GetDistanceCost(6, 14));
        graph.AgregarArista(10, 14, GetDistanceCost(10, 14));
        graph.AgregarArista(14, 7, GetDistanceCost(14, 7));

        // Entradas de las oleadas 2 y 3: Spawn superior e inferior.
        graph.AgregarArista(15, 2, GetDistanceCost(15, 2));
        graph.AgregarArista(15, 11, GetDistanceCost(15, 11));
        graph.AgregarArista(16, 5, GetDistanceCost(16, 5));
        graph.AgregarArista(16, 12, GetDistanceCost(16, 12));

        // El ultimo tramo es comun: desde la union hasta la Base.
        graph.AgregarArista(7, 8, GetDistanceCost(7, 8));
    }

    private static Vector3 GetNodePosition(int nodeNumber, Vector3 fallback)
    {
        GameObject node = GameObject.Find($"Nodo{nodeNumber}");
        return node != null ? node.transform.position : fallback;
    }

    // El peso de cada arista es la distancia real entre los dos nodos de la escena.
    // Si se mueve un nodo desde Unity, cambia este costo y Dijkstra vuelve a comparar rutas.
    private int GetDistanceCost(int from, int to)
    {
        return Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(nodePositions[from], nodePositions[to])));
    }

    private void RefreshRoadVisuals(List<int> selectedPath)
    {
        if (!Application.isPlaying)
            return;

        if (roadRoot == null)
        {
            GameObject root = new GameObject("Caminos del Grafo");
            root.transform.SetParent(transform, false);
            roadRoot = root.transform;
        }

        EnsureRoadMaterial();
        ConfigureRoadSegment(0, 1, 2, IsSelectedEdge(selectedPath, 1, 2));
        ConfigureRoadSegment(1, 2, 3, IsSelectedEdge(selectedPath, 2, 3));
        ConfigureRoadSegment(2, 3, 7, IsSelectedEdge(selectedPath, 3, 7));
        ConfigureRoadSegment(3, 1, 5, IsSelectedEdge(selectedPath, 1, 5));
        ConfigureRoadSegment(4, 5, 6, IsSelectedEdge(selectedPath, 5, 6));
        ConfigureRoadSegment(5, 6, 7, IsSelectedEdge(selectedPath, 6, 7));
        ConfigureRoadSegment(6, 7, 8, IsSelectedEdge(selectedPath, 7, 8));
        ConfigureRoadSegment(7, 2, 9, IsSelectedEdge(selectedPath, 2, 9));
        ConfigureRoadSegment(8, 9, 7, IsSelectedEdge(selectedPath, 9, 7));
        ConfigureRoadSegment(9, 4, 9, IsSelectedEdge(selectedPath, 4, 9));
        ConfigureRoadSegment(10, 4, 10, IsSelectedEdge(selectedPath, 4, 10));
        ConfigureRoadSegment(11, 5, 10, IsSelectedEdge(selectedPath, 5, 10));
        ConfigureRoadSegment(12, 10, 7, IsSelectedEdge(selectedPath, 10, 7));
        ConfigureRoadSegment(13, 2, 11, IsSelectedEdge(selectedPath, 2, 11));
        ConfigureRoadSegment(14, 11, 4, IsSelectedEdge(selectedPath, 11, 4));
        ConfigureRoadSegment(15, 11, 9, IsSelectedEdge(selectedPath, 11, 9));
        ConfigureRoadSegment(16, 5, 12, IsSelectedEdge(selectedPath, 5, 12));
        ConfigureRoadSegment(17, 12, 4, IsSelectedEdge(selectedPath, 12, 4));
        ConfigureRoadSegment(18, 12, 10, IsSelectedEdge(selectedPath, 12, 10));
        ConfigureRoadSegment(19, 3, 13, IsSelectedEdge(selectedPath, 3, 13));
        ConfigureRoadSegment(20, 9, 13, IsSelectedEdge(selectedPath, 9, 13));
        ConfigureRoadSegment(21, 13, 7, IsSelectedEdge(selectedPath, 13, 7));
        ConfigureRoadSegment(22, 6, 14, IsSelectedEdge(selectedPath, 6, 14));
        ConfigureRoadSegment(23, 10, 14, IsSelectedEdge(selectedPath, 10, 14));
        ConfigureRoadSegment(24, 14, 7, IsSelectedEdge(selectedPath, 14, 7));
        ConfigureRoadSegment(25, 15, 2, IsSelectedEdge(selectedPath, 15, 2));
        ConfigureRoadSegment(26, 15, 11, IsSelectedEdge(selectedPath, 15, 11));
        ConfigureRoadSegment(27, 16, 5, IsSelectedEdge(selectedPath, 16, 5));
        ConfigureRoadSegment(28, 16, 12, IsSelectedEdge(selectedPath, 16, 12));
    }

    private void ConfigureRoadSegment(int index, int from, int to, bool selected)
    {
        while (roadSegments.Count <= index)
        {
            GameObject segment = new GameObject("Tramo de camino");
            segment.transform.SetParent(roadRoot, false);
            LineRenderer line = segment.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = roadMaterial;
            line.numCornerVertices = 5;
            line.numCapVertices = 5;
            line.sortingOrder = -2;
            roadSegments.Add(line);
        }

        LineRenderer road = roadSegments[index];
        road.widthMultiplier = roadWidth;
        road.startColor = selected ? selectedRoadColor : inactiveRoadColor;
        road.endColor = selected ? selectedRoadColor : inactiveRoadColor;
        road.positionCount = 2;
        road.SetPosition(0, nodePositions[from]);
        road.SetPosition(1, nodePositions[to]);
    }

    private void EnsureRoadMaterial()
    {
        if (roadMaterial != null)
            return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        roadMaterial = new Material(shader);
    }

    private static bool IsSelectedEdge(List<int> path, int from, int to)
    {
        // Un tramo se pinta amarillo solo si sus dos nodos aparecen seguidos en la ruta de Dijkstra.
        if (path == null)
            return false;

        for (int index = 0; index < path.Count - 1; index++)
        {
            if (path[index] == from && path[index + 1] == to)
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        BuildGraph();
        DrawEdge(1, 2, Color.gray);
        DrawEdge(2, 3, Color.gray);
        DrawEdge(3, 7, Color.gray);
        DrawEdge(1, 5, Color.gray);
        DrawEdge(5, 6, Color.gray);
        DrawEdge(6, 7, Color.gray);
        DrawEdge(2, 9, Color.gray);
        DrawEdge(9, 7, Color.gray);
        DrawEdge(4, 9, Color.gray);
        DrawEdge(4, 10, Color.gray);
        DrawEdge(5, 10, Color.gray);
        DrawEdge(10, 7, Color.gray);
        DrawEdge(2, 11, Color.gray);
        DrawEdge(11, 4, Color.gray);
        DrawEdge(11, 9, Color.gray);
        DrawEdge(5, 12, Color.gray);
        DrawEdge(12, 4, Color.gray);
        DrawEdge(12, 10, Color.gray);
        DrawEdge(3, 13, Color.gray);
        DrawEdge(9, 13, Color.gray);
        DrawEdge(13, 7, Color.gray);
        DrawEdge(6, 14, Color.gray);
        DrawEdge(10, 14, Color.gray);
        DrawEdge(14, 7, Color.gray);
        DrawEdge(15, 2, Color.gray);
        DrawEdge(15, 11, Color.gray);
        DrawEdge(16, 5, Color.gray);
        DrawEdge(16, 12, Color.gray);
        DrawEdge(7, 8, Color.gray);

        foreach (KeyValuePair<int, Vector3> node in nodePositions)
        {
            Gizmos.color = node.Key == 1 ? Color.green : node.Key == 8 ? Color.red : Color.white;
            Gizmos.DrawSphere(node.Value, 0.12f);
        }
    }

    private void DrawEdge(int from, int to, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(nodePositions[from], nodePositions[to]);
    }

    private void OnDestroy()
    {
        if (roadMaterial != null)
            Destroy(roadMaterial);
    }
}
