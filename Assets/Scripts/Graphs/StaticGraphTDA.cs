using System;
using System.Collections.Generic;

// TDA Conjunto minimo que usa el grafo para guardar los nodos que lo forman.
// Se mantiene separado de la cola y la pila: cada estructura resuelve su propia tarea.
public interface IConjuntoTDA
{
    void InicializarConjunto();
    bool ConjuntoVacio();
    void Agregar(int value);
    bool Pertenece(int value);
    int Elegir();
    void Sacar(int value);
}

internal sealed class NodoConjunto
{
    public int Value;
    public NodoConjunto Next;
}

public sealed class ConjuntoLD : IConjuntoTDA
{
    private NodoConjunto first;

    public void InicializarConjunto() => first = null;
    public bool ConjuntoVacio() => first == null;

    public void Agregar(int value)
    {
        if (Pertenece(value))
            return;

        first = new NodoConjunto { Value = value, Next = first };
    }

    public bool Pertenece(int value)
    {
        for (NodoConjunto current = first; current != null; current = current.Next)
        {
            if (current.Value == value)
                return true;
        }

        return false;
    }

    public int Elegir()
    {
        if (ConjuntoVacio())
            throw new InvalidOperationException("No se puede elegir de un conjunto vacio.");

        return first.Value;
    }

    public void Sacar(int value)
    {
        NodoConjunto previous = null;
        NodoConjunto current = first;

        while (current != null && current.Value != value)
        {
            previous = current;
            current = current.Next;
        }

        if (current == null)
            return;

        if (previous == null)
            first = current.Next;
        else
            previous.Next = current.Next;
    }
}

// Grafo estatico representado con matriz de adyacencia, como el TDA visto en clase.
public interface IGrafoTDA
{
    void InicializarGrafo();
    void AgregarVertice(int vertex);
    void AgregarArista(int origin, int destination, int cost);
    bool ExisteArista(int origin, int destination);
    int PesoArista(int origin, int destination);
    IConjuntoTDA Vertices();
}

public sealed class GrafoMA : IGrafoTDA
{
    // Se usa un valor grande como "infinito": significa que dos nodos no tienen conexion directa.
    public const int Infinity = int.MaxValue / 4;

    private readonly int[,] adjacency = new int[32, 32];
    private readonly int[] labels = new int[32];
    private int vertexCount;

    public int VertexCount => vertexCount;

    public void InicializarGrafo()
    {
        vertexCount = 0;
        for (int row = 0; row < adjacency.GetLength(0); row++)
        {
            for (int column = 0; column < adjacency.GetLength(1); column++)
                adjacency[row, column] = Infinity;
        }
    }

    public void AgregarVertice(int vertex)
    {
        if (FindIndex(vertex) >= 0)
            return;
        if (vertexCount >= labels.Length)
            throw new InvalidOperationException("El grafo estatico alcanzo su cantidad maxima de nodos.");

        labels[vertexCount++] = vertex;
    }

    public void AgregarArista(int origin, int destination, int cost)
    {
        int originIndex = FindIndex(origin);
        int destinationIndex = FindIndex(destination);
        if (originIndex < 0 || destinationIndex < 0)
            throw new InvalidOperationException("Los dos vertices deben existir antes de agregar una arista.");
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Dijkstra requiere costos no negativos.");

        // Fila = nodo de origen; columna = nodo destino; valor = costo/distancia del tramo.
        adjacency[originIndex, destinationIndex] = cost;
    }

    public bool ExisteArista(int origin, int destination) => PesoArista(origin, destination) < Infinity;

    public int PesoArista(int origin, int destination)
    {
        int originIndex = FindIndex(origin);
        int destinationIndex = FindIndex(destination);
        return originIndex < 0 || destinationIndex < 0 ? Infinity : adjacency[originIndex, destinationIndex];
    }

    public IConjuntoTDA Vertices()
    {
        IConjuntoTDA vertices = new ConjuntoLD();
        vertices.InicializarConjunto();
        for (int index = 0; index < vertexCount; index++)
            vertices.Agregar(labels[index]);
        return vertices;
    }

    public int VertexAt(int index) => labels[index];

    private int FindIndex(int vertex)
    {
        for (int index = 0; index < vertexCount; index++)
        {
            if (labels[index] == vertex)
                return index;
        }

        return -1;
    }
}

// Implementacion de Dijkstra: devuelve los nodos de menor costo entre origen y destino.
public static class AlgDijkstra
{
    public static bool TryGetShortestPath(GrafoMA graph, int origin, int destination, out List<int> path, out int totalCost)
    {
        path = new List<int>();
        totalCost = GrafoMA.Infinity;
        if (graph == null || graph.VertexCount == 0)
            return false;

        int count = graph.VertexCount;
        // distance: menor costo conocido desde el origen hasta cada nodo.
        // previous: nodo anterior para reconstruir la ruta final.
        // visited: evita volver a evaluar un nodo cuyo mejor costo ya esta decidido.
        int[] distance = new int[count];
        int[] previous = new int[count];
        bool[] visited = new bool[count];
        int originIndex = FindVertexIndex(graph, origin);
        int destinationIndex = FindVertexIndex(graph, destination);
        if (originIndex < 0 || destinationIndex < 0)
            return false;

        for (int index = 0; index < count; index++)
        {
            distance[index] = GrafoMA.Infinity;
            previous[index] = -1;
        }

        distance[originIndex] = 0;
        for (int step = 0; step < count; step++)
        {
            // Se elige el nodo pendiente con menor distancia acumulada.
            int current = -1;
            for (int index = 0; index < count; index++)
            {
                if (!visited[index] && (current < 0 || distance[index] < distance[current]))
                    current = index;
            }

            if (current < 0 || distance[current] == GrafoMA.Infinity)
                break;

            visited[current] = true;
            if (current == destinationIndex)
                break;

            for (int neighbor = 0; neighbor < count; neighbor++)
            {
                int edgeCost = graph.PesoArista(graph.VertexAt(current), graph.VertexAt(neighbor));
                if (visited[neighbor] || edgeCost == GrafoMA.Infinity)
                    continue;

                // Relajacion: se prueba si pasar por "current" mejora el costo del vecino.
                int candidateDistance = distance[current] + edgeCost;
                if (candidateDistance < distance[neighbor])
                {
                    distance[neighbor] = candidateDistance;
                    previous[neighbor] = current;
                }
            }
        }

        if (distance[destinationIndex] == GrafoMA.Infinity)
            return false;

        // Se vuelve desde destino hacia origen usando previous[] y luego se invierte la lista.
        for (int current = destinationIndex; current >= 0; current = previous[current])
            path.Add(graph.VertexAt(current));
        path.Reverse();
        totalCost = distance[destinationIndex];
        return true;
    }

    private static int FindVertexIndex(GrafoMA graph, int vertex)
    {
        for (int index = 0; index < graph.VertexCount; index++)
        {
            if (graph.VertexAt(index) == vertex)
                return index;
        }

        return -1;
    }
}
