using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;


public class State
{
    // public Dictionary<string, Vector3> targetPosition { get; set; }
    // public Dictionary<string, Vector3> pathPosition { get; set; }
    // public Dictionary<string, Vector3> carPosition { get; set; }
    // public Dictionary<string, Vector3> carVelocity { get; set; }
    // public Dictionary<string, Vector3> carAugularVelocity { get; set; }
    // public Dictionary<string, Quaternion> carQuaternion { get; set; }
    // public Dictionary<string, Vector3> wheelAngularVelocity { get; set; }
    // public Dictionary<string, Quaternion> wheelQuaternion { get; set; }
    // public Vector3 targetPosition { get; set; }
    // public Vector3 pathPositionClosest { get; set; }
    public Vector3 carPosition { get; set; }
    public float objectUpVector { get; set; }

    public Vector3 ROS2TargetPosition { get; set; } //3 4 5
    public Vector3 ROS2PathPositionClosest { get; set; }
    public Vector3 ROS2PathPositionSecondClosest { get; set; }
    public Vector3 ROS2PathPositionFarthest { get; set; }
    public Vector3 ROS2CarPosition { get; set; }
    public Vector3 ROS2CarVelocity { get; set; }
    public Vector3 ROS2CarAugularVelocity { get; set; }
    public Quaternion ROS2CarQuaternion { get; set; }
    public Vector3 ROS2WheelAngularVelocityLeftBack { get; set; }
    public Vector3 ROS2WheelAngularVelocityLeftFront { get; set; }
    public Vector3 ROS2WheelAngularVelocityRightBack { get; set; }
    public Vector3 ROS2WheelAngularVelocityRightFront { get; set; }
    public Quaternion ROS2WheelQuaternionLeftBack { get; set; }
    public Quaternion ROS2WheelQuaternionLeftFront { get; set; }
    public Quaternion ROS2WheelQuaternionRightBack { get; set; }
    public Quaternion ROS2WheelQuaternionRightFront { get; set; }

    public float[] ROS2Range { get; set; }
    public Vector3[] ROS2RangePosition { get; set; }
    public bool isFirst { get; set; }

    void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
