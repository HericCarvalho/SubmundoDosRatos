using UnityEngine;

public class ContadorFPSUI : MonoBehaviour
{
    private float tempoAcumulado = 0f;
    private int quadrosContados = 0;
    private float fpsAtual = 0f;

    private void Update()
    {
        bool mostrar = PlayerPrefs.GetInt("MostrarFPS", 0) == 1;
        if (!mostrar) return;

        tempoAcumulado += Time.unscaledDeltaTime;
        quadrosContados++;

        if (tempoAcumulado >= 0.5f)
        {
            fpsAtual = quadrosContados / tempoAcumulado;
            tempoAcumulado = 0f;
            quadrosContados = 0;
        }
    }

    private void OnGUI()
    {
        bool mostrar = PlayerPrefs.GetInt("MostrarFPS", 0) == 1;
        if (!mostrar) return;

        GUIStyle estilo = new GUIStyle();
        estilo.fontSize = 20;
        estilo.normal.textColor = Color.yellow;

        GUI.Label(new Rect(10, 10, 150, 30), $"FPS: {Mathf.Ceil(fpsAtual)}", estilo);
    }
}