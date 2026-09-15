using UnityEngine;
using UnityEngine.InputSystem;

// Panel de practica provisional; no es la interfaz final del juego.
public class CombatPrototypeHUD : MonoBehaviour
{
    // Controla el estado de las oleadas y los datos que se muestran en el HUD.
    [SerializeField] private WaveController wave;
    // Administra la pila de torretas que el jugador puede colocar.
    [SerializeField] private TowerStackController towerStack;

    // Indica que el jugador pulso el boton y esta esperando elegir una posicion en el mapa.
    private bool placingTower;

    private void Awake()
    {
        // Si no se asigno desde el Inspector, se busca el componente en este objeto.
        if (towerStack == null)
            towerStack = GetComponent<TowerStackController>();

        // La escena original no tenia inventario de torretas: se agrega al HUD al iniciar.
        if (towerStack == null)
            towerStack = gameObject.AddComponent<TowerStackController>();
    }

    private void Update()
    {
        // Solo se lee el clic cuando el jugador eligio colocar una torreta.
        if (!placingTower || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // La camara convierte las coordenadas del cursor de pantalla al mundo 2D.
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 position = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        position.z = 0;

        // Si se coloco correctamente, se sale del modo de colocacion.
        if (towerStack.TryPlaceTopTower(position))
            placingTower = false;
    }

    private void OnGUI()
    {
        if (wave == null)
            return;

        GUILayout.BeginArea(new Rect(16, 16, 310, 260), GUI.skin.box);
        GUILayout.Label("PROTOTIPO: TORRE + ENEMIGOS");
        GUILayout.Label("Una ruta recta / enemigo basico");
        GUILayout.Space(8);
        GUILayout.Label($"Pendientes en cola: {wave.PendingCount}");
        GUILayout.Label($"Enemigos activos: {wave.ActiveCount}");
        GUILayout.Label($"Eliminados: {wave.KilledCount} / Llegaron: {wave.EscapedCount}");
        GUILayout.Label($"Oleadas completadas: {wave.CompletedWaves}");
        GUILayout.Space(8);
        GUI.enabled = !wave.IsRunning;
        if (GUILayout.Button("Iniciar oleada"))
            wave.StartWave();
        GUI.enabled = true;
        GUILayout.Label(wave.IsRunning ? "Oleada en curso..." : "Listo para iniciar.");
        GUILayout.Space(10);
        // El contador permite comprobar visualmente cuantos elementos quedan en la pila.
        GUILayout.Label($"Torretas basicas en pila: {towerStack.AvailableCount}");
        GUI.enabled = towerStack.HasTowers;
        // Este boton activa el siguiente clic valido sobre el mapa.
        if (GUILayout.Button(placingTower ? "Hace click en el mapa..." : "Colocar torreta basica"))
            placingTower = towerStack.HasTowers;
        GUI.enabled = true;
        if (placingTower)
            GUILayout.Label("Se quitara una torreta del tope de la pila.");
        GUILayout.EndArea();
    }
}
