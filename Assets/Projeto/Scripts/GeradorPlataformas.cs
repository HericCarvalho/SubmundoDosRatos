using System.Collections.Generic;
using UnityEngine;

public class GeradorPlataformas : MonoBehaviour
{
    public enum TipoCorObstaculo { CorA, CorB, BrancoIntrasponivel }

    [Header("Prefab do Chao")]
    [Tooltip("Prefab do chao")]
    [SerializeField] private GameObject prefabChaoNeutro;

    [Header("Cores dos Obstaculos")]
    [SerializeField] private Color corA = Color.cyan;
    [SerializeField] private Color corB = Color.red;
    [SerializeField] private Color corBranca = Color.white;

    [Header("Modelos de Obstaculos")]
    [Tooltip("Adicione aqui seus formatos")]
    [SerializeField] private List<GameObject> listaModelosObstaculos;

    [Header("Configuracoes de Visao")]
    [SerializeField] private Transform alvoJogador;
    [SerializeField] private float distanciaVisaoFrente = 50f;
    [SerializeField] private float distanciaSegurancaAtras = 20f;

    [Header("Configuracoes da Pista")]
    [SerializeField] private float tamanhoPadraoX = 10f;
    [SerializeField] private float chanceGerarBuraco = 0.2f;
    [SerializeField] private float tamanhoBuraco = 4f;
    [Range(0f, 1f)]
    [SerializeField] private float chanceObstaculoSerBranco = 0.25f;

    [Header("Variacao de Altura dos Obstaculos")]
    [SerializeField] private bool usarAlturasAleatorias = false;
    [SerializeField] private float alturaMinimaOffset = 0f;
    [SerializeField] private float alturaMaximaOffset = 2.5f;

    private float proximaPosicaoX = 0f;
    private List<GameObject> plataformasAtivas = new List<GameObject>();
    private int plataformasIniciaisSeguras = 5;
    private bool ultimoObstaculoExigiuPulo = false;

    private void Start()
    {
        if (prefabChaoNeutro == null)
        {
            Debug.LogError("[GeradorPlataformas] ERRO: Voce esqueceu de arrastar o prefab no campo 'Prefab Chao Neutro'!");
            return;
        }

        BuscarJogadorLider();

        if (alvoJogador != null)
        {
            proximaPosicaoX = alvoJogador.position.x - 10f;
        }
        else
        {
            proximaPosicaoX = -10f;
        }

        for (int i = 0; i < plataformasIniciaisSeguras; i++)
        {
            GerarChaoSemObstaculo();
        }
    }

    private void Update()
    {
        BuscarJogadorLider();

        if (alvoJogador == null) return;

        while (proximaPosicaoX < alvoJogador.position.x + distanciaVisaoFrente)
        {
            ProcessarProximoElemento();
        }

        RemoverPlataformasAtras();
    }

    private void BuscarJogadorLider()
    {
        float maiorX = -99999f;
        Transform jogadorLider = null;

        JogadorNetwork[] jogadoresNetwork = Object.FindObjectsByType<JogadorNetwork>(FindObjectsSortMode.None);
        if (jogadoresNetwork.Length > 0)
        {
            foreach (var j in jogadoresNetwork)
            {
                if (j.transform.position.x > maiorX)
                {
                    maiorX = j.transform.position.x;
                    jogadorLider = j.transform;
                }
            }
        }
        else
        {
            JogadorRunner jogadorSingle = Object.FindAnyObjectByType<JogadorRunner>();
            if (jogadorSingle != null)
            {
                jogadorLider = jogadorSingle.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    jogadorLider = playerObj.transform;
                }
            }
        }

        if (jogadorLider != null)
        {
            alvoJogador = jogadorLider;
        }
    }

    private void ProcessarProximoElemento()
    {
        if (!ultimoObstaculoExigiuPulo && plataformasAtivas.Count > plataformasIniciaisSeguras && Random.value < chanceGerarBuraco)
        {
            proximaPosicaoX += tamanhoBuraco;
            ultimoObstaculoExigiuPulo = true;
        }
        else
        {
            ultimoObstaculoExigiuPulo = false;
        }

        GerarChaoComObstaculo();
    }

    private void GerarChaoSemObstaculo()
    {
        Vector3 posicao = new Vector3(proximaPosicaoX, 0f, 0f);
        GameObject novoChao = Instantiate(prefabChaoNeutro, posicao, Quaternion.identity);

        plataformasAtivas.Add(novoChao);
        proximaPosicaoX += tamanhoPadraoX;
    }

    private void GerarChaoComObstaculo()
    {
        Vector3 posicao = new Vector3(proximaPosicaoX, 0f, 0f);
        GameObject novoChao = Instantiate(prefabChaoNeutro, posicao, Quaternion.identity);

        GerarObstaculoGarantido(posicao, novoChao.transform);

        plataformasAtivas.Add(novoChao);
        proximaPosicaoX += tamanhoPadraoX;
    }

    private void GerarObstaculoGarantido(Vector3 posicaoPlataforma, Transform paiPlataforma)
    {
        if (listaModelosObstaculos == null || listaModelosObstaculos.Count == 0) return;
        if (ultimoObstaculoExigiuPulo) return;

        int indiceModelo = Random.Range(0, listaModelosObstaculos.Count);
        GameObject modeloSorteado = listaModelosObstaculos[indiceModelo];

        if (modeloSorteado == null) return;

        TipoCorObstaculo corEscolhida;

        if (Random.value < chanceObstaculoSerBranco)
        {
            corEscolhida = TipoCorObstaculo.BrancoIntrasponivel;
            ultimoObstaculoExigiuPulo = true;
        }
        else
        {
            corEscolhida = (Random.value > 0.5f) ? TipoCorObstaculo.CorA : TipoCorObstaculo.CorB;
        }

        float offsetAltura = usarAlturasAleatorias ? Random.Range(alturaMinimaOffset, alturaMaximaOffset) : 0f;

        Vector3 posicaoObstaculo = new Vector3(posicaoPlataforma.x, posicaoPlataforma.y + 1f + offsetAltura, posicaoPlataforma.z);
        GameObject novoObstaculo = Instantiate(modeloSorteado, posicaoObstaculo, Quaternion.identity);
        novoObstaculo.transform.SetParent(paiPlataforma);

        AplicarPropriedadesAoObstaculo(novoObstaculo, corEscolhida);
    }

    private void AplicarPropriedadesAoObstaculo(GameObject obstaculo, TipoCorObstaculo tipoCor)
    {
        Color corFinal = corBranca;
        string nomeLayer = "Default";

        switch (tipoCor)
        {
            case TipoCorObstaculo.CorA:
                corFinal = corA;
                nomeLayer = "CorA";
                break;
            case TipoCorObstaculo.CorB:
                corFinal = corB;
                nomeLayer = "CorB";
                break;
            case TipoCorObstaculo.BrancoIntrasponivel:
                corFinal = corBranca;
                nomeLayer = "Default";
                break;
        }

        int layerID = LayerMask.NameToLayer(nomeLayer);

        Renderer[] renderizadores = obstaculo.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderizadores)
        {
            rend.material.color = corFinal;
        }

        Transform[] todosFilhos = obstaculo.GetComponentsInChildren<Transform>();
        foreach (Transform t in todosFilhos)
        {
            t.gameObject.tag = "Obstaculo";
            if (layerID != -1)
            {
                t.gameObject.layer = layerID;
            }
        }
    }

    private void RemoverPlataformasAtras()
    {
        if (plataformasAtivas.Count > 0)
        {
            GameObject plataformaMaisAntiga = plataformasAtivas[0];
            float fimDaPlataformaX = plataformaMaisAntiga.transform.position.x + tamanhoPadraoX;

            if (fimDaPlataformaX < alvoJogador.position.x - distanciaSegurancaAtras)
            {
                plataformasAtivas.RemoveAt(0);
                Destroy(plataformaMaisAntiga);
            }
        }
    }
}