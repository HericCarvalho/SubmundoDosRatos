using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MultiplayerControlador : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Cena de Gameplay")]
    [SerializeField] private string nomeCenaGameplay = "CenaModoInfinito";

    private TextField inputIP;
    private Button btnCriar;
    private Button btnEntrar;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        MapearElementos(root);
        ConfigurarBotoes();
    }

    private void MapearElementos(VisualElement root)
    {
        inputIP = root.Q<TextField>("InputIP");
        btnCriar = root.Q<Button>("Criar");
        btnEntrar = root.Q<Button>("Entrar");
    }

    private void ConfigurarBotoes()
    {
        if (inputIP != null)
            inputIP.value = "127.0.0.1";

        if (btnCriar != null)
            btnCriar.clicked += CriarSalaHost;

        if (btnEntrar != null)
            btnEntrar.clicked += EntrarComoCliente;
    }

    public void CriarSalaHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager não encontrado na cena!");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        bool hostIniciadoComSucesso = NetworkManager.Singleton.StartHost();

        if (!hostIniciadoComSucesso)
        {
            Debug.LogError("Falha ao iniciar o Host. Verifique se a porta 7777 já está em uso por outro processo.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(nomeCenaGameplay, LoadSceneMode.Single);
    }

    private void EntrarComoCliente()
    {
        PlayerPrefs.SetString("ModoJogoAtual", "Multiplayer");

        if (NetworkManager.Singleton != null)
        {
            string ipDigitado = inputIP != null ? inputIP.value : "127.0.0.1";

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = ipDigitado;
            }

            NetworkManager.Singleton.StartClient();
        }
    }
}