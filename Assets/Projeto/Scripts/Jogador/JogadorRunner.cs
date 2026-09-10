using UnityEngine;
using UnityEngine.SceneManagement;

public class JogadorRunner : MonoBehaviour
{
    [Header("Configuracoes de Movimento")]
    [SerializeField] private float forcaPulo = 10f;
    [SerializeField] private float velocidadeFrente = 8f;

    [Header("Configuracao de Morte")]
    [SerializeField] private float limiteAlturaQueda = -10f;

    [Header("Configuracoes de Cores")]
    [SerializeField] private Color corA = Color.cyan;
    [SerializeField] private Color corB = Color.red;

    private Rigidbody rb;
    private Renderer renderizadorMesh;
    private bool estaNaCorA = true;
    private bool estaNoChao;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderizadorMesh = GetComponent<Renderer>();
        DefinirCorJogador(true);
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
        rb.linearVelocity = new Vector3(velocidadeFrente, rb.linearVelocity.y, 0f);
    }

    private void LateUpdate()
    {
        if (transform.position.y < limiteAlturaQueda)
        {
            Morrer();
        }
    }

    private void Pular()
    {
        if (estaNoChao)
        {
            rb.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);
            estaNoChao = false;
        }
    }

    private void AlternarCor()
    {
        DefinirCorJogador(!estaNaCorA);
    }

    private void DefinirCorJogador(bool ativarCorA)
    {
        estaNaCorA = ativarCorA;

        if (estaNaCorA)
        {
            renderizadorMesh.material.color = corA;
            gameObject.layer = LayerMask.NameToLayer("CorA");
        }
        else
        {
            renderizadorMesh.material.color = corB;
            gameObject.layer = LayerMask.NameToLayer("CorB");
        }
    }

    private void OnCollisionEnter(Collision colisao)
    {
        estaNoChao = true;

        if (colisao.gameObject.CompareTag("Obstaculo"))
        {
            Morrer();
        }
    }
    private void Morrer()
    {
        Debug.Log("O jogador morreu!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}