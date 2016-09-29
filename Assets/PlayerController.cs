using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMovementPhotonView))]
public class PlayerController : MonoBehaviour
{
    public GameObject targetPrefab;
    private GameObject _target;
    private GameObject target
    {
        get
        {
            return _target;
        }
        set
        {
            if (value == null)
            {
                movementManager.targetPosition = null;
            }
            else
            {
                movementManager.targetPosition = value.transform.position;
            }
            _target = value;
        }
    }
    PlayerMovementPhotonView movementManager;

    public bool isServer;

    private Rigidbody myRigidbody;

    void Start()
    {
        movementManager = GetComponent<PlayerMovementPhotonView>();
        myRigidbody = GetComponent<Rigidbody>();
        target = null;
    }

    void Update()
    {
        if (isServer)
        {
            if (Input.GetMouseButton(1))
            {
                CreateTarget();
            }
        }
    }

    internal void DestroyTarget()
    {
        Destroy(target);
        target = null;
    }

    public void CreateTarget()
    {
        Vector3 position = Functions.GetMouseWorldPosition();
        CreateTarget(position);
    }

    public void CreateTarget(Vector3 position)
    {
        DestroyTarget();
        target = (GameObject)Instantiate(targetPrefab, position, Quaternion.identity);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Respawn")
        {
            DestroyTarget();
        }
    }

    //public void Init(int teamNumber, string name)
    //{
    //    spawningPoint = GameObject.FindGameObjectWithTag(Tags.Spawns).transform.GetChild(teamNumber).position;
    //    photonView.RPC("InitPlayer", PhotonTargets.AllBufferedViaServer, teamNumber, name, spawningPoint);
    //}

    //[PunRPC]
    //private void InitPlayer(int teamNumber, string name, Vector3 spawningPoint)
    //{
    //    if (photonView.isMine)
    //    {
    //        tag = Tags.MyPlayer;
    //    }
    //    this.teamNumber = teamNumber;
    //    gameObject.name = name;
    //    this.spawningPoint = spawningPoint;
    //    transform.position = spawningPoint;
    //    transform.LookAt(Vector3.zero);

    //    if (teamNumber == 0)
    //    {
    //        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) { renderer.material = ResourcesGetter.BlueMaterial(); }
    //    }
    //    else
    //    {
    //        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) { renderer.material = ResourcesGetter.RedMaterial(); }
    //    }
    //    gameObject.layer = LayersGetter.players[teamNumber];
    //    foreach (Transform go in transform) { go.gameObject.layer = LayersGetter.players[teamNumber]; };
    //}
}
