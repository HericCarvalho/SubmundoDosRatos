using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JogadorNetwork : NetworkBehaviour
{
    [Header("Configuracoes de Movimento")]
    [SerializeField] private float forcaPulo = 10f;
    [SerializeField] private float velocidadeFrente = 8f;

    [Header("Configuracao de Morte")]
    [SerializeField] private float limiteAlturaQueda = -10f;

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
        DefinirCorJogador(true);

        if (IsOwner)
        {
            CinemachineCamera vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = transform;
                vcam.LookAt = transform;
            }
        }
        else
        {
            AplicarEfeitoFantasma();
            IgnorarColisaoAdversario();
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

    private void Update()
    {
        if (GameMultiplayer.Instance != null && !GameMultiplayer.Instance.JogoIniciado.Value)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        rb.linearVelocity = new Vector3(velocidadeFrente, rb.linearVelocity.y, 0f);
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
            rb.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);
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

        gameObject.layer = LayerMask.NameToLayer(estaNaCorA ? "CorA" : "CorB");
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
    private void IgnorarColisaoAdversario()
    {
        Collider meuCollider = GetComponent<Collider>();
        if (meuCollider == null) return;

        JogadorNetwork[] outrosJogadores = Object.FindObjectsByType<JogadorNetwork>(FindObjectsSortMode.None);
        foreach (var jogador in outrosJogadores)
        {
            if (jogador != this)
            {
                Collider colliderAdversario = jogador.GetComponent<Collider>();
                if (colliderAdversario != null)
                {
                    Physics.IgnoreCollision(meuCollider, colliderAdversario, true);
                }
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

    private void Morrer()
    {
        Debug.Log("O jogador morreu no Multiplayer!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}