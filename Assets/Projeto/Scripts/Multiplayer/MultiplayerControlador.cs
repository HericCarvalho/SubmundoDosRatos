using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class MultiplayerControlador : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Cena de Gameplay")]
    [SerializeField] private string nomeCenaGameplay = "CenaModoInfinito";

    private TextField inputIP;
    private TextField inputPort;
    private Button btnCriar;
    private Button btnEntrar;

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Relay] Autenticado com sucesso! PlayerID: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro na autenticação do Unity Services: {e.Message}");
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        MapearElementos(root);
        ConfigurarBotoes();
    }

    private void MapearElementos(VisualElement root)
    {
        inputIP = root.Q<TextField>("InputIP");
        inputPort = root.Q<TextField>("InputPort");

        btnCriar = root.Q<Button>("Criar");
        btnEntrar = root.Q<Button>("Entrar");
    }

    private void ConfigurarBotoes()
    {
        if (btnCriar != null)
            btnCriar.clicked += async () => await CriarSalaHost();

        if (btnEntrar != null)
            btnEntrar.clicked += async () => await EntrarComoCliente();
    }

    public async Task CriarSalaHost()
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

        try
        {
            Allocation alocacao = await RelayService.Instance.CreateAllocationAsync(2);

            string codigoEntrada = await RelayService.Instance.GetJoinCodeAsync(alocacao.AllocationId);

            Debug.Log($"[Relay] CÓDIGO DA SALA GERADO: {codigoEntrada}");

            if (inputPort != null)
            {
                inputPort.value = codigoEntrada;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                RelayServerData relayData = AllocationUtils.ToRelayServerData(alocacao, "dtls");
                transport.SetRelayServerData(relayData);
            }

            bool hostIniciadoComSucesso = NetworkManager.Singleton.StartHost();

            if (hostIniciadoComSucesso)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(nomeCenaGameplay, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("Falha ao iniciar o Host.");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Erro ao criar sala no Relay: {e.Message}");
        }
    }

    private async Task EntrarComoCliente()
    {
        if (NetworkManager.Singleton == null) return;

        PlayerPrefs.SetString("ModoJogoAtual", "Multiplayer");

        string codigoDigitado = inputIP != null ? inputIP.value.Trim() : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            Debug.LogWarning("Por favor, digite o código da sala no campo IP.");
            return;
        }

        try
        {
            JoinAllocation alocacaoEntrada = await RelayService.Instance.JoinAllocationAsync(codigoDigitado);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                RelayServerData relayData = AllocationUtils.ToRelayServerData(alocacaoEntrada, "dtls");
                transport.SetRelayServerData(relayData);
            }

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Não foi possível conectar com o código informado: {e.Message}");
        }
    }
}