using UnityEngine;
using UnityEngine.UIElements;

public class ContadorFPSUI : MonoBehaviour
{
    [Header("Configuracoes")]
    [SerializeField] private float tempoAtualizacao = 0.5f;

    private Label labelFPS;
    private int quantidadeQuadros = 0;
    private float acumuladorTempo = 0f;
    private float fpsAtual = 0f;

    private void OnEnable()
    {
        UIDocument documentoUI = GetComponent<UIDocument>();

        if (documentoUI != null)
        {
            labelFPS = documentoUI.rootVisualElement.Q<Label>("TextoFPS");
        }
    }

    private void Update()
    {
        acumuladorTempo += Time.unscaledDeltaTime;
        quantidadeQuadros++;

        if (acumuladorTempo >= tempoAtualizacao)
        {
            fpsAtual = quantidadeQuadros / acumuladorTempo;

            if (labelFPS != null)
            {
                labelFPS.text = "FPS: " + Mathf.RoundToInt(fpsAtual);
            }

            acumuladorTempo = 0f;
            quantidadeQuadros = 0;
        }
    }
}