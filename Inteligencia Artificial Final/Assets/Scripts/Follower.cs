using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    public FSM<FollowerStates> _FSM;

    Material _myMaterial;
    [SerializeField]Color _originalColor;

    [SerializeField] public float _maxSpeed, life, maxLife;

    private Collider minCollider;
    [SerializeField] public LayerMask nodes, obstacles, walls;

    [Header("Obstacle Avoidance")]
    public int numberOfRays;
    public float angle = 90;

    [SerializeField] float _maxForce;
    [SerializeField] public float _distanceToLeader;
    public Vector3 _velocity;

    [Header("SEEK")]
    SeekSteering _mySeekSteering;
    [Header("ARRIVE")]
    [SerializeField] private float _arriveRadius;
    [SerializeField] public Transform leader;
    public ArriveSteering _myArriveSteering;

    [Header("FLOCKING")]
    public Separation _mySeparationSteering;
    public Alignment _myAlignmentSteering;
    public Cohesion _myCohesionSteering;
    
    [Header("FOV")]
    [SerializeField] private float _viewRadius;
    [SerializeField] private float _viewAngle;

    [Header("Pathfinding")]
    public Node _startingNode, _goalNode, _baseNode;
    [HideInInspector] public bool isEvadingObstacles = false;
    [HideInInspector] public bool isEvadingWalls = false; 
    [HideInInspector] public bool isRetreating = false; 
    //public bool _startSearch = false;
    //public bool _hasReachNode = false;
    List<Node> _pathToFollow;
    Pathfinding _pathfinding;

    [Header("Combat")]
    [SerializeField] public Leader enemyLeader;
    [SerializeField] public List<Follower> enemiesFollowers;
    [SerializeField] public Bullet model;
    [SerializeField] private float bulletTimer;
    private float timer;
    public bool isBulletCooldown, canShoot;
    private void Start() 
    {
        _FSM = new FSM<FollowerStates>();
        _myMaterial = GetComponent<Renderer>().material;

        _pathToFollow = new List<Node>();
        _pathfinding = new Pathfinding();
        maxLife = 100;
        life = maxLife;
        _myMaterial.color = _originalColor;

        IState idle = new FollowerIdleState(_FSM, this);
        _FSM.AddState(FollowerStates.Idle, new FollowerIdleState(_FSM, this));

        _FSM.AddState(FollowerStates.Search, new FollowerSearchState(_FSM, this, _pathToFollow, _pathfinding));
        _FSM.AddState(FollowerStates.Retreat, new FollowerRetreatState(_FSM, this, _pathToFollow, _pathfinding));
        _FSM.AddState(FollowerStates.Arrive, new FollowerArriveState(_FSM, this));
        _FSM.AddState(FollowerStates.Flocking, new FollowerFlockingState(_FSM, this));

        if(this.gameObject.tag == "Team1")
        {
            FollowersManagerTeam1.Instance.RegisterNewFollower(this);
            enemiesFollowers = FollowersManagerTeam2.Instance.AllFollowers;
        }
        if(this.gameObject.tag == "Team2")
        {
            FollowersManagerTeam2.Instance.RegisterNewFollower(this);
            enemiesFollowers = FollowersManagerTeam1.Instance.AllFollowers;
        }
        
        _myArriveSteering = new ArriveSteering(transform, _maxSpeed, _maxForce, _arriveRadius);
        _mySeekSteering = new SeekSteering(transform, _maxSpeed, _maxForce);

        _mySeparationSteering = new Separation(transform, _maxSpeed, _maxForce);
        _myAlignmentSteering = new Alignment(transform, _maxSpeed, _maxForce);
        _myCohesionSteering = new Cohesion(transform, _maxSpeed, _maxForce);

        

        _FSM.ChangeState(FollowerStates.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        _FSM.Update();
        _FSM.FixedUpdate();

        if(canShoot)
        {
            if(isBulletCooldown)
                ShootCooldown();
            else
                Shoot();
        }
        
        
    }

    public void ReceiveDamage()
    {
        life = life - 10;
        _myMaterial.color = Color.red;
        Invoke("RestoreColor", 0.3f);
    }
    public void ChangeColor(Color newColor)
    {
        _myMaterial.color = newColor;
    }

    public void RestoreColor()
    {
        _myMaterial.color = _originalColor;
    }
    public void AddForce(Vector3 force)
    {
        _velocity += force;

        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
    }
    public void Shoot()
    {
        Bullet bullet = GameObject.Instantiate(model);
        if(this.gameObject.tag == "Team1")
            bullet.gameObject.tag = "Team1";
        if(this.gameObject.tag == "Team2")
            bullet.gameObject.tag = "Team2";
        bullet.Move(transform.position, transform.forward);
        bullet.parent = this.gameObject;
        
        isBulletCooldown = true;
        timer = 0;
    }
    public void ShootCooldown()
    {
        if(timer >= bulletTimer)
            isBulletCooldown = false;
        else
            timer = timer + 1 * Time.deltaTime;
    }
    public void SetGoalNode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(leader.position, 5f, nodes);
        float minDistance = 10f;
        
        foreach (var hitCollider in hitColliders)
        {
            Vector3 pos = hitCollider.GetComponent<Transform>().position;
            //Debug.Log(pos +"" + hitCollider);
            if(Vector3.Distance(leader.position, pos) < minDistance)
            {
                minDistance = Vector3.Distance(leader.position, pos);
                minCollider = hitCollider;
            }
        }
        _goalNode = minCollider.GetComponent<Node>();
    }
    public void SetStartingNode()
    {
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 5f, nodes);
        float minDistance = 10f;
        
        foreach (var hitCollider in hitColliders)
        {
            Vector3 pos = hitCollider.GetComponent<Transform>().position;
            //Debug.Log(pos +"" + hitCollider);
            if(Vector3.Distance(this.transform.position, pos) < minDistance)
            {
                minDistance = Vector3.Distance(this.transform.position, pos);
                minCollider = hitCollider;
            }
        }
        _startingNode = minCollider.GetComponent<Node>();
    }
    private void OnDrawGizmos()
    {  
        ///FOV Gizmos
        //Gizmos.color = Color.cyan;
        //Gizmos.DrawWireSphere(transform.position, _viewRadius);

        var realAngle = _viewAngle / 2;

        Gizmos.color = Color.magenta;
        Vector3 lineLeft = GetDirFromAngle(-realAngle + transform.eulerAngles.y);
        Gizmos.DrawLine(transform.position, transform.position + lineLeft * _viewRadius);

        Vector3 lineRight = GetDirFromAngle(realAngle + transform.eulerAngles.y);
        Gizmos.DrawLine(transform.position, transform.position + lineRight * _viewRadius);

        ///
        /// Obstacle Avoidance Gizmos
        for (int i = 0; i < numberOfRays; i++)
        {
            var rotation = this.transform.rotation;
            var rotationMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, this.transform.up);
            var direction = rotation * rotationMod * Vector3.forward;
            Gizmos.DrawRay(this.transform.position, direction);
        }
        ///
    }
    
    public void EvadeObstacles(Vector3 dist)
    {
        var deltaPosition = Vector3.zero;
        for (int i = 0; i < numberOfRays; i++)
        {
            var rotation = transform.rotation;
            var rotationMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, transform.up);
            var direction = rotation * rotationMod * Vector3.forward;
           
            var ray = new Ray(transform.position, direction);
            RaycastHit hitInfo;
            if(Physics.Raycast(ray, out hitInfo, 2))
                deltaPosition -= (1.0f / numberOfRays) * _maxSpeed * direction;
            else
                deltaPosition += (1.0f / numberOfRays) * _maxSpeed * direction;
        }
        transform.position += deltaPosition * Time.deltaTime;
        if(Vector3.Distance(transform.position, dist) < 1f)
            isEvadingObstacles = false;
    }
    public void EvadeWalls(Vector3 dist)
    {
        var deltaPosition = Vector3.zero;
        for (int i = 0; i < numberOfRays; i++)
        {
            var rotation = transform.rotation;
            var rotationMod = Quaternion.AngleAxis((i / ((float)numberOfRays - 1)) * angle * 2 - angle, transform.up);
            var direction = rotation * rotationMod * Vector3.forward;
           
            var ray = new Ray(transform.position, direction);
            RaycastHit hitInfo;
            if(Physics.Raycast(ray, out hitInfo, 2))
                deltaPosition -= (1.0f / numberOfRays) * _maxSpeed * direction;
            else
                deltaPosition += (1.0f / numberOfRays) * _maxSpeed * direction;
        }
        transform.position += deltaPosition * Time.deltaTime;
        if(Vector3.Distance(transform.position, dist) < 1f)
            isEvadingWalls = false;
    }
    Vector3 GetDirFromAngle(float angle)
    {
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    public bool InFieldOfView(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;

        //Que este dentro de la distancia maxima de vision
        if (dir.sqrMagnitude > _viewRadius * _viewRadius) return false;

        //Que no haya obstaculos
        //if (InLineOfSight(dir)) return false;

        //Que este dentro del angulo
        return Vector3.Angle(transform.forward, dir) <= _viewAngle/2;
    }

    public bool InLineOfSight(Vector3 end)
    {
        Vector3 dir = end - this.transform.position;
        RaycastHit hit;
        //Origen,radio, direccion, distancia maxima y layer mask
        //Debug.DrawLine(transform.position, leader.transform.position, Color.red);
        //return !Physics.Raycast(this.transform.position, dir, 99, walls);
        return !Physics.SphereCast(this.transform.position,0.5f, dir, out hit, dir.magnitude, walls);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 12)
        {
            //Debug.Log("Daño");
            if((this.gameObject.tag == "Team1" && other.gameObject.tag == "Team2") || (this.gameObject.tag == "Team2" && other.gameObject.tag == "Team1"))
            {
                ReceiveDamage();
            }
        }
    }
}
