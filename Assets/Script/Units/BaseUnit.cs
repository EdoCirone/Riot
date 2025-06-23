using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseUnit : MonoBehaviour
{

    protected SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public virtual void Select()
    {
        spriteRenderer.color = Color.yellow;
    }

    public virtual void Deselect()
    {
        spriteRenderer.color = Color.white;
    }
}
