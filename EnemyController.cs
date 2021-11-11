using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBehaviour
{
    protected GameObject m_object;
    protected GameObject m_target;
    protected bool awake = false;
    public float awarenessDistance { get; set; } = 10;
    public Vector2 direction { get; set; }
    public float speed { get; set; } = 2.5f;

    public EnemyBehaviour(GameObject owner)
    {
        m_object = owner;
        m_target = GameObject.Find("Player");
    }

    public void WakeOnProximity()
    {
        Vector2 distance = m_object.transform.position - m_target.transform.position;
        if (distance.magnitude < awarenessDistance)
        {
            awake = true;
        }
    }
    public abstract void Update();
}

public class WalkerBehaviour : EnemyBehaviour
{
    public WalkerBehaviour(GameObject owner) : base(owner) { }

    public override void Update()
    {
        if (!awake)
            WakeOnProximity();
        if (awake)
        {
            direction = m_target.transform.position - m_object.transform.position;
            direction = direction.normalized;
            m_object.GetComponent<Rigidbody2D>().velocity = direction * speed;
            float angle = Mathf.Atan2(direction.y, direction.x);
            angle *= Mathf.Rad2Deg;
            m_object.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}

public class SpitterBehaviour: EnemyBehaviour
{
    public int range { get; set; } = 8;
    private bool inRange = false;
    private float fireCooldown = 2;
    private float fireTime;
    public GameObject projectile { get; set; }
    public float projectileSpeed { get; set; } = 3;

    public SpitterBehaviour (GameObject owner, GameObject projectile) : base(owner)
    {
        this.projectile = projectile;
    }
    public override void Update()
    {
        if (!awake)
            WakeOnProximity();
        if (awake)
        {
            direction = m_target.transform.position - m_object.transform.position;
            inRange = (direction.magnitude < range);
            direction = direction.normalized;
            if (!inRange)
                m_object.GetComponent<Rigidbody2D>().velocity = direction * speed;
            float angle = Mathf.Atan2(direction.y, direction.x);
            angle *= Mathf.Rad2Deg;
            m_object.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        if (inRange)
        {
            fireTime -= Time.deltaTime;
            if(fireTime < 0)
            {
                GameObject slime = GameObject.Instantiate(projectile);
                slime.transform.position = m_object.transform.position + m_object.transform.right * 1.5f;
                slime.transform.rotation = m_object.transform.rotation;
                slime.GetComponent<Rigidbody2D>().velocity = slime.transform.right * projectileSpeed;
                GameObject.Destroy(slime, 2.5f);
                fireTime = fireCooldown;
            }
        }
    }
}
public class EnemyController : MonoBehaviour
{
    [SerializeField] int health = 3;
    [SerializeField] AudioSource audio;
    [SerializeField] float animSpeed = 0.5f;
    [SerializeField] GameObject projectile;

    enum BehaviourType { walker, spitter };
    [SerializeField] BehaviourType behaviour;
    EnemyBehaviour m_enemyBehaviour;
    //WalkerBehaviour m_enemyBehaviour;
    //SpitterBehaviour m_spitterBehaviour;
    Animator m_animator;
    Rigidbody2D m_body;

    [SerializeField] float meleeCooldown = 1.5f;
    float currentmeleeCooldown;
    bool attacking = false;
    // Start is called before the first frame update
    void Start()
    {
        m_body = GetComponent<Rigidbody2D>();
        m_animator = GetComponent<Animator>();

        switch (behaviour)
        {
            case BehaviourType.walker:
                m_enemyBehaviour = new WalkerBehaviour(gameObject);
                break;
            case BehaviourType.spitter:
                m_enemyBehaviour = new SpitterBehaviour(gameObject, projectile);
                break;
        }

    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name.Contains("bulletFire"))
        {
            CollisionResponse(collision);
        }
        else if (collision.gameObject.name == "Player")
        {
            CollisionResponse(collision.gameObject.GetComponent<PlayerController>());
        }
    }
    private void CollisionResponse(PlayerController player)
    {
        attacking = true;
    }
    private void CollisionResponse(Collision2D collision)
    {
        health--;
        Destroy(collision.gameObject);
        if (health == 0)
        {
            GetComponent<SpriteRenderer>().color = new Color(0.5f, 0, 0, 0.5f);
            audio.Play();
            Destroy(gameObject, 2f);
            foreach (var collider in GetComponents<Collider2D>())
                Destroy(collider);
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            attacking = false;
            currentmeleeCooldown = meleeCooldown;
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_animator.speed = m_body.velocity.magnitude * animSpeed;

        if (health == 0) return;
        m_enemyBehaviour.Update();
        //switch (behaviour)
        //{
        //    case BehaviourType.walker:
        //        m_enemyBehaviour.Update();
        //        break;
        //    case BehaviourType.spitter:
        //        m_spitterBehaviour.Update();
        //        break;
        //}
        if (attacking)
        {
            currentmeleeCooldown -= Time.deltaTime;
            if (currentmeleeCooldown < 0)
                currentmeleeCooldown = meleeCooldown;
            GameObject.Find("Player").GetComponent<PlayerController>().TakeDamage(5);
        }
    }
}
