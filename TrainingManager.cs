using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
// using System;
using System.Reflection;
using WebSocketSharp;
using MiniJSON;


public class TrainingManager : MonoBehaviour
{
    string topicName = "/Unity2Trainer";
    string topicName_receive = "/Trainer2Unity";

    private WebSocket socket;

    private string rosbridgeServerUrl = "ws://localhost:9090";

    Thread t;

    public Robot robot;

    [SerializeField]
    GameObject anchor1, anchor2, anchor3, anchor4;
    Vector3[] outerPolygonVertices;

    [SerializeField]
    GameObject target;

    enum Phase
    {
        Freeze,
        Run
    }
    Phase phase;
    public float stepTime = 0.05f; //0.1f
    public float currentStepTime = 0.0f;

    Vector3 newTarget;
    Vector3 newTarget_car;

    public System.Random random = new System.Random();

    Transform base_footprint;

    [System.Serializable]
    public class RobotNewsMessage
    {
        public string op;
        public string topic;
        public MessageData msg;
    }
    [System.Serializable]
    public class MessageData
    {
        public LayoutData layout;
        public float[] data;
    }
    [System.Serializable]
    public class LayoutData
    {
        public int[] dim;
        public int data_offset;
    }
    
    Transform baselink;
    Vector3 carPos;
    float target_x;
    float target_y;
    float target_x_car;
    float target_y_car;
    float target_change_flag = 0;


    void Awake()
    {
        base_footprint = robot.transform.Find("base_link");
    }

    void Start()
    {
        baselink = robot.transform.Find("base_link");
        socket = new WebSocket(rosbridgeServerUrl);
        socket.OnOpen += (sender, e) =>
        {
            SubscribeToTopic(topicName_receive);
        };
        socket.OnMessage += OnWebSocketMessage;
        socket.Connect();
        MoveGameObject(target, newTarget);
        State state = updateState(newTarget, true);
        Send(state);
    }



    void Update()
    {
        if (target_change_flag == 1)
        {
            change_target();
            target_change_flag = 0;
        }
    }

    void change_target()
    {
        carPos = baselink.GetComponent<ArticulationBody>().transform.position;
        outerPolygonVertices = new Vector3[]{
            anchor1.transform.position,
            anchor2.transform.position,
            anchor3.transform.position,
            anchor4.transform.position
        };
        // -------------------------------------
        target_x_car = Random.Range(-3.0f, 3.0f);
        target_x_car = abs_biggerthan1(target_x_car);

        target_y_car = Random.Range(-3.0f, 3.0f);
        target_y_car = abs_biggerthan1(target_y_car);

        newTarget_car = new Vector3(carPos[0] + target_x_car, carPos[1], carPos[2] + target_y_car);
        while (!IsPointInsidePolygon(newTarget_car, outerPolygonVertices))
        {
            target_x_car = Random.Range(-3.0f, 3.0f);
            target_x_car = abs_biggerthan1(target_x_car);
            target_y_car = Random.Range(-3.0f, 3.0f);
            target_y_car = abs_biggerthan1(target_y_car);
            newTarget_car = new Vector3(carPos[0] + target_x_car, 0, carPos[2] + target_y_car);
        }
        // MoveGameObject(robot.GameObject, newTarget_car); // check
        MoveRobot(newTarget_car);
        // ------------------------------------------

        target_x = Random.Range(-3.0f, 3.0f);
        target_x = abs_biggerthan1(target_x);

        target_y = Random.Range(-3.0f, 3.0f);
        target_y = abs_biggerthan1(target_y);
        newTarget = new Vector3(newTarget_car[0] + target_x, 0, newTarget_car[2] + target_y);
        while (!IsPointInsidePolygon(newTarget, outerPolygonVertices))
        {
            target_x = Random.Range(-3.0f, 3.0f);
            target_x = abs_biggerthan1(target_x);
            target_y = Random.Range(-3.0f, 3.0f);
            target_y = abs_biggerthan1(target_y);
            newTarget = new Vector3(carPos[0] + target_x, 0, carPos[2] + target_y);

        }
        MoveGameObject(target, newTarget);

        State state = updateState(newTarget, false);
        Debug.Log("carPosition: " + state.carPosition);
        Debug.Log("ROS2TargetPosition: " + state.ROS2TargetPosition);

        StartStep();
        // Send(state);
    }

    private float abs_biggerthan1(float random)
    {
        if (random <= 1 && random >= -1)
        {
            if (random > 0)
            {
                random += 1;
            }
            else
            {
                random -= 1;
            }
        }
        return random;
    }
    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        RobotNewsMessage message = JsonUtility.FromJson<RobotNewsMessage>(jsonString);
        float[] data = message.msg.data;

        switch (data[0])
        {
            case 0:
                Debug.Log("set wheel speed (left, right): "+data[1]+"  "+data[2]);
                Robot.Action action = new Robot.Action();
                action.voltage = new List<float>();
                action.voltage.Add((float)data[1]);
                action.voltage.Add((float)data[2]);
                robot.DoAction(action);
                StartStep();
                break;
            case 1:
                target_change_flag = 1;
                break;
        }
        //DO: receive data from AI model
    }
    // Update is called once per frame

    void FixedUpdate()
    {
        if (phase == Phase.Run)
            currentStepTime += Time.fixedDeltaTime;
        if (phase == Phase.Run && currentStepTime >= stepTime)
        {
            EndStep();
        }
    }

    void StartStep()
    {
        phase = Phase.Run;
        currentStepTime = 0;
        Time.timeScale = 1;
    }

    void EndStep()
    {
        phase = Phase.Freeze;
        State state = updateState(newTarget, false);
        Send(state);
    }

    private float randomFloat(float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }

    void Send(object data)
    {
        var properties = typeof(State).GetProperties();
        Dictionary<string, object> stateDict = new Dictionary<string, object>();
        foreach (var property in properties)
        {
            string propertyName = property.Name;
            var value = property.GetValue(data);
            stateDict[propertyName] = value;
        }

        string dictData = MiniJSON.Json.Serialize(stateDict);
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "publish" },
            { "id", "1" },
            { "topic", topicName },
            { "msg", new Dictionary<string, object>
                {
                    { "data", dictData}
                }
           }
        };

        string jsonMessage = MiniJSON.Json.Serialize(message);

        try
        {
            socket.Send(jsonMessage);

        }
        catch
        {
            Debug.Log("error-send");
        }
    }

    void MoveGameObject(GameObject obj, Vector3 pos)
    {
        obj.transform.position = pos;
    }

    void MoveRobot(Vector3 pos)
    {
        // Transform baselink = robot.transform.Find("base_footprint");
        // Transform baselink = robot.transform.Find("base_link");
        base_footprint.GetComponent<ArticulationBody>().TeleportRoot(pos, Quaternion.identity);
    }

    State updateState(Vector3 newTarget, bool isFirst)
    {
        State state = robot.GetState(newTarget, isFirst);
        // System.Type type = state.GetType();

        return state;
    }


    private void SubscribeToTopic(string topic)
    {
        string subscribeMessage = "{\"op\":\"subscribe\",\"id\":\"1\",\"topic\":\"" + topic + "\",\"type\":\"std_msgs/msg/Float32MultiArray\"}";
        socket.Send(subscribeMessage);
    }


    bool IsPointInsidePolygon(Vector3 point, Vector3[] polygonVertices)
    {
        Debug.Log("IsPointInsidePolygon called" + point);
        int polygonSides = polygonVertices.Length;
        bool isInside = false;

        for (int i = 0, j = polygonSides - 1; i < polygonSides; j = i++)
        {
            if (((polygonVertices[i].z <= point.z && point.z < polygonVertices[j].z) ||
                (polygonVertices[j].z <= point.z && point.z < polygonVertices[i].z)) &&
                (point.x < (polygonVertices[j].x - polygonVertices[i].x) * (point.z - polygonVertices[i].z) / (polygonVertices[j].z - polygonVertices[i].z) + polygonVertices[i].x))
            {
                isInside = !isInside;
            }
        }
        return isInside;
    }
}
