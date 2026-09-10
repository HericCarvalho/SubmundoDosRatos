using UnityEngine;

public class ObstaculoDinamico : MonoBehaviour
{
    public enum TipoCor { CorA, CorB, BrancoIntrasponivel }

    [SerializeField] private Renderer renderizador;

    public void AplicarCorETag(TipoCor tipo, Color corA, Color corB, Color corBranca)
    {
        if (renderizador == null)
        {
            renderizador = GetComponentInChildren<Renderer>();
        }
        switch (tipo)
        {
            case TipoCor.CorA:
                if (renderizador != null) renderizador.material.color = corA;
                gameObject.layer = LayerMask.NameToLayer("CorA");
                break;

            case TipoCor.CorB:
                if (renderizador != null) renderizador.material.color = corB;
                gameObject.layer = LayerMask.NameToLayer("CorB");
                break;

            case TipoCor.BrancoIntrasponivel:
                if (renderizador != null) renderizador.material.color = corBranca;
                gameObject.layer = LayerMask.NameToLayer("Default"); 
                break;
        }
        gameObject.tag = "Obstaculo";
    }
}