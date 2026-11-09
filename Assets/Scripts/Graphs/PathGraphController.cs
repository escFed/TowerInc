using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Une el TDA Grafo con el nivel. Cada vertice representa un punto del camino.
public sealed class PathGraphController : MonoBehaviour
{
    [Header("Nodos intermedios del camino")]
    [SerializeField] private Vector3 upperLeftNode = new Vector3(-4f, 2f, 0f);
    [SerializeField] private Vector3 upperRightNode = new Vector3(0f, 2f, 0f);
    [SerializeField] private Vector3 lowerLeftNode = new Vector3(-3f, -2f, 0f);
    [SerializeField] private Vector3 lowerRightNode = new Vector3(2f, -2f, 0f);

    [Header("Decision de ruta")]
    [Tooltip("Una torreta dentro de este radio vuelve mas peligroso el tramo cercano.")]
    [SerializeField, Min(0.1f)] private float towerAvoidanceRadius = 2.1f;
    [SerializeField, Min(1)] private int costPerNearbyTower = 4;

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
        BuildGraph();
        route = new List<Vector3>();
        if (!AlgDijkstra.TryGetShortestPath(graph, 1, 6, out List<int> nodePath, out int cost))
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
        nodePositions[1] = spawn != null ? spawn.position : new Vector3(-7f, 0f, 0f);
        nodePositions[2] = upperLeftNode;
        nodePositions[3] = upperRightNode;
        nodePositions[4] = lowerLeftNode;
        nodePositions[5] = lowerRightNode;
        nodePositions[6] = goal != null ? goal.position : new Vector3(7f, 0f, 0f);

        graph = new GrafoMA();
        graph.InicializarGrafo();
        for (int node = 1; node <= 6; node++)
            graph.AgregarVertice(node);

        // Camino superior. El costo cambia segun las torretas cercanas.
        graph.AgregarArista(1, 2, GetEdgeCost(1, 2, 2));
        graph.AgregarArista(2, 3, GetEdgeCost(2, 3, 2));
        graph.AgregarArista(3, 6, GetEdgeCost(3, 6, 2));

        // Camino inferior alternativo, inicialmente mas largo.
        graph.AgregarArista(1, 4, GetEdgeCost(1, 4, 2));
        graph.AgregarArista(4, 5, GetEdgeCost(4, 5, 5));
        graph.AgregarArista(5, 6, GetEdgeCost(5, 6, 2));
    }

    // Esta es la funcion jugable de Dijkstra: una ruta con torretas cerca
    // aumenta su costo y los enemigos pueden elegir la otra rama del mapa.
    private int GetEdgeCost(int from, int to, int baseCost)
    {
        Vector3 middle = (nodePositions[from] + nodePositions[to]) * 0.5f;
        int nearbyTowers = 0;
        float radiusSquared = towerAvoidanceRadius * towerAvoidanceRadius;

        foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (tower != null && (tower.transform.position - middle).sqrMagnitude <= radiusSquared)
                nearbyTowers++;
        }

        return baseCost + nearbyTowers * costPerNearbyTower;
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
        ConfigureRoadSegment(2, 3, 6, IsSelectedEdge(selectedPath, 3, 6));
        ConfigureRoadSegment(3, 1, 4, IsSelectedEdge(selectedPath, 1, 4));
        ConfigureRoadSegment(4, 4, 5, IsSelectedEdge(selectedPath, 4, 5));
        ConfigureRoadSegment(5, 5, 6, IsSelectedEdge(selectedPath, 5, 6));
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
        DrawEdge(3, 6, Color.yellow);
        DrawEdge(1, 4, Color.gray);
        DrawEdge(4, 5, Color.gray);
        DrawEdge(5, 6, Color.gray);

        foreach (KeyValuePair<int, Vector3> node in nodePositions)
        {
            Gizmos.color = node.Key == 1 ? Color.green : node.Key == 6 ? Color.red : Color.white;
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
