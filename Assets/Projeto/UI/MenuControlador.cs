using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Configuracoes FMOD (Caminho VCA / Bus)")]
    [SerializeField] private string vcaMusicaPath = "vca:/Musica";
    [SerializeField] private string vcaSFXPath = "vca:/SFX";

    [Header("Cenas")]
    [SerializeField] private string nomeCenaModoInfinito = "CenaModoInfinito";

    private VisualElement menuPrincipal;
    private VisualElement containerBotoesPrincipais;
    private VisualElement panelModoInfinito;
    private VisualElement panelOpcoes;
    private VisualElement panelSavesHistoria;
    private VisualElement panelCreditos;

    private Button btnModoHistoria;
    private Button btnModoInfinito;
    private Button btnOpcoes;
    private Button btnCreditos;
    private Button btnSair;

    private Slider sliderMusica;
    private Slider sliderSFX;
    private Toggle toggleFPS;
    private DropdownField dropdownResolucao;
    private Button btnVoltarOpcoes;

    private Button btnSinglePlayer;
    private Button btnMultiplayer;
    private Button btnVoltarInfinito;
    private Label labelPontuacaoSingle;
    private Label labelPontuacaoMulti;

    private Resolution[] resolucoesDisponiveis;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        MapearElementos(root);
        ConfigurarBotoes();
        ConfigurarOpcoes();
        ConfigurarModoInfinito();

        VoltarParaBotoesPrincipais();
    }

    private void MapearElementos(VisualElement root)
    {
        menuPrincipal = root.Q<VisualElement>("MenuPrincipal");

        containerBotoesPrincipais = root.Q<VisualElement>("UI");
        if (containerBotoesPrincipais == null)
            containerBotoesPrincipais = root.Q<VisualElement>("Botoes");

        panelModoInfinito = root.Q<VisualElement>("UI_ModoInfinito");
        panelOpcoes = root.Q<VisualElement>("UI_Opcoes");
        panelSavesHistoria = root.Q<VisualElement>("UI_SavesHistoria");
        panelCreditos = root.Q<VisualElement>("UI_Creditos");

        btnModoHistoria = root.Q<Button>("ModoHistoria");
        btnModoInfinito = root.Q<Button>("ModoInfinito");
        btnOpcoes = root.Q<Button>("Opcoes");
        btnCreditos = root.Q<Button>("Creditos");
        btnSair = root.Q<Button>("Sair");

        sliderMusica = root.Q<Slider>("SliderMusica");
        sliderSFX = root.Q<Slider>("SliderSFX");
        toggleFPS = root.Q<Toggle>("MostrarFPS");
        dropdownResolucao = root.Q<DropdownField>("MudarResolucao");

        btnSinglePlayer = root.Q<Button>("SinglePlayer");
        btnMultiplayer = root.Q<Button>("Multiplayer");
        labelPontuacaoSingle = root.Q<Label>("PontuacaoSingle");
        labelPontuacaoMulti = root.Q<Label>("PontuacaoMulti");

        if (panelOpcoes != null)
            btnVoltarOpcoes = panelOpcoes.Q<Button>("Voltar");

        if (panelModoInfinito != null)
            btnVoltarInfinito = panelModoInfinito.Q<Button>("Voltar");
    }

    private void ConfigurarBotoes()
    {
        if (btnModoHistoria != null)
            btnModoHistoria.clicked += () => AbrirSubPainel(panelSavesHistoria);

        if (btnModoInfinito != null)
            btnModoInfinito.clicked += () => {
                AtualizarPontuacoesInfinito();
                AbrirSubPainel(panelModoInfinito);
            };

        if (btnOpcoes != null)
            btnOpcoes.clicked += () => AbrirSubPainel(panelOpcoes);

        if (btnCreditos != null)
            btnCreditos.clicked += () => AbrirSubPainel(panelCreditos);

        if (btnSair != null)
            btnSair.clicked += SairDoJogo;

        if (btnVoltarOpcoes != null)
            btnVoltarOpcoes.clicked += VoltarParaBotoesPrincipais;
    }

    private void ConfigurarModoInfinito()
    {
        if (btnSinglePlayer != null)
            btnSinglePlayer.clicked += IniciarSinglePlayerInfinito;

        if (btnMultiplayer != null)
            btnMultiplayer.clicked += AbrirMenuMultiplayer;

        if (btnVoltarInfinito != null)
            btnVoltarInfinito.clicked += VoltarParaBotoesPrincipais;
    }

    private void AtualizarPontuacoesInfinito()
    {
        int recordeSingle = PlayerPrefs.GetInt("RecordeSingle", 0);
        int recordeMulti = PlayerPrefs.GetInt("RecordeMulti", 0);

        if (labelPontuacaoSingle != null)
            labelPontuacaoSingle.text = $"Recorde: {recordeSingle}m";

        if (labelPontuacaoMulti != null)
            labelPontuacaoMulti.text = $"Recorde: {recordeMulti}m";
    }

    private void IniciarSinglePlayerInfinito()
    {
        PlayerPrefs.SetString("ModoJogoAtual", "SinglePlayer");
        SceneManager.LoadScene(nomeCenaModoInfinito);
    }

    private void AbrirMenuMultiplayer()
    {
        PlayerPrefs.SetString("ModoJogoAtual", "Multiplayer");
    }

    private void AbrirSubPainel(VisualElement subPainelAlvo)
    {
        if (containerBotoesPrincipais != null)
            containerBotoesPrincipais.style.display = DisplayStyle.None;

        OcultarElemento(panelModoInfinito);
        OcultarElemento(panelOpcoes);
        OcultarElemento(panelSavesHistoria);
        OcultarElemento(panelCreditos);

        if (subPainelAlvo != null)
        {
            subPainelAlvo.style.display = DisplayStyle.Flex;
        }
    }

    private void VoltarParaBotoesPrincipais()
    {
        OcultarElemento(panelModoInfinito);
        OcultarElemento(panelOpcoes);
        OcultarElemento(panelSavesHistoria);
        OcultarElemento(panelCreditos);

        if (containerBotoesPrincipais != null)
        {
            containerBotoesPrincipais.style.display = DisplayStyle.Flex;
        }
    }

    private void OcultarElemento(VisualElement elemento)
    {
        if (elemento != null)
        {
            elemento.style.display = DisplayStyle.None;
        }
    }

    private void ConfigurarOpcoes()
    {
        float volMusica = PlayerPrefs.GetFloat("VolMusica", 1.0f);
        float volSFX = PlayerPrefs.GetFloat("VolSFX", 1.0f);

        if (sliderMusica != null)
        {
            sliderMusica.lowValue = 0f;
            sliderMusica.highValue = 1f;
            sliderMusica.value = volMusica;
            AplicarVolumeFMOD(vcaMusicaPath, volMusica);

            sliderMusica.RegisterValueChangedCallback(evt => {
                PlayerPrefs.SetFloat("VolMusica", evt.newValue);
                AplicarVolumeFMOD(vcaMusicaPath, evt.newValue);
            });
        }

        if (sliderSFX != null)
        {
            sliderSFX.lowValue = 0f;
            sliderSFX.highValue = 1f;
            sliderSFX.value = volSFX;
            AplicarVolumeFMOD(vcaSFXPath, volSFX);

            sliderSFX.RegisterValueChangedCallback(evt => {
                PlayerPrefs.SetFloat("VolSFX", evt.newValue);
                AplicarVolumeFMOD(vcaSFXPath, evt.newValue);
            });
        }

        if (toggleFPS != null)
        {
            toggleFPS.value = PlayerPrefs.GetInt("MostrarFPS", 0) == 1;
            toggleFPS.RegisterValueChangedCallback(evt => {
                PlayerPrefs.SetInt("MostrarFPS", evt.newValue ? 1 : 0);
            });
        }

        if (dropdownResolucao != null)
        {
#if UNITY_ANDROID || UNITY_IOS
            dropdownResolucao.style.display = DisplayStyle.None;
#else
            dropdownResolucao.style.display = DisplayStyle.Flex;
            resolucoesDisponiveis = Screen.resolutions;
            List<string> opcoes = new List<string>();
            int indiceAtual = 0;

            for (int i = 0; i < resolucoesDisponiveis.Length; i++)
            {
                string opcao = resolucoesDisponiveis[i].width + " x " + resolucoesDisponiveis[i].height;
                opcoes.Add(opcao);

                if (resolucoesDisponiveis[i].width == Screen.currentResolution.width &&
                    resolucoesDisponiveis[i].height == Screen.currentResolution.height)
                {
                    indiceAtual = i;
                }
            }

            dropdownResolucao.choices = opcoes;
            dropdownResolucao.index = PlayerPrefs.GetInt("IndiceResolucao", indiceAtual);

            dropdownResolucao.RegisterValueChangedCallback(evt => {
                int index = dropdownResolucao.index;
                if (index >= 0 && index < resolucoesDisponiveis.Length)
                {
                    Resolution res = resolucoesDisponiveis[index];
                    Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
                    PlayerPrefs.SetInt("IndiceResolucao", index);
                }
            });
#endif
        }
    }

    private void AplicarVolumeFMOD(string vcaPath, float volume)
    {
        Type runtimeManagerType = Type.GetType("FMODUnity.RuntimeManager, FMODUnity");
        if (runtimeManagerType != null)
        {
            try
            {
                var getVCAMethod = runtimeManagerType.GetMethod("GetVCA", new Type[] { typeof(string) });
                if (getVCAMethod != null)
                {
                    var vcaInstance = getVCAMethod.Invoke(null, new object[] { vcaPath });
                    var setVolumeMethod = vcaInstance.GetType().GetMethod("setVolume", new Type[] { typeof(float) });
                    if (setVolumeMethod != null)
                    {
                        setVolumeMethod.Invoke(vcaInstance, new object[] { volume });
                    }
                }
            }
            catch { }
        }
    }

    private void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}