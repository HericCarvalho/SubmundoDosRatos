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
        Debug.Log($"[Netcode] CLIENTE CONECTADO! ClientId = {clientId}");

        if (NetworkManager.Singleton == null)
            return;

        // HOST
        if (NetworkManager.Singleton.IsHost &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[Netcode] Host conectado. Aguardando clientes.");
            return;
        }

        // CLIENTE
        if (NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[Netcode] Cliente conectado ao Host!");

            // NÃO carregue a cena aqui.
            // O Host controla a mudança de cena pelo NetworkSceneManager.
        }
    }
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning(
            $"[Netcode] CLIENTE DESCONECTADO! ClientId = {clientId}"
        );

        if (NetworkManager.Singleton != null)
        {
            Debug.LogWarning(
                $"[Netcode] DisconnectReason: " +
                $"{NetworkManager.Singleton.DisconnectReason}"
            );
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
            Debug.LogError("[Relay] NetworkManager não encontrado!");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Relay] NetworkManager já está em execução.");
            return;
        }

        try
        {
            Debug.Log("[Relay] Criando alocação...");

            Allocation alocacao =
                await RelayService.Instance.CreateAllocationAsync(2);

            string codigoEntrada =
                await RelayService.Instance.GetJoinCodeAsync(
                    alocacao.AllocationId
                );

            Debug.Log($"[Relay] CÓDIGO DA SALA: {codigoEntrada}");

            if (inputPort != null)
                inputPort.value = codigoEntrada;

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("[Relay] UnityTransport não encontrado!");
                return;
            }

            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(alocacao, "dtls");

            transport.SetRelayServerData(relayData);

            Debug.Log("[Relay] UnityTransport configurado.");

            bool iniciou =
                NetworkManager.Singleton.StartHost();

            if (!iniciou)
            {
                Debug.LogError("[Relay] StartHost() falhou!");
                return;
            }

            Debug.Log("[Relay] HOST iniciado!");
            Debug.Log(
                $"[Netcode] Host ClientId: " +
                $"{NetworkManager.Singleton.LocalClientId}"
            );

            // IMPORTANTE:
            // Somente o HOST manda trocar a cena.
            if (NetworkManager.Singleton.SceneManager == null)
            {
                Debug.LogError(
                    "[Netcode] NetworkSceneManager não está disponível!"
                );
                return;
            }

            Debug.Log(
                $"[Netcode] Host carregando cena: {nomeCenaGameplay}"
            );

            var resultado =
                NetworkManager.Singleton.SceneManager.LoadScene(
                    nomeCenaGameplay,
                    LoadSceneMode.Single
                );

            Debug.Log(
                $"[Netcode] Resultado LoadScene: {resultado}"
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
        PlayerPrefs.Save();

        string codigoDigitado =
            inputIP != null
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
                "[Relay] NetworkManager já está conectado/ouvindo."
            );
            return;
        }

        try
        {
            Debug.Log("[Relay] Entrando na alocação...");

            JoinAllocation alocacaoEntrada =
                await RelayService.Instance.JoinAllocationAsync(
                    codigoDigitado
                );

            Debug.Log(
                "[Relay] JoinAllocation recebido com sucesso!"
            );

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError(
                    "[Relay] UnityTransport não encontrado!"
                );
                return;
            }

            RelayServerData relayData =
                AllocationUtils.ToRelayServerData(
                    alocacaoEntrada,
                    "dtls"
                );

            transport.SetRelayServerData(relayData);

            Debug.Log(
                "[Relay] UnityTransport configurado para Client."
            );

            bool iniciou =
                NetworkManager.Singleton.StartClient();

            Debug.Log(
                $"[Netcode] StartClient retornou: {iniciou}"
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
                "Aguardando conexão com o Host..."
            );

            // NÃO carregue CenaModoInfinito aqui.
            //
            // Quando o cliente realmente conectar,
            // o Host enviará a cena através do NetworkSceneManager.
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

}