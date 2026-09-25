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

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        Inicializar();
    }

    private async void Inicializar()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("[Relay] Unity Services inicializado.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Erro ao inicializar: {e}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Netcode] CLIENTE CONECTADO! ClientId = {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogError($"[Netcode] CLIENTE DESCONECTADO! ClientId = {clientId}");
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
            Debug.LogError("[Relay] NetworkManager não encontrado na cena!");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "[Relay] O NetworkManager já está em execução. " +
                "Não é possível criar outro Host."
            );

            return;
        }

        try
        {
            // Criar a alocação no Relay
            Allocation alocacao =
                await RelayService.Instance.CreateAllocationAsync(2);

            // Gerar o código que será enviado ao outro jogador
            string codigoEntrada =
                await RelayService.Instance.GetJoinCodeAsync(
                    alocacao.AllocationId
                );

            Debug.Log(
                $"[Relay] CÓDIGO DA SALA GERADO: {codigoEntrada}"
            );

            // Mostra o Join Code na UI
            if (inputPort != null)
            {
                inputPort.value = codigoEntrada;
            }

            // Configurar UnityTransport
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError(
                    "[Relay] UnityTransport não encontrado no NetworkManager!"
                );

                return;
            }

            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(alocacao, "dtls");

            transport.SetRelayServerData(relayData);

            Debug.Log("[Relay] UnityTransport configurado para Host.");

            // Iniciar Host
            bool iniciou =
                NetworkManager.Singleton.StartHost();

            if (!iniciou)
            {
                Debug.LogError("[Relay] Falha ao iniciar o Host.");
                return;
            }

            Debug.Log("[Relay] Host iniciado com sucesso!");

            // Carregar a cena pelo NetworkManager
            NetworkManager.Singleton.SceneManager.LoadScene(
                nomeCenaGameplay,
                LoadSceneMode.Single
            );
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(
                $"[Relay] Erro ao criar sala:\n{e}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[Netcode] Erro inesperado:\n{e}"
            );
        }
    }

    private async Task EntrarComoCliente()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Relay] NetworkManager não encontrado!");
            return;
        }

        PlayerPrefs.SetString("ModoJogoAtual", "Multiplayer");

        // Esse campo deve conter o JOIN CODE do Relay.
        string codigoDigitado = inputIP != null
            ? inputIP.value.Trim().ToUpperInvariant()
            : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            Debug.LogWarning("[Relay] Digite o código da sala.");
            return;
        }

        Debug.Log($"[Relay] Tentando entrar com Join Code: {codigoDigitado}");

        // Não tente iniciar outro cliente/host se já existe
        // uma sessão de Netcode ativa.
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                $"[Relay] NetworkManager já está ouvindo. " +
                $"IsHost={NetworkManager.Singleton.IsHost}, " +
                $"IsClient={NetworkManager.Singleton.IsClient}, " +
                $"IsServer={NetworkManager.Singleton.IsServer}"
            );

            return;
        }

        try
        {
            // 1. Entrar na alocação do Relay
            JoinAllocation alocacaoEntrada =
                await RelayService.Instance.JoinAllocationAsync(codigoDigitado);

            Debug.Log("[Relay] JoinAllocation recebido com sucesso.");

            // 2. Configurar o UnityTransport
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("[Relay] UnityTransport não encontrado no NetworkManager!");
                return;
            }

            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(alocacaoEntrada, "dtls");

            transport.SetRelayServerData(relayData);

            Debug.Log("[Relay] UnityTransport configurado.");

            // 3. Iniciar o cliente
            bool iniciou = NetworkManager.Singleton.StartClient();

            Debug.Log($"[Netcode] StartClient retornou: {iniciou}");
            Debug.Log($"[Netcode] IsClient: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"[Netcode] IsListening: {NetworkManager.Singleton.IsListening}");

            if (!iniciou)
            {
                Debug.LogError(
                    $"[Relay] StartClient() falhou. " +
                    $"IsListening={NetworkManager.Singleton.IsListening}"
                );

                return;
            }

            Debug.Log("[Relay] Cliente iniciado com sucesso.");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(
                $"[Relay] Erro ao entrar na sala:\n{e}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"[Netcode] Erro inesperado ao iniciar cliente:\n{e}"
            );
        }
    }
}