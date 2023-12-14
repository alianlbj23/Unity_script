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
using System.Reflection;
using WebSocketSharp;
using MiniJSON;
// using System;


public class TrainingManager : MonoBehaviour
{
    string topicName = "/unity2Ros";
    string topicName_receive = "/ros2Unity";
    private WebSocket socket;
    private string rosbridgeServerUrl = "ws://localhost:9090";

    public Robot robot;
    // ------parameters that can be changed ---------------
    [SerializeField]
    GameObject anchor1, anchor2, anchor3, anchor4;
    Vector3[] outerPolygonVertices;
    [SerializeField]
    GameObject target;
    public float duration_of_timestep = 0.05f;
    public float currenttime = 0.0f;
    // ---------------------------------- end ---------------
    float new_target_coordinate_x;
    float new_target_coordinate_y;
    float new_car_coordinate_x;
    float new_car_coordinate_y;
    float target_reset_flag = 0;
    Transform base_footprint;
    Transform baselink;
    Vector3 carPos;
    enum Phase
    {
        Freeze,
        Run
    }
    Phase Timing_phase;

    Vector3 new_target_coordinate;
    Vector3 new_car_coordinate;
    public System.Random random = new System.Random();
    
    [System.Serializable]
    public class New_message_robot
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

    void Start()
    {
        // Initialization
        base_footprint = robot.transform.Find("base_link");
        baselink = robot.transform.Find("base_link");
        socket = new WebSocket(rosbridgeServerUrl);
        socket.OnOpen += (sender, e) =>
        {
            SubscribeToTopic(topicName_receive);
        };
        socket.OnMessage += OnWebSocketMessage;
        socket.Connect();
        MoveGameObject(target, new_target_coordinate);
        State state = updateState(new_target_coordinate);
        Send_to_ROS(state);
    }

    void Update()
    {
        // this function will be called in each frame
        if (target_reset_flag == 1)
        {
            reset_car_and_target_pos();
            target_reset_flag = 0;
        }
    }

    // When we get a case 1 action, we'll move the car and the target to another position.
    // After that, send the updated data back to AI through ROS
    void reset_car_and_target_pos()
    {
        change_car_and_target_pos();
        State state = updateState(new_target_coordinate);
        Send_to_ROS(state);
    }

    // When we get a case 0 action, we'll set new wheel speeds and need
    // some time (duration_of_timestep) to update the environment. -----------
    void Start_Timing_for_setting_speed();
    {
        Timing_phase = Phase.Run;
        currenttime = 0;
        Time.timeScale = 1;
    }

    void Time_up_and_send_to_ROS();
    {
        Timing_phase = Phase.Freeze;
        State state = updateState(new_target_coordinate);
        Send_to_ROS(state);
    }
    // --------------------------------------------------------------------------

    private void Receive_from_ROS(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        New_message_robot message = JsonUtility.FromJson<New_message_robot>(jsonString);
        float[] data = message.msg.data;
        switch (data[0])
        {
            case 0:
                Robot.Action action = new Robot.Action();
                action.voltage = new List<float>();
                action.voltage.Add((float)data[1]);
                action.voltage.Add((float)data[2]);
                robot.DoAction(action);
                Start_Timing_for_setting_speed();
                break;
            case 1:
                target_reset_flag = 1;
                break;
        }
    }
    
    // This function will be called in each time duration (can set yourself)
    void FixedUpdate()
    {
        if (Timing_phase == Phase.Run)
            currenttime += Time.fixedDeltaTime;
        if (Timing_phase == Phase.Run && currenttime >= duration_of_timestep)
        {
            Time_up_and_send_to_ROS();
        }
    }

    private float randomFloat(float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    }

    void Send_to_ROS(object data)
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
        base_footprint.GetComponent<ArticulationBody>().TeleportRoot(pos, Quaternion.identity);
    }

    State updateState(Vector3 new_target_coordinate)
    {
        State state = robot.GetState(new_target_coordinate);
        System.Type type = state.GetType();

        return state;
    }

    private void SubscribeToTopic(string topic)
    {
        string subscribeMessage = "{\"op\":\"subscribe\",\"id\":\"1\",\"topic\":\"" + topic + "\",\"type\":\"std_msgs/msg/Float32MultiArray\"}";
        socket.Send(subscribeMessage);
    }

    void change_car_and_target_pos()
    {
        carPos = baselink.GetComponent<ArticulationBody>().transform.position;
        outerPolygonVertices = new Vector3[]{
            anchor1.transform.position,
            anchor2.transform.position,
            anchor3.transform.position,
            anchor4.transform.position
        };
        new_car_coordinate_x = Random.Range(-3.0f, 3.0f);
        new_car_coordinate_x = abs_biggerthan1(new_car_coordinate_x);

        new_car_coordinate_y = Random.Range(-3.0f, 3.0f);
        new_car_coordinate_y = abs_biggerthan1(new_car_coordinate_y);

        new_car_coordinate = new Vector3(carPos[0] + new_car_coordinate_x, carPos[1], carPos[2] + new_car_coordinate_y);
        while (!IsPointInsidePolygon(new_car_coordinate, outerPolygonVertices))
        {
            new_car_coordinate_x = Random.Range(-3.0f, 3.0f);
            new_car_coordinate_x = abs_biggerthan1(new_car_coordinate_x);
            new_car_coordinate_y = Random.Range(-3.0f, 3.0f);
            new_car_coordinate_y = abs_biggerthan1(new_car_coordinate_y);
            new_car_coordinate = new Vector3(carPos[0] + new_car_coordinate_x, 0, carPos[2] + new_car_coordinate_y);
        }
        MoveRobot(new_car_coordinate);

        new_target_coordinate_x = Random.Range(-3.0f, 3.0f);
        new_target_coordinate_x = abs_biggerthan1(new_target_coordinate_x);

        new_target_coordinate_y = Random.Range(-3.0f, 3.0f);
        new_target_coordinate_y = abs_biggerthan1(new_target_coordinate_y);
        new_target_coordinate = new Vector3(new_car_coordinate[0] + new_target_coordinate_x, 0, new_car_coordinate[2] + new_target_coordinate_y);
        while (!IsPointInsidePolygon(new_target_coordinate, outerPolygonVertices))
        {
            new_target_coordinate_x = Random.Range(-3.0f, 3.0f);
            new_target_coordinate_x = abs_biggerthan1(new_target_coordinate_x);
            new_target_coordinate_y = Random.Range(-3.0f, 3.0f);
            new_target_coordinate_y = abs_biggerthan1(new_target_coordinate_y);
            new_target_coordinate = new Vector3(carPos[0] + new_target_coordinate_x, 0, carPos[2] + new_target_coordinate_y);

        }
        MoveGameObject(target, new_target_coordinate);
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
