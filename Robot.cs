using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using System.Linq;


public class Robot : MonoBehaviour
{
    public struct Action
    {
        public List<float> voltage;
    }

    public MotionSensor baseLinkM;
    public List<MotionSensor> wheelM;

    // List<Motor> motorList;
    List<MotorMoveForward> motorListMF;

    public TrailRenderer trailRenderer;

    //test
    public Vector3 rot;

    public State ROS2State;

    Vector3 targetPosition;

    const float DEG2RAD = 0.01745329251f;

    public LidarSensor lidar;

    void Awake()
    {
        baseLinkM = Util.GetOrAddComponent<MotionSensor>(transform, "base_link");
        wheelM = new List<MotionSensor>() {
            Util.GetOrAddComponent<MotionSensor>(transform, "left_back_forward_wheel"),
            Util.GetOrAddComponent<MotionSensor>(transform, "right_back_forward_wheel"),
        };

        motorListMF = new List<MotorMoveForward>() {
            Util.GetOrAddComponent<MotorMoveForward>(transform, "left_back_forward_wheel"),
            Util.GetOrAddComponent<MotorMoveForward>(transform, "left_front_forward_wheel"),
            Util.GetOrAddComponent<MotorMoveForward>(transform, "right_back_forward_wheel"),
            Util.GetOrAddComponent<MotorMoveForward>(transform, "right_front_forward_wheel"),
        };

        trailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    public State GetState(Vector3 newTarget, bool isFirst)
    {
        Vector3 carPos = baseLinkM.x;
        Vector3 carVel = baseLinkM.v;
        Vector3 carAngV = baseLinkM.AngularV;
        Quaternion carQ = baseLinkM.q;
        Vector3 angVLB = wheelM[0].AngularV;
        Vector3 angVRB = wheelM[1].AngularV;

        List<float> range = lidar.GetRange();

        var rangeDirection = lidar.GetRangeDirection(); //list
        for (int i = 0; i < rangeDirection.Count; i++)
        {
            rangeDirection[i] = ToRosVec(rangeDirection[i]);
        }

        State ROS2State = new State()
        {
            carPosition = carPos,

            ROS2TargetPosition = ToRosVec(newTarget),
            ROS2CarPosition = ToRosVec(carPos),
            ROS2CarVelocity = ToRosVec(carVel),
            ROS2CarAugularVelocity = ToRosVec(carAngV),
            ROS2CarQuaternion = ToRosQuaternion(carQ),
            ROS2WheelAngularVelocityLeftBack = ToRosVec(angVLB),
            ROS2WheelAngularVelocityRightBack = ToRosVec(angVRB),

            ROS2Range = range.ToArray(),
            isFirst = isFirst,
            ROS2RangePosition = rangeDirection.ToArray(),
        };
        return ROS2State;
    }

    public void DoAction(Action action)
    {
        motorListMF[0].SetVoltage((float)action.voltage[0]);
        motorListMF[1].SetVoltage((float)action.voltage[0]);
        motorListMF[2].SetVoltage((float)action.voltage[1]);
        motorListMF[3].SetVoltage((float)action.voltage[1]);
    }

    public float getTargetAngle(Vector2 pos, Vector2 targetPos)
    {
        Vector2 v = targetPos - pos;
        Vector2 up = new Vector2(0, 1);
        float rad = getAngle(v, up);
        if (v[0] < 0)
            rad = Mathf.PI * 2 - rad;
        return rad;
    }

    public float getAngle(Vector2 v1, Vector2 v2)
    {
        v1.Normalize();
        v2.Normalize();
        return Mathf.Acos(Vector2.Dot(v1, v2));
    }

    static (double, double, double) EulerFromQuaternion(Quaternion orientation)
    {
        /*
        Convert a quaternion into euler angles (roll, pitch, yaw)
        roll is rotation around x in radians (counterclockwise)
        pitch is rotation around y in radians (counterclockwise)
        yaw is rotation around z in radians (counterclockwise)
        */

        double x = orientation.x;
        double y = orientation.y;
        double z = orientation.z;
        double w = orientation.w;

        double t0 = +2.0 * (w * x + y * z);
        double t1 = +1.0 - 2.0 * (x * x + y * y);
        double roll_x = Math.Atan2(t0, t1);

        double t2 = +2.0 * (w * y - z * x);
        t2 = +1.0 > t2 ? +1.0 : t2;
        t2 = -1.0 < t2 ? -1.0 : t2;
        double pitch_y = Math.Asin(t2);

        double t3 = +2.0 * (w * z + x * y);
        double t4 = +1.0 - 2.0 * (y * y + z * z);
        double yaw_z = Math.Atan2(t3, t4);

        return (roll_x, pitch_y, yaw_z);
    }

    Vector3 ToRosVec(Vector3 position)
    {
        PointMsg ROS2Position = position.To<FLU>();
        position = new Vector3((float)ROS2Position.x, (float)ROS2Position.y, (float)ROS2Position.z);

        return position;
    }

    Quaternion ToRosQuaternion(Quaternion quaternion)
    {
        QuaternionMsg ROS2Quaternion = quaternion.To<FLU>();
        quaternion = new Quaternion((float)ROS2Quaternion.x, (float)ROS2Quaternion.y, (float)ROS2Quaternion.z, (float)ROS2Quaternion.w);


        quaternion.To<FLU>();
        return quaternion;
    }
}
