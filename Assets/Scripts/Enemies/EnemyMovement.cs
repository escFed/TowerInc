using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private Transform destination;

    private EnemyHealth health;
    private readonly List<Vector3> route = new List<Vector3>();
    private int nextRouteNode;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    public void SetDestination(Transform target)
    {
        route.Clear();
        nextRouteNode = 0;
        destination = target;
    }

    // El grafo entrega una lista de puntos; el enemigo los recorre en orden.
    public void SetRoute(IReadOnlyList<Vector3> newRoute)
    {
        route.Clear();
        nextRouteNode = 0;
        destination = null;

        if (newRoute == null)
            return;

        for (int index = 0; index < newRoute.Count; index++)
            route.Add(newRoute[index]);

        // El primer nodo coincide con el Spawn, asi que se omite si ya llego ahi.
        if (route.Count > 0 && (transform.position - route[0]).sqrMagnitude <= 0.0025f)
            nextRouteNode = 1;
    }

    private void Update()
    {
        if (!health.IsAlive)
            return;

        if (nextRouteNode < route.Count)
        {
            MoveTo(route[nextRouteNode]);
            return;
        }

        // Conserva el movimiento recto como respaldo si no se configura el grafo.
        if (destination != null)
            MoveTo(destination.position);
    }

    private void MoveTo(Vector3 target)
    {
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Max(0, speed) * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.0025f)
        {
            if (nextRouteNode < route.Count)
            {
                nextRouteNode++;
                if (nextRouteNode >= route.Count)
                    health.ReachDestination();
            }
            else
            {
                health.ReachDestination();
            }
        }
    }
}
