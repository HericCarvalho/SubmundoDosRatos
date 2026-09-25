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

    private bool servicesProntos = false;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Netcode] NetworkManager não encontrado!");
            return;
        }

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
            $"[NETCODE] CLIENTE CONECTADO!\n" +
            $"ClientId: {clientId}\n" +
            $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
            $"IsServer: {NetworkManager.Singleton.IsServer}\n" +
            $"IsClient: {NetworkManager.Singleton.IsClient}\n" +
            $"IsListening: {NetworkManager.Singleton.IsListening}\n" +
            $"Cena atual: {SceneManager.GetActiveScene().name}"
        );

        // Somente o Host/Server deve mandar carregar a cena.
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log(
                $"[NETCODE] Cliente conectado. " +
                $"Carregando cena: {nomeCenaGameplay}"
            );

            var status = NetworkManager.Singleton.SceneManager.LoadScene(
                nomeCenaGameplay,
                LoadSceneMode.Single
            );

            Debug.Log(
                $"[NETCODE] Resultado do LoadScene: {status}"
            );
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogError(
            $"[NETCODE] CLIENTE DESCONECTADO!\n" +
            $"ClientId: {clientId}\n" +
            $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
            $"IsServer: {NetworkManager.Singleton.IsServer}\n" +
            $"IsClient: {NetworkManager.Singleton.IsClient}\n" +
            $"IsListening: {NetworkManager.Singleton.IsListening}\n" +
            $"DisconnectReason: {NetworkManager.Singleton.DisconnectReason}"
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
            bool iniciou =
                NetworkManager.Singleton.StartHost();

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
            Debug.LogError(
                "[Relay] NetworkManager não encontrado!"
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
                $"[Relay] NetworkManager já está ouvindo.\n" +
                $"IsHost: {NetworkManager.Singleton.IsHost}\n" +
                $"IsClient: {NetworkManager.Singleton.IsClient}\n" +
                $"IsServer: {NetworkManager.Singleton.IsServer}"
            );

            return;
        }

        PlayerPrefs.SetString(
            "ModoJogoAtual",
            "Multiplayer"
        );

        // IMPORTANTE:
        // Esse campo deve conter o JOIN CODE do Relay.
        //
        // Exemplo:
        // ABC123
        //
        // NÃO é IP.
        // NÃO é porta.
        // NÃO é 172.17.x.x.
        string codigoDigitado =
            inputIP != null
                ? inputIP.value.Trim().ToUpperInvariant()
                : "";

        if (string.IsNullOrEmpty(codigoDigitado))
        {
            Debug.LogWarning(
                "[Relay] Digite o código da sala."
            );

            return;
        }

        Debug.Log(
            $"[Relay] Tentando entrar com Join Code: {codigoDigitado}"
        );

        try
        {
            // 1. Entrar na alocação do Host.
            JoinAllocation alocacaoEntrada =
                await RelayService.Instance.JoinAllocationAsync(
                    codigoDigitado
                );

            Debug.Log(
                "[Relay] JoinAllocation recebido com sucesso!"
            );

            // 2. Obter UnityTransport.
            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError(
                    "[Relay] UnityTransport não encontrado no NetworkManager!"
                );

                return;
            }

            // 3. Configurar o Relay no transporte.
            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(
                    alocacaoEntrada,
                    "dtls"
                );

            transport.SetRelayServerData(relayData);

            Debug.Log(
                "[Relay] UnityTransport configurado para Client."
            );

            // 4. Iniciar Client.
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
                "[Relay] CLIENT iniciado com sucesso!"
            );

            Debug.Log(
                "[Relay] Aguardando conexão com o Host..."
            );
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
                $"[Netcode] Erro inesperado ao iniciar Client:\n{e}"
            );
        }
    }
}