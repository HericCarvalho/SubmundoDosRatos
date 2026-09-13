using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class JogadorNetwork : NetworkBehaviour
{
    [Header("Configuracoes de Movimento")]
    [SerializeField] private float forcaPulo = 10f;
    [SerializeField] private float velocidadeFrente = 8f;

    [Header("Configuracao de Morte e Respawn")]
    [SerializeField] private float limiteAlturaQueda = -10f;

    private Vector3 pontoCheckpointAtual;

    [Header("Configuracoes de Cores")]
    [SerializeField] private Color corA = Color.cyan;
    [SerializeField] private Color corB = Color.red;

    [Header("Aparência do Adversario (Fantasma)")]
    [Range(0f, 1f)]
    [SerializeField] private float opacidadeFantasma = 0.35f;

    private Rigidbody rb;
    private Renderer renderizadorMesh;
    private bool estaNaCorA = true;
    private bool estaNoChao;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderizadorMesh = GetComponent<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        float deslocamentoZ = (OwnerClientId % 2 == 0) ? -0.5f : 0.5f;
        transform.position += new Vector3(0f, 0f, deslocamentoZ);
        transform.rotation = Quaternion.identity;

        pontoCheckpointAtual = transform.position;

        DefinirCorJogador(true);

        if (IsOwner)
        {
            CinemachineCamera vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = transform;
                vcam.LookAt = transform;
            }

            AtualizarTodosOsFantasmas();
        }
        else
        {
            AplicarEfeitoFantasma();
        }
    }

    private void OnEnable()
    {
        GerenciadorInputs.AoApertarPulo += Pular;
        GerenciadorInputs.AoApertarTrocarCor += AlternarCor;
    }

    private void OnDisable()
    {
        GerenciadorInputs.AoApertarPulo -= Pular;
        GerenciadorInputs.AoApertarTrocarCor -= AlternarCor;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (GameMultiplayer.Instance != null && !GameMultiplayer.Instance.JogoIniciado.Value)
        {
            ZerarVelocidade();
            return;
        }

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(velocidadeFrente, rb.linearVelocity.y, 0f);
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        if (transform.position.y < limiteAlturaQueda)
        {
            Morrer();
        }
    }

    private void Pular()
    {
        if (!IsOwner) return;

        if (estaNoChao)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);
            }
            estaNoChao = false;
        }
    }

    private void AlternarCor()
    {
        if (!IsOwner) return;

        DefinirCorJogador(!estaNaCorA);
    }

    private void DefinirCorJogador(bool ativarCorA)
    {
        estaNaCorA = ativarCorA;

        if (renderizadorMesh != null)
        {
            Color corBase = estaNaCorA ? corA : corB;

            if (!IsOwner)
            {
                corBase.a = opacidadeFantasma;
            }

            renderizadorMesh.material.color = corBase;
        }

        string nomeLayer = estaNaCorA ? "CorA" : "CorB";
        int layerID = LayerMask.NameToLayer(nomeLayer);
        if (layerID != -1)
        {
            gameObject.layer = layerID;
        }
    }

    private void AplicarEfeitoFantasma()
    {
        if (renderizadorMesh != null)
        {
            Material mat = renderizadorMesh.material;

            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            Color corAtual = mat.color;
            corAtual.a = opacidadeFantasma;
            mat.color = corAtual;
        }
    }

    private void AtualizarTodosOsFantasmas()
    {
        JogadorNetwork[] todos = Object.FindObjectsByType<JogadorNetwork>(FindObjectsSortMode.None);
        foreach (var jogador in todos)
        {
            if (!jogador.IsOwner)
            {
                jogador.AplicarEfeitoFantasma();
            }
        }
    }

    private void OnCollisionEnter(Collision colisao)
    {
        if (!IsOwner) return;

        estaNoChao = true;

        if (colisao.gameObject.CompareTag("Obstaculo"))
        {
            Morrer();
        }
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (!IsOwner) return;

        if (outro.CompareTag("Checkpoint"))
        {
            pontoCheckpointAtual = outro.transform.position;
            Debug.Log("Novo Checkpoint Alcançado!");
        }
    }

    private void ZerarVelocidade()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Morrer()
    {
        ZerarVelocidade();

        if (TryGetComponent<NetworkTransform>(out var netTransform))
        {
            netTransform.Teleport(pontoCheckpointAtual, Quaternion.identity, transform.localScale);
        }
        else
        {
            transform.position = pontoCheckpointAtual;
        }

        DefinirCorJogador(true);
    }
}