using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.AI;
using Panda;

public class AI : MonoBehaviour
{
 
    [SerializeField] public float health = 100f;
    [SerializeField] private float damage = 10;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject Enemy;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawn;
    [SerializeField] private float shootColDown = 0.5f;

    private float _shootColDown;
    private float waitTimer = 0f;
    private float lastCheckedHealth;
    private float healthCheckInterval = 3f;
    private float healthCheckTimer = 0f;

    private bool needNewDestination;
    private bool needNewDestinationWonder;
    private bool isWaiting = false;
    private bool isHavePotion = true;


    NavMeshAgent agent;
    private Vector3 aimDestination;
    
    void Start()
    {
        needNewDestination = true;
        needNewDestinationWonder = true;
        agent = this.GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        _shootColDown = shootColDown;
        healthBar.maxValue = health;
    }


    void Update()
    {
        healthBar.value = (int)health;
    }
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "Bullet")
        {
            Bullet bulletScript = col.gameObject.GetComponent<Bullet>();
            if (bulletScript.creator != gameObject)
            {
                Destroy(col.gameObject);
                health -= damage;
            }

        }
    }
    Vector3 RandomNavMeshPoint(float radius)
    {
        Vector3 randomPoint = Random.insideUnitSphere * radius;
        randomPoint += transform.position; 
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }

    // Tasks

    #region Behavior Tasks
    [Task]
    public void AimTarget()
    {
        _shootColDown -= Time.deltaTime;
        aimDestination = Enemy.transform.position - this.transform.position;
        aimDestination.z = 0f;
        float errorDegree = 10f;
        Vector2 errorVector = Quaternion.Euler(0, 0, Random.Range(-errorDegree, errorDegree)) * aimDestination;
        aimDestination = errorVector;

        if (_shootColDown <= 0)
        {
            _shootColDown = shootColDown;
            Task.current.Succeed();
        }
    }
    [Task]
    public void ShootTarget()
    {
        GameObject bullet = GameObject.Instantiate(bulletPrefab, bulletSpawn.transform.position,
                                                           bulletSpawn.transform.rotation);
        bullet.GetComponent<Bullet>().creator = gameObject;
        bullet.GetComponent<Rigidbody2D>().AddForce(aimDestination.normalized * 0.05f);
        Task.current.Succeed();
    }
    [Task]
    bool SeePlayer()
    {
        Vector2 direction = Enemy.transform.position - transform.position;
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y);
        float distance = direction.magnitude;
        direction.Normalize();

        Debug.DrawRay(rayOrigin, direction * distance, Color.red, 0.1f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, distance);

        var filteredHits = hits.Where(hit =>
            hit.collider != null &&
            hit.collider.gameObject != gameObject
        ).ToArray();


        if (filteredHits.Any(hit => hit.collider.CompareTag("wall")))
        {
            return false;
        }

        bool playerSeen = filteredHits.Any(hit => hit.collider.CompareTag("AI"));

        return playerSeen;
    }
    [Task]
    public void Wonder()
    {
        agent.isStopped = false;
        if (needNewDestinationWonder)
        {
            Vector3 dest = RandomNavMeshPoint(8);
            agent.SetDestination(dest);
            needNewDestinationWonder = false;
        }
        if (agent.remainingDistance < 1f)
        {
            needNewDestinationWonder = true;
        }

        Task.current.Succeed();
    }
    [Task]
    public void IsStop(bool value)
    {
        agent.isStopped = value;
        Task.current.Succeed();
    }
    [Task]
    public void IsRapeatDeclineHealth()
    {

        healthCheckTimer += Time.deltaTime;
        Debug.Log(healthCheckTimer);
        if (healthCheckTimer >= healthCheckInterval)
        {

            float healthDifference = lastCheckedHealth - health;
            healthCheckTimer = 0f;
            lastCheckedHealth = health;

            if (healthDifference > 10)
            {
                Task.current.Succeed();
            }
            else
            {
                Task.current.Fail();

            }

        }
    }
    [Task]
    public void ChangePosition()
    {
        agent.isStopped = false;
        if (needNewDestination)
        {
            Vector3 dest = RandomNavMeshPoint(5);
            agent.SetDestination(dest);
            needNewDestination = false;
        }
        if (agent.remainingDistance < 1f)
        {
            needNewDestination = true;
            Task.current.Succeed();
            agent.isStopped = true;
        }
    }
    [Task]
    public void WaitForSeconds(float seconds)
    {
        if (!isWaiting)
        {
            waitTimer = seconds;
            isWaiting = true;
        }

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
        }
        else
        {
            isWaiting = false;
            Task.current.Succeed();
        }
    }
    #endregion

    #region Survival Tasks
    [Task]
    public void UsePotion()
    {
        health += 50f;
        isHavePotion = false;
        Task.current.Succeed();
    }

    [Task]
    bool IsHealthLessThan(float value)
    {
        return health < value;
    }
    [Task]
    bool HavePotion()
    {
        return isHavePotion;
    }

    [Task]
    public void Explode()
    {
        Destroy(gameObject);
    }
    #endregion


}
