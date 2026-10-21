using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAISimple : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, FleeFire }

    [Header("Patrulha Sem NavMesh")]
    public float patrolRadius = 10f;
    public float moveSpeed = 3.5f;
    public float turnSpeed = 12f;
    public float waitTimeAtPoint = 1.5f;
    public float stoppingDistance = 3f;

    [Header("Vis�o e Persegui��o")]
    public float detectionRadius = 8f;
    public LayerMask obstacleMask;

    [Header("Prote��o da Fogueira")]
    public Light fireLight;

    [Header("�rea de Patrulha")]
    public BoxCollider patrolArea;

    [Header("Sanidade")]
    public PlayerSanity playerSanity;
    public float maxSanitySpeedMultiplier = 1.2f;

    private Rigidbody rb;
    private Transform playerTransform;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float waitTimer;
    private bool isWaiting = false;
    private float patrolMoveTimer = 0f;
    public float maxPatrolMoveTime = 8f;
    private EnemyState currentState = EnemyState.Patrol;
    private Vector3 patrolDirection = Vector3.zero;
    public float patrolTurnSmoothness = 2.5f;
    private bool playerKilled = false;
    private bool insideFireZone = false;
    private FireDangerZone currentFireZone;
    public float fireDangerMargin = 1.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }

        SetNewTargetPosition();
    }

    void Update()
    {
        if (playerTransform == null) return;

        bool canSeePlayer = CheckSightToPlayer();

        if (canSeePlayer)
        {
            currentState = EnemyState.Chase;
            isWaiting = false;
        }
        else if (currentState == EnemyState.Chase && !canSeePlayer)
        {
            currentState = EnemyState.Patrol;
            SetNewTargetPosition();
        }

        if (currentState == EnemyState.Patrol && insideFireZone)
        {
            SetNewTargetPosition();
            isWaiting = false;
        }
    }

    float GetCurrentMoveSpeed()
    {
        if (playerSanity == null)
            return moveSpeed;

        float sanityPercentage =
            playerSanity.currentSanity /
            playerSanity.maxSanity;

        float multiplier =
            Mathf.Lerp(
                maxSanitySpeedMultiplier,
                1f,
                sanityPercentage
            );

        return moveSpeed * multiplier;
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolMove();
                break;

            case EnemyState.Chase:
                ChaseMove();
                break;

            case EnemyState.FleeFire:
                FleeFireMove();
                break;
        }
    }

    bool CheckSightToPlayer()
    {
        if (HidingSpot.PlayerIsHiding)
            return false;

        if (fireLight != null)
        {
            float distanceToFire =
                Vector3.Distance(playerTransform.position, fireLight.transform.position);

            if (distanceToFire <= fireLight.range)
                return false;
        }

        float distanceToPlayer =
            Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            Vector3 directionToPlayer =
                (playerTransform.position - transform.position).normalized;

            if (!Physics.Raycast(
                transform.position + Vector3.up,
                directionToPlayer,
                distanceToPlayer,
                obstacleMask))
            {
                return true;
            }
        }

        return false;
    }

    void PatrolMove()
    {
        if (isWaiting)
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                new Vector3(0f, rb.linearVelocity.y, 0f),
                Time.fixedDeltaTime * 10f
            );

            waitTimer += Time.fixedDeltaTime;

            if (waitTimer >= waitTimeAtPoint)
            {
                SetNewTargetPosition();
                isWaiting = false;
                waitTimer = 0f;
            }

            return;
        }

        patrolMoveTimer += Time.fixedDeltaTime;

        if (patrolMoveTimer >= maxPatrolMoveTime)
        {
            SetNewTargetPosition();
            patrolMoveTimer = 0f;
            isWaiting = false;
            return;
        }

        MoveTowards(targetPosition);

        Vector3 flatCurrent = new Vector3(
            transform.position.x,
            0f,
            transform.position.z
        );

        Vector3 flatTarget = new Vector3(
            targetPosition.x,
            0f,
            targetPosition.z
        );

        if (Vector3.Distance(flatCurrent, flatTarget) <= stoppingDistance)
        {
            isWaiting = true;
        }
    }

    void ChaseMove()
    {
        MoveTowards(playerTransform.position);
    }

    void FleeFireMove()
    {
        if (currentFireZone == null)
            return;

        Vector3 directionAway =
            transform.position - currentFireZone.transform.position;

        directionAway.y = 0f;

        float distanceToFire = directionAway.magnitude;

        // Quando sair da �rea da fogueira, volta a patrulhar
        if (distanceToFire > currentFireZone.GetComponent<Collider>().bounds.extents.magnitude + 2f)
        {
            currentState = EnemyState.Patrol;
            SetNewTargetPosition();
            return;
        }

        if (directionAway.sqrMagnitude > 0.1f)
        {
            directionAway.Normalize();

            // Nome diferente do campo "targetPosition" para n�o escond�-lo.
            Vector3 fleeTargetPosition =
                currentFireZone.transform.position +
                directionAway * (currentFireZone.GetComponent<Collider>().bounds.extents.magnitude + 2f);

            MoveTowards(fleeTargetPosition);
        }
    }

    void MoveTowards(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.1f)
        {
            direction.Normalize();

            if (currentState == EnemyState.Patrol)
            {
                if (patrolDirection.sqrMagnitude < 0.01f)
                {
                    patrolDirection = direction;
                }

                patrolDirection = Vector3.Slerp(
                    patrolDirection,
                    direction,
                    patrolTurnSmoothness * Time.fixedDeltaTime
                );

                direction = patrolDirection.normalized;
            }

            Vector3 targetVelocity =
                direction * GetCurrentMoveSpeed();

            rb.linearVelocity = new Vector3(
                targetVelocity.x,
                rb.linearVelocity.y,
                targetVelocity.z
            );

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            Quaternion nextRotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );

            rb.MoveRotation(nextRotation);
        }
    }

    bool IsInsideFireDangerZone(Vector3 position)
    {
        FireDangerZone[] fireZones =
            FindObjectsByType<FireDangerZone>();

        foreach (FireDangerZone zone in fireZones)
        {
            Collider zoneCollider = zone.GetComponent<Collider>();

            if (zoneCollider == null)
                continue;

            Vector3 closestPoint = zoneCollider.ClosestPoint(position);

            if (Vector3.Distance(position, closestPoint) <= fireDangerMargin)
            {
                return true;
            }
        }

        return false;
    }

    bool IsPathInsideFireDangerZone(Vector3 destination)
    {
        Vector3 start = transform.position;

        float distance = Vector3.Distance(start, destination);

        int checks = Mathf.CeilToInt(distance / 1f);

        for (int i = 1; i <= checks; i++)
        {
            float t = (float)i / checks;

            Vector3 point = Vector3.Lerp(start, destination, t);

            if (IsInsideFireDangerZone(point))
            {
                return true;
            }
        }

        return false;
    }

    void SetNewTargetPosition()
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPosition;

            if (patrolArea != null)
            {
                Bounds bounds = patrolArea.bounds;

                float randomX =
                    Random.Range(bounds.min.x, bounds.max.x);

                float randomZ =
                    Random.Range(bounds.min.z, bounds.max.z);

                randomPosition = new Vector3(
                    randomX,
                    transform.position.y,
                    randomZ
                );
            }
            else
            {
                Vector2 randomCircle =
                    Random.insideUnitCircle * patrolRadius;

                randomPosition =
                    startPosition +
                    new Vector3(
                        randomCircle.x,
                        0f,
                        randomCircle.y
                    );
            }

            if (!IsInsideFireDangerZone(randomPosition) &&
    !IsPathInsideFireDangerZone(randomPosition))
            {
                targetPosition = randomPosition;
                patrolMoveTimer = 0f;
                return;
            }
        }

        // Se n�o encontrou um ponto v�lido,
        // mant�m a posi��o atual como destino.
        targetPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            KillPlayer();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            KillPlayer();
    }

    void KillPlayer()
    {
        if (playerKilled)
            return;

        if (HidingSpot.PlayerIsHiding)
            return;

        playerKilled = true;

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );

        Gizmos.color = Color.cyan;

        Vector3 center =
            Application.isPlaying
                ? startPosition
                : transform.position;

        Gizmos.DrawWireSphere(
            center,
            patrolRadius
        );
    }

    public void EnterFireZone(FireDangerZone fireZone)
    {
        insideFireZone = true;
        currentFireZone = fireZone;
        currentState = EnemyState.FleeFire;
        isWaiting = false;
    }

    public void ExitFireZone(FireDangerZone fireZone)
    {
        if (currentFireZone == fireZone)
        {
            insideFireZone = false;
            currentFireZone = null;
        }
    }
}