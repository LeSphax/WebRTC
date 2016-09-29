using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System;
using Byn.Net;
using System.Collections.Generic;
using Byn.Common;

public class NetworkManagement : MonoBehaviour
{
    /* 
 * Copyright (C) 2015 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */



    /// <summary>
    /// This is a test server. Don't use in production! The server code is in a zip file in WebRtcNetwork
    /// </summary>
    public string uSignalingUrl = "wss://because-why-not.com:12777/chatapp";

    /// <summary>
    /// Mozilla stun server. Used to get trough the firewall and establish direct connections.
    /// Replace this with your own production server as well. 
    /// </summary>
    public string uStunServer = "stun:stun.l.google.com:19302";

    /// <summary>
    /// Set true to use send the WebRTC log + wrapper log output to the unity log.
    /// </summary>
    public bool uLog = false;

    /// <summary>
    /// Debug console to be able to see the unity log on every platform
    /// </summary>
    public bool uDebugConsole = false;

    #region UI
    /// <summary>
    /// Input field used to enter the room name.
    /// </summary>
    public InputField uRoomName;

    /// <summary>
    /// Join button to connect to a server.
    /// </summary>
    public Button uJoin;


    /// <summary>
    /// Open room button to start a server.
    /// </summary>
    public Button uOpenRoom;

    public GameObject playerPrefab;

    #endregion
    /// <summary>
    /// The network interface.
    /// This can be native webrtc or the browser webrtc version.
    /// (Can also be the old or new unity network but this isn't part of this package)
    /// </summary>
    private IBasicNetwork mNetwork = null;

    /// <summary>
    /// True if the user opened an own room allowing incoming connections
    /// </summary>
    private bool mIsServer = false;

    /// <summary>
    /// Keeps track of all current connections
    /// </summary>
    private List<ConnectionId> mConnections = new List<ConnectionId>();


    private const int MAX_CODE_LENGTH = 256;

    private PlayerMovementPhotonView view;

    private bool started = false;


    /// <summary>
    /// Will setup webrtc and create the network object
    /// </summary>
	private void Start()
    {
        //shows the console on all platforms. for debugging only
        if (uDebugConsole)
            DebugHelper.ActivateConsole();
        if (uLog)
            SLog.SetLogger(OnLog);

        SLog.LV("Verbose log is active!");
        SLog.LD("Debug mode is active");

        Append("Setting up WebRtcNetworkFactory");
        WebRtcNetworkFactory factory = WebRtcNetworkFactory.Instance;
        if (factory != null)
            Append("WebRtcNetworkFactory created");

    }
    private void OnLog(object msg, string[] tags)
    {
        StringBuilder builder = new StringBuilder();
        TimeSpan time = DateTime.Now - DateTime.Today;
        builder.Append(time);
        builder.Append("[");
        for (int i = 0; i < tags.Length; i++)
        {
            if (i != 0)
                builder.Append(",");
            builder.Append(tags[i]);
        }
        builder.Append("]");
        builder.Append(msg);
        Debug.Log(builder.ToString());
    }

    private void Setup()
    {
        Append("Initializing webrtc network");


        mNetwork = WebRtcNetworkFactory.Instance.CreateDefault(uSignalingUrl, new string[] { uStunServer });
        if (mNetwork != null)
        {
            Append("WebRTCNetwork created");
        }
        else
        {
            Append("Failed to access webrtc ");
        }
        SetGuiState(false);
    }

    private void Reset()
    {
        Debug.Log("Cleanup!");

        mIsServer = false;
        mConnections = new List<ConnectionId>();
        Cleanup();
        SetGuiState(true);
    }

    /// <summary>
    /// called during reset and destroy
    /// </summary>
    private void Cleanup()
    {
        mNetwork.Dispose();
        mNetwork = null;
    }

    private void OnDestroy()
    {
        if (mNetwork != null)
        {
            Cleanup();
        }
    }

    private void FixedUpdate()
    {
        //check each fixed update if we have got new events
        HandleNetwork();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendString("Jambon");
        }
    }
    private void HandleNetwork()
    {
        //check if the network was created
        if (mNetwork != null)
        {
            //first update it to read the data from the underlaying network system
            mNetwork.Update();

            //handle all new events that happened since the last update
            NetworkEvent evt;
            //check for new messages and keep checking if mNetwork is available. it might get destroyed
            //due to an event
            while (mNetwork != null && mNetwork.Dequeue(out evt))
            {
                switch (evt.Type)
                {
                    case NetEventType.ServerInitialized:
                        {
                            //server initialized message received
                            //this is the reaction to StartServer -> switch GUI mode
                            mIsServer = true;
                            string address = evt.Info;
                            Append("Server started. Address: " + address);
                            uRoomName.text = "" + address;
                            StartGame();
                        }
                        break;
                    case NetEventType.ServerInitFailed:
                        {
                            //user tried to start the server but it failed
                            //maybe the user is offline or signaling server down?
                            mIsServer = false;
                            Append("Server start failed.");
                            Reset();
                        }
                        break;
                    case NetEventType.ServerClosed:
                        {
                            //server shut down. reaction to "Shutdown" call or
                            //StopServer or the connection broke down
                            mIsServer = false;
                            Append("Server closed. No incoming connections possible until restart.");
                        }
                        break;
                    case NetEventType.NewConnection:
                        {
                            mConnections.Add(evt.ConnectionId);
                            //either user runs a client and connected to a server or the
                            //user runs the server and a new client connected
                            Append("New local connection! ID: " + evt.ConnectionId);

                            //if server -> send announcement to everyone and use the local id as username
                            if (mIsServer)
                            {
                                //user runs a server. announce to everyone the new connection
                                //using the server side connection id as identification
                                string msg = "New user " + evt.ConnectionId + " joined the room.";
                                Append(msg);
                                SendString(msg);
                            }
                            StartGame();
                        }
                        break;
                    case NetEventType.ConnectionFailed:
                        {
                            //Outgoing connection failed. Inform the user.
                            Append("Connection failed");
                            Reset();
                        }
                        break;
                    case NetEventType.Disconnected:
                        {
                            mConnections.Remove(evt.ConnectionId);
                            //A connection was disconnected
                            //If this was the client then he was disconnected from the server
                            //if it was the server this just means that one of the clients left
                            Append("Local Connection ID " + evt.ConnectionId + " disconnected");
                            if (mIsServer == false)
                            {
                                Reset();
                            }
                            else
                            {
                                string userLeftMsg = "User " + evt.ConnectionId + " left the room.";

                                //show the server the message
                                Append(userLeftMsg);

                                //other users left? inform them 
                                if (mConnections.Count > 0)
                                {
                                    SendString(userLeftMsg);
                                }
                            }
                        }
                        break;
                    case NetEventType.ReliableMessageReceived:
                        break;
                    case NetEventType.UnreliableMessageReceived:
                        {
                            HandleIncommingMessage(ref evt);
                        }
                        break;
                }
            }

            //finish this update by flushing the messages out if the network wasn't destroyed during update
            if (mNetwork != null)
                mNetwork.Flush();
        }
    }

    private void StartGame()
    {
        if (!started)
        {
            started = true;
            uOpenRoom.gameObject.SetActive(false);
            uJoin.gameObject.SetActive(false);
            uRoomName.gameObject.SetActive(false);
            GameObject player = (GameObject)Instantiate(playerPrefab);
            player.GetComponent<PlayerController>().isServer = mIsServer;
            view = player.GetComponent<PlayerMovementPhotonView>();
        }
    }

    private void HandleIncommingMessage(ref NetworkEvent evt)
    {
        MessageDataBuffer buffer = evt.MessageData;

        if (mIsServer)
        {
            //Append("Received"+msg);
        }
        else
        {
            //Append(msg);
            Debug.Log("Received");
            view.ReceiveData(buffer.Buffer);
        }

        //return the buffer so the network can reuse it
        buffer.Dispose();
    }


    /// <summary>
    /// Sends a string as UTF8 byte array to all connections
    /// </summary>
    /// <param name="msg">String containing the message to send</param>
    /// <param name="reliable">false to use unreliable messages / true to use reliable messages</param>
    private void SendString(string msg, bool reliable = true)
    {
        if (msg.Split(':').Length == 2)
        {
            msg = msg.Split(':')[1];
        }
        Debug.Log(msg);
        if (mNetwork == null || mConnections.Count == 0)
        {
            Append("No connection. Can't send message.");
        }
        else if (msg == "/ping")
        {
            Debug.Log("Send Ping");
            byte[] msgData = Encoding.UTF8.GetBytes("ping@" + Time.realtimeSinceStartup);
            foreach (ConnectionId id in mConnections)
            {
                mNetwork.SendData(id, msgData, 0, msgData.Length, reliable);
            }
        }
        else
        {
            byte[] msgData = Encoding.UTF8.GetBytes(msg);
            foreach (ConnectionId id in mConnections)
            {
                mNetwork.SendData(id, msgData, 0, msgData.Length, reliable);
            }
        }
    }

    public void SendData(byte[] data)
    {
        foreach (ConnectionId id in mConnections)
        {
            mNetwork.SendData(id, data, 0, data.Length, false);
        }
    }

    #region UI

    private void OnGUI()
    {
        //draws the debug console (or the show button in the corner to open it)
        DebugHelper.DrawConsole();
    }

    /// <summary>
    /// Adds a new message to the message view
    /// </summary>
    /// <param name="text"></param>
    private void Append(string text)
    {
        Debug.Log("chat: " + text);
    }


    /// <summary>
    /// Changes the gui depending on if the user is connected
    /// or disconnected
    /// </summary>
    /// <param name="showSetup">true = user is connected. false = user isn't connected</param>
    private void SetGuiState(bool showSetup)
    {
        uJoin.interactable = showSetup;
        uOpenRoom.interactable = showSetup;
    }

    /// <summary>
    /// Join button pressed. Tries to join a room.
    /// </summary>
    public void JoinRoomButtonPressed()
    {
        Setup();
        mNetwork.Connect(uRoomName.text);
        Append("Connecting to " + uRoomName.text + " ...");
    }
    private void EnsureLength()
    {
        if (uRoomName.text.Length > MAX_CODE_LENGTH)
        {
            uRoomName.text = uRoomName.text.Substring(0, MAX_CODE_LENGTH);
        }
    }

    /// <summary>
    /// Open room button pressed.
    /// 
    /// Opens a room / starts a server
    /// </summary>
    public void OpenRoomButtonPressed()
    {
        Setup();
        EnsureLength();
        mNetwork.StartServer(uRoomName.text);

        Debug.Log("StartServer " + uRoomName.text);
    }
    #endregion
}
