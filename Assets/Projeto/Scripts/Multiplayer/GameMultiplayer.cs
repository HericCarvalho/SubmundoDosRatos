using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameMultiplayer : NetworkBehaviour
{
    public static GameMultiplayer Instance;
    [Header("Configurações da Partida")]
    [SerializeField] private float tempoDeEspera = 5f;

    private UIDocument uiDocument;
    private Label labelIniciar;

    public NetworkVariable<bool> JogoIniciado = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        VincularUIGameplay();
    }

    private void VincularUIGameplay()
    {
        uiDocument = Object.FindAnyObjectByType<UIDocument>();

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            labelIniciar = uiDocument.rootVisualElement.Q<Label>("Iniciar");

            AtualizarStatusLobby();
        }
    }

    public override void OnNetworkSpawn()
    {
        VincularUIGameplay();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += VerificarJogadores;
        }

        AtualizarStatusLobby();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= VerificarJogadores;
        }
    }

    private void VerificarJogadores(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2 && !JogoIniciado.Value)
        {
            StartCoroutine(RoutineContagemRegressiva());
        }
    }

    private IEnumerator RoutineContagemRegressiva()
    {
        float tempoRestante = tempoDeEspera;

        while (tempoRestante > 0)
        {
            AtualizarTextoClientRpc($"Preparados? {Mathf.CeilToInt(tempoRestante)}");
            yield return new WaitForSeconds(1f);
            tempoRestante--;
        }

        AtualizarTextoClientRpc("VÃO!");
        JogoIniciado.Value = true;

        yield return new WaitForSeconds(1f);
        OcultarTextoClientRpc();
    }

    [ClientRpc]
    private void AtualizarTextoClientRpc(string mensagem)
    {
        if (labelIniciar == null) VincularUIGameplay();
        if (labelIniciar != null) labelIniciar.text = mensagem;
    }

    [ClientRpc]
    private void OcultarTextoClientRpc()
    {
        if (labelIniciar == null) VincularUIGameplay();
        if (labelIniciar != null) labelIniciar.text = "";
    }

    private void AtualizarStatusLobby()
    {
        if (!JogoIniciado.Value)
        {
            if (labelIniciar == null) VincularUIGameplay();

            if (labelIniciar != null)
            {
                labelIniciar.text = "Aguardando outro jogador...";
            }
        }
    }
}