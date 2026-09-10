using UnityEngine;

// Panel de practica provisional; no es la interfaz final del juego.
public class CombatPrototypeHUD : MonoBehaviour
{
    [SerializeField] private WaveController wave;

    private void OnGUI()
    {
        if (wave == null)
            return;

        GUILayout.BeginArea(new Rect(16, 16, 290, 210), GUI.skin.box);
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
        GUILayout.EndArea();
    }
}
