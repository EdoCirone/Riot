// =====================
// UNITMOVER.CS
// =====================
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UnitMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private List<Node> path = new List<Node>();
    private int currentIndex = 0;
    private bool isMoving = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!isMoving || path == null || currentIndex >= path.Count) return;

        Vector2 currentPos = rb.position;
        Vector2 targetPos = path[currentIndex].worldPosition;

        Vector2 direction = (targetPos - currentPos).normalized;
        Vector2 newPosition = currentPos + direction * moveSpeed * Time.fixedDeltaTime;

        if (Vector2.Distance(currentPos, targetPos) < 0.1f)
        {
            currentIndex++;
            if (currentIndex >= path.Count)
            {
                isMoving = false;
                Debug.Log($"{name} ha raggiunto la destinazione.");
                return;
            }
        }
        else
        {
            rb.MovePosition(newPosition);
        }
    }

    public void FollowPath(List<Node> newPath)
    {
        if (newPath == null || newPath.Count == 0)
        {
            Debug.LogWarning("Percorso nullo o vuoto.");
            return;
        }

        path = newPath;
        currentIndex = 0;
        isMoving = true;
    }

    public void Stop()
    {
        isMoving = false;
        path.Clear();
    }

    public bool IsMoving => isMoving;
}
