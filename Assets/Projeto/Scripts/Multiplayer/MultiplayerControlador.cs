using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
    private TextField inputPort;
    private Button btnCriar;
    private Button btnEntrar;

    private bool servicesProntos = false;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnNetworkSceneLoaded;
        }

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

            servicesProntos = true;

            Debug.Log(
                $"[Relay] Unity Services inicializado! " +
                $"PlayerId: {AuthenticationService.Instance.PlayerId}"
            );
        }
        catch (System.Exception e)
        {
            servicesProntos = false;

            Debug.LogError(
                $"[Relay] Erro ao inicializar Unity Services:\n{e}"
            );
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log(
            $"[NETCODE] ========================================\n" +
            $"[NETCODE] CLIENTE CONECTADO!\n" +
            $"[NETCODE] ClientId: {clientId}\n" +
            $"[NETCODE] IsClient: {NetworkManager.Singleton.IsClient}\n" +
            $"[NETCODE] IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}\n" +
            $"[NETCODE] IsHost: {NetworkManager.Singleton.IsHost}\n" +
            $"[NETCODE] IsServer: {NetworkManager.Singleton.IsServer}\n" +
            $"[NETCODE] Cena atual: {SceneManager.GetActiveScene().name}\n" +
            $"[NETCODE] ========================================"
        );

        if (NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[NETCODE] Cliente conectado ao Host. Aguardando sincronização da cena...");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string motivo = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.DisconnectReason
            : "NetworkManager inexistente";

        Debug.LogError(
            $"[NETCODE] ========================================\n" +
            $"[NETCODE] CLIENTE DESCONECTADO!\n" +
            $"[NETCODE] ClientId: {clientId}\n" +
            $"[NETCODE] Motivo: {motivo}\n" +
            $"[NETCODE] IsClient: {NetworkManager.Singleton?.IsClient}\n" +
            $"[NETCODE] IsListening: {NetworkManager.Singleton?.IsListening}\n" +
            $"[NETCODE] ========================================"
        );
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
            Debug.LogError(
                "[Relay] NetworkManager não encontrado na cena!"
            );

            return;
        }

        if (!servicesProntos)
        {
            Debug.LogWarning(
                "[Relay] Unity Services ainda não terminou de inicializar."
            );

            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                "[Relay] O NetworkManager já está em execução."
            );

            return;
        }

        try
        {
            Debug.Log("[Relay] Criando alocação...");

            // 1. Criar a alocação no Relay.
            // 2 = Host + 1 jogador.
            Allocation alocacao =
                await RelayService.Instance.CreateAllocationAsync(2);

            Debug.Log("[Relay] Allocation criada.");

            // Gerar Join Code.
            string codigoEntrada =
                await RelayService.Instance.GetJoinCodeAsync(
                    alocacao.AllocationId
                );

            Debug.Log(
                $"[Relay] CÓDIGO DA SALA: {codigoEntrada}"
            );

            // Mostrar Join Code na UI.
            if (inputPort != null)
            {
                inputPort.value = codigoEntrada;
            }

            // Obter UnityTransport.
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError(
                    "[Relay] UnityTransport não encontrado no NetworkManager!"
                );

                return;
            }

            // Configurar Relay no transporte.
            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(
                    alocacao,
                    "dtls"
                );

            transport.SetRelayServerData(relayData);

            Debug.Log(
                "[Relay] UnityTransport configurado para Host."
            );

            // Iniciar Host.
            bool iniciou = NetworkManager.Singleton.StartHost();

            if (!iniciou)
            {
                Debug.LogError("[Relay] Falha ao iniciar o Host.");
                return;
            }

            Debug.Log(
                $"[HOST] ========================================\n" +
                $"[HOST] HOST INICIADO\n" +
                $"[HOST] IsHost: {NetworkManager.Singleton.IsHost}\n" +
                $"[HOST] IsServer: {NetworkManager.Singleton.IsServer}\n" +
                $"[HOST] IsClient: {NetworkManager.Singleton.IsClient}\n" +
                $"[HOST] IsListening: {NetworkManager.Singleton.IsListening}\n" +
                $"[HOST] LocalClientId: {NetworkManager.Singleton.LocalClientId}\n" +
                $"[HOST] ========================================"
            );

            if (!iniciou)
            {
                Debug.LogError(
                    "[Relay] StartHost() retornou FALSE."
                );

                return;
            }

            Debug.Log(
                "[Relay] HOST iniciado com sucesso!"
            );

            Debug.Log(
                $"[Netcode] IsHost = {NetworkManager.Singleton.IsHost}"
            );

            Debug.Log(
                $"[Netcode] IsServer = {NetworkManager.Singleton.IsServer}"
            );

            Debug.Log(
                $"[Netcode] IsListening = {NetworkManager.Singleton.IsListening}"
            );

            Debug.Log(
                "[Relay] Aguardando o Client entrar..."
            );

            // NÃO carregamos a cena aqui.
            //
            // A cena será carregada no OnClientConnected(),
            // quando o Client realmente conectar.
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
                $"[Netcode] Erro inesperado ao criar Host:\n{e}"
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

        string codigoDigitado = inputIP != null
            ? inputIP.value.Trim().ToUpperInvariant()
            : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            Debug.LogWarning("[Relay] Digite o código da sala.");
            return;
        }

        Debug.Log(
            $"[Relay] Tentando entrar com Join Code: {codigoDigitado}"
        );

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning(
                $"[Relay] NetworkManager já está ouvindo.\n" +
                $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
                $"IsClient: {NetworkManager.Singleton.IsClient}\n" +
                $"IsServer: {NetworkManager.Singleton.IsServer}"
            );

            return;
        }

        try
        {
            // =========================================================
            // 1. ENTRAR NO RELAY
            // =========================================================

            JoinAllocation alocacaoEntrada =
                await RelayService.Instance.JoinAllocationAsync(
                    codigoDigitado
                );

            Debug.Log(
                "[Relay] JoinAllocation recebido com sucesso!"
            );

            // =========================================================
            // 2. PEGAR UNITY TRANSPORT
            // =========================================================

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError(
                    "[Relay] UnityTransport não encontrado!"
                );

                return;
            }

            // =========================================================
            // 3. CONFIGURAR RELAY
            // =========================================================

            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(
                    alocacaoEntrada,
                    "dtls"
                );

            transport.SetRelayServerData(relayData);

            Debug.Log(
                "[Relay] UnityTransport configurado para Client."
            );

            // =========================================================
            // 4. INICIAR CLIENT
            // =========================================================

            bool iniciou =
                NetworkManager.Singleton.StartClient();

            Debug.Log(
                $"[Netcode] StartClient retornou: {iniciou}"
            );

            Debug.Log(
                $"[Netcode] IsClient: " +
                $"{NetworkManager.Singleton.IsClient}"
            );

            Debug.Log(
                $"[Netcode] IsListening: " +
                $"{NetworkManager.Singleton.IsListening}"
            );

            if (!iniciou)
            {
                Debug.LogError(
                    "[Netcode] StartClient() falhou!"
                );

                return;
            }

            Debug.Log(
                "[Relay] CLIENT iniciado. " +
                "Aguardando conexão real com o Host..."
            );

            // =========================================================
            // 5. NÃO TROQUE A CENA AQUI
            // =========================================================

            // IMPORTANTE:
            // O cliente NÃO deve chamar LoadScene.
            //
            // Quem manda a cena é o Host através do
            // NetworkSceneManager.
            //
            // Portanto não coloque:
            //
            // SceneManager.LoadScene(...)
            //
            // aqui.
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
                $"[Netcode] Erro inesperado:\n{e}"
            );
        }
    }
    private void OnNetworkSceneLoaded(
    string sceneName,
    LoadSceneMode loadSceneMode,
    List<ulong> clientsCompleted,
    List<ulong> clientsTimedOut)
    {
        Debug.Log(
            $"[NETCODE] ========================================\n" +
            $"[NETCODE] CENA CARREGADA PELA REDE\n" +
            $"[NETCODE] Cena: {sceneName}\n" +
            $"[NETCODE] Clientes concluíram: {clientsCompleted.Count}\n" +
            $"[NETCODE] Clientes timeout: {clientsTimedOut.Count}\n" +
            $"[NETCODE] ========================================"
        );
    }

}