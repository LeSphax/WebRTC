using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
class PlayerMovementPhotonView : MonoBehaviour
{

    Rigidbody myRigidbody;

    private float speed = 70;

    public static float MAX_VELOCITY = 45;
    private float ANGULAR_SPEED = 400;
    public Vector3? targetPosition;
    private int currentId = 0;

    private const float FRAME_DURATION = 0.02f;
    PlayerController controller;

    private double simulationTime
    {
        get
        {
            if (controller.isServer)
            {
                //return PhotonNetwork.time;
            }
            else
            {
                //return PhotonNetwork.time - ClientDelay.Delay;
            }
            return 0;
        }
    }

    private Queue<PlayerPacket> StateBuffer = new Queue<PlayerPacket>();
    private PlayerPacket? currentPacket = null;

    private PlayerPacket? nextPacket
    {
        get
        {
            if (StateBuffer.Count > 0)
            {
                return StateBuffer.Peek();
            }
            else
            {
                return currentPacket;
            }
        }
    }

    void Awake()
    {
        myRigidbody = GetComponent<Rigidbody>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {

        float delay = 0f;
        if (!controller.isServer)
            delay = (float)0.1f;
        Invoke("StartUpdating", delay);
    }

    private bool startUpdating = false;

    private void StartUpdating() { startUpdating = true; }

    void FixedUpdate()
    {
        if (startUpdating)
        {
            if (controller.isServer)
            {
                OwnerUpdate();
            }
            else
            {
                //SimulationUpdate();
            }
        }
    }

    private void OwnerUpdate()
    {
        if (targetPosition != null)
        {
            var lookPos = targetPosition.Value - transform.position;
            lookPos.y = 0;
            var targetRotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, FRAME_DURATION * ANGULAR_SPEED);
            myRigidbody.AddForce(transform.forward * speed * FRAME_DURATION, ForceMode.VelocityChange);

            ClampPlayerVelocity();
        }
        PlayerPacket packet = CreatePacket();
        Debug.Log(packet.position);
        packet = PlayerPacket.Deserialize(packet.Serialize());
        Debug.Log(packet.position);
        Functions.GetNetworkManagement().SendData(packet.Serialize());
        //myRigidbody.CustomUpdate();
    }

    private PlayerPacket CreatePacket()
    {
        PlayerPacket packet = new PlayerPacket();
        packet.velocity = myRigidbody.velocity;
        packet.position = transform.position;
        packet.rotation = transform.rotation;
        packet.id = currentId;
        return packet;
    }

    public void ClampPlayerVelocity()
    {
        myRigidbody.velocity *= Mathf.Min(1.0f, MAX_VELOCITY / myRigidbody.velocity.magnitude);
    }

    private void SimulationUpdate()
    {
        if (StateBuffer.Count > 0)
        {
            if (currentId >= StateBuffer.Peek().id)
            {
                currentPacket = StateBuffer.Dequeue();
                //Debug.Log("Packet Consumed " + currentPacket.Value.id);
            }
        }
        else
        {
            Debug.LogError("No Packet in buffer !!! " + currentPacket.Value.id + "   " + gameObject.name);
        }

        if (currentPacket != null)
        {
            double deltaTime = (nextPacket.Value.id - currentPacket.Value.id) * FRAME_DURATION;
            float completion = 0;
            if (deltaTime != 0)
                completion = (float)((simulationTime - currentPacket.Value.timeSent) / deltaTime);
            transform.position = Vector3.Lerp(currentPacket.Value.position, nextPacket.Value.position, completion);
            transform.rotation = Quaternion.Lerp(currentPacket.Value.rotation, nextPacket.Value.rotation, completion);
        }
        else
        {
            Debug.LogWarning("No Packets for currentId " + currentId);
        }
        currentId++;
    }


    public void ReceiveData(byte[] data)
    {
        PlayerPacket newPacket = PlayerPacket.Deserialize(data);
        transform.position = newPacket.position;
        transform.rotation = newPacket.rotation;
        //StateBuffer.Enqueue(newPacket);
    }
}

[Serializable]
public struct PlayerPacket
{
    public Vector3 velocity;
    public Vector3 position;
    public Quaternion rotation;
    public double timeSent;
    public int id;

    public byte[] Serialize()
    {
        return Functions.ObjectToByteArray(this);
    }
    
    public static PlayerPacket Deserialize(byte[] data)
    {
        return (PlayerPacket)Functions.ByteArrayToObject(data);
    }
}

