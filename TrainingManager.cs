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
    string topicName = "/unity2Ros"; 
    string topicName_receive = "/ros2Unity"; 
    
    private WebSocket socket;
    private string rosbridgeServerUrl = "ws://localhost:9090";

    
    // string host = "127.0.0.1"; //; "localhost"
    // public int port = 5060;
    // Socket client;

    const int messageLength = 10000; //12000
    byte[] messageHolder = new byte[messageLength];
    readonly ConcurrentQueue<string> inMessage = new ConcurrentQueue<string>();

    Thread t;

    public Robot robot;
    


    [SerializeField]
    GameObject target;

    [SerializeField]
    GameObject trailTarget;

    [SerializeField]
    GameObject trailClosest;

    [SerializeField]
    GameObject trailSecond;

    [SerializeField]
    GameObject obstacle1;

    [SerializeField]
    GameObject obstacle2;


    enum Phase {
        Freeze,
        Run
    } 
    Phase phase;
    public float stepTime = 0.05f; //0.1f
    public float currentStepTime = 0.0f;

    Vector3 newTarget;


    public BezierCurve curver;

    public System.Random random = new System.Random();

    Transform base_footprint;

    // List<float> radius = new List<float>{4f, 4.5f, 5f};
    float radius = 4.5f;
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
    void Awake()
    {
        base_footprint = robot.transform.Find("base_link");
    }

    Transform baselink;
    Vector3 carPos;

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

        State state = updateState(newTarget, curver);
        
        Send(state);

    }

    float target_x;
    float target_y;
    
    
    float target_change_flag = 0;
    
    void Update()
    {  

        if(target_change_flag == 1){

            change_target();
            target_change_flag = 0;
        }   
        
                
                          
    }
        
    

    void change_target()
    {
        target_x = Random.Range(-3.0f, 3.0f);//broken TODO
            
        if (target_x <= 1 && target_x >= -1) {
            if (target_x > 0) {
                target_x += 1;
            } else {
                target_x -= 1;
            }
        }

        float target_y = Random.Range(-3.0f, 3.0f);
        if (target_y <= 1 && target_y >= -1) {
            if (target_y > 0) {
                target_y += 1;
            } else {
                target_y -= 1;
            }   
        }
        

        carPos = baselink.GetComponent<ArticulationBody>().transform.position;
        newTarget = new Vector3(carPos[0]+target_x, 0, carPos[2]+target_y);
        Debug.Log("newTarget: "+newTarget);
        MoveGameObject(target, newTarget);
            
        State state = updateState(newTarget, curver);
        Debug.Log("ROS2TargetPosition: "+state.ROS2TargetPosition);
        Send(state);
        
    }

    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        
        RobotNewsMessage message = JsonUtility.FromJson<RobotNewsMessage>(jsonString);

        
        float[] data = message.msg.data;
        
        switch (data[0]) {
            case 0:
                
                Robot.Action action = new Robot.Action();
                action.voltage = new List<float>();

                action.voltage.Add((float)data[1]);
                
                action.voltage.Add((float)data[2]);

                robot.DoAction(action);
                StartStep();

                break;
                
            case 1:
                
                // robot.trailRenderer.Clear();
                
                // float target_x = Random.Range(-3.0f, 3.0f);//broken TODO
                // Debug.Log(target_x);
                //     if (target_x <= 1 && target_x >= -1) {
                //        if (target_x > 0) {
                //             target_x += 1;
                //         } else {
                //             target_x -= 1;
                //         }
                            
                //     }
                // Debug.Log("target_x");
                // float target_y = Random.Range(-3.0f, 3.0f);
                // if (target_y <= 1 && target_y >= -1) {
                //     if (target_y > 0) {
                //         target_y += 1;
                //     } else {
                //         target_y -= 1;
                //     }   
                // }
                target_change_flag = 1;
                // Transform baselink = robot.transform.Find("base_link");
                Debug.Log("new target: "+ newTarget);
                // var carPos = baselink.GetComponent<ArticulationBody>().transform.position;
                
                
                
                // MoveGameObject(target, newTarget);
                
                // State state = updateState(newTarget, curver);
                // Debug.Log("new target!!!!!!!!!!!!!!!");
                // Debug.Log("carPosition: "+state.carPosition);
                // Debug.Log("targetPosition: "+state.ROS2TargetPosition);
                // Send(state);
                
                break;
            }

        //TODO: receive data from AI model

        
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

        

        State state = updateState(newTarget, curver);
        Send(state);
        
        // Debug.Log(state.carPosition);
        // Debug.Log(state.ROS2TargetPosition);
                
    }

    

    private float randomFloat(float min, float max)
    {
        return (float)(random.NextDouble() * (max - min) + min);
    } 

    private List<Vector3> findPeak(Vector3 start, Vector3 end) {
        Vector3 mid = (start + end)/2;
        float minX = Mathf.Min(start[0], end[0]);
        float maxX = Mathf.Max(start[0], end[0]);
        float minY = Mathf.Min(start[2], end[2]);
        float maxY = Mathf.Max(start[2], end[2]);

        // float minPortion0 = randomFloat(0.2f, 0.8f);
        // float minPortion1 = randomFloat(0.2f, 0.8f);
        // while (Mathf.Abs(minPortion1 - minPortion0) <= 0.3) {
        //     minPortion1 = randomFloat(0.2f, 0.8f);
        // }
        // Vector3 c0 = new Vector3(minX + minPortion0 * (maxX - minX), start[1], randomFloat(minY, minY*1.3f));
        // Vector3 c1 = new Vector3(minX + minPortion1 * (maxX - minX), start[1], randomFloat(maxY, maxY*1.3f));
        
        //https://cubic-bezier.com/#.17,.67,.83,.67
        Vector3 c0 = new Vector3(randomFloat(minX, maxX), start[1], randomFloat(minY, minY + Mathf.Abs(maxY - minY)*0.7f));
        Vector3 c1 = new Vector3(randomFloat(minX, maxX), start[1], randomFloat(maxY - Mathf.Abs(maxY - minY)*0.7f, maxY));

        List<Vector3> result = new List<Vector3>{c0, c1};
        return result;
    }

    void Send(object data)
    {   
        List<float> send_to_python = new List<float>();
        var properties = typeof(State).GetProperties();
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(Vector3) || property.PropertyType == typeof(Quaternion) || property.PropertyType == typeof(float))
            {
                var value = property.GetValue(data);

                if (property.PropertyType == typeof(Vector3))
                {
                    var vector3Value = (Vector3)value;
                    send_to_python.Add(vector3Value.x);
                    send_to_python.Add(vector3Value.y);
                    send_to_python.Add(vector3Value.z);
                }
                // 如果值是 Quaternion，将其分解为 x、y、z、w
                else if (property.PropertyType == typeof(Quaternion))
                {
                    var quaternionValue = (Quaternion)value;
                    send_to_python.Add(quaternionValue.x);
                    send_to_python.Add(quaternionValue.y);
                    send_to_python.Add(quaternionValue.z);
                    send_to_python.Add(quaternionValue.w);
                }
                // 如果值是 float，直接添加到列表中
                else if (property.PropertyType == typeof(float))
                {
                    send_to_python.Add((float)value);
                }
            }
        }
        // Debug.Log("publish to ros topic name : " + topicName);
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "publish" },
            { "id", "1" },
            { "topic", topicName },
            { "msg", new Dictionary<string, object>
                {
                    { "layout", new Dictionary<string, object>
                        {
                            { "dim", new object[] { } },
                            { "data_offset", 0 }
                        }
                    },
                    { "data", send_to_python.ToArray() }
                }
           }
        };
        string jsonMessage = MiniJSON.Json.Serialize(message);
        try{
            socket.Send(jsonMessage);
            // Debug.Log("Send messages");
            // Debug.Log("send message to ROS Bridge: " + jsonMessage);
        }
            
        catch{
            Debug.Log("error-send");
        }
    }



    void MoveGameObject(GameObject obj, Vector3 pos)
    {
        Debug.Log("pos: "+pos);
        obj.transform.position = pos;
    }

    void MoveRobot(Vector3 pos)
    {   
        // Transform baselink = robot.transform.Find("base_footprint");
        // Transform baselink = robot.transform.Find("base_link");
        base_footprint.GetComponent<ArticulationBody>().TeleportRoot(pos, Quaternion.identity);
    }

    State UpdatePath(BezierCurve curver, State state)
    {


        float minDist = 100000.0f;
        int closestIndex = 0;
        for (int i = 0; i < curver.numPoints-1; i++) {
            float dist = Vector2.Distance(new Vector2(state.carPosition.x, state.carPosition.z), new Vector2(curver.positions[i].x, curver.positions[i].z));
            if (dist < minDist) {
                minDist = dist;
                closestIndex = i;
            }
        }
        
        Vector3 p0 = new Vector3(curver.positions[closestIndex].x, curver.positions[closestIndex].y, curver.positions[closestIndex].z);
        
        Vector3 p1;
        if ((closestIndex+1) > (curver.numPoints-1)) { //|| (curver.positions[closestIndex+1].x <= 0.001 && curver.positions[closestIndex+1].z <= 0.001)
            p1 = new Vector3(curver.positions[curver.numPoints-1].x, curver.positions[curver.numPoints-1].y, curver.positions[curver.numPoints-1].z);
        } else {
            p1 = new Vector3(curver.positions[closestIndex+1].x, curver.positions[closestIndex+1].y, curver.positions[closestIndex+1].z);
        }
        
        Vector3 p2;
        int offset = 40;
        if ((closestIndex+offset) > (curver.numPoints-1) ) { //|| (curver.positions[closestIndex+offset].x <= 0.001 && curver.positions[closestIndex+offset].z <= 0.001)
            p2 = new Vector3(curver.positions[curver.numPoints-1].x, curver.positions[curver.numPoints-1].y, curver.positions[curver.numPoints-1].z);
        } else {
            p2 = new Vector3(curver.positions[closestIndex+offset].x, curver.positions[closestIndex+offset].y, curver.positions[closestIndex+offset].z);
            
        }
        
        state = robot.UpdatePath(state, p0, p1, p2);

        trailClosest.transform.position = p0;
        trailSecond.transform.position = p1;
        trailTarget.transform.position = p2;

        return state;
    }

    void SetObstables(BezierCurve curver) 
    {
        int curve_idx = random.Next(40, curver.numPoints-40);
        Vector3 pos1 = new Vector3(curver.positions[curve_idx].x + Random.Range(0.1f, 0.2f), curver.positions[curve_idx].y, curver.positions[curve_idx].z + Random.Range(0.1f, 0.2f));
        MoveGameObject(obstacle1, pos1);

        // curve_idx = random.Next(40, curver.numPoints-40);
        // pos1 = new Vector3(curver.positions[curve_idx].x - Random.Range(0.3f, 0.4f), curver.positions[curve_idx].y, curver.positions[curve_idx].z - Random.Range(0.3f, 0.4f));
        // MoveGameObject(obstacle2, pos1);

    }

    State updateState(Vector3 newTarget, BezierCurve curver) 
    {

        State state = robot.GetState(newTarget);
        System.Type type = state.GetType();
        // FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        

        state = UpdatePath(curver, state);

        

        return state;
    }

    private void SubscribeToTopic(string topic)
    {
        
        string subscribeMessage = "{\"op\":\"subscribe\",\"id\":\"1\",\"topic\":\"" + topic + "\",\"type\":\"std_msgs/msg/Float32MultiArray\"}";
        socket.Send(subscribeMessage);
    }



}

