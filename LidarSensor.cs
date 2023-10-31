using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class LidarSensor : MonoBehaviour
{   
    

    public float RangeMetersMin = 0.10f; // 0
    public float RangeMetersMax = 100; // 1000
    float ScanAngleStartDegrees = 0; //-45
    float ScanAngleEndDegrees = -359; //45
    public int NumMeasurementsPerScan = 180; //10

    List<float> ranges = new List<float>();
    float m_CurrentScanAngleStart;
    float m_CurrentScanAngleEnd;
    int m_NumMeasurementsTaken;
    bool isScanning = false;
    float minRange;

    List<Vector3> directionVectors = new List<Vector3>();
    Vector3 minDirectionVector;

    
    // Start is called before the first frame update
    void Start()
    {
        m_CurrentScanAngleStart = ScanAngleStartDegrees;
        m_CurrentScanAngleEnd = ScanAngleEndDegrees;
    }

    void BeginScan()
    {
        isScanning = true;
        m_NumMeasurementsTaken = 0;
    }

    public void EndScan()
    {
        if (ranges.Count == 0)
        {
            Debug.LogWarning($"Took {m_NumMeasurementsTaken} measurements but found no valid ranges");
        }
        else if (ranges.Count != m_NumMeasurementsTaken || ranges.Count != NumMeasurementsPerScan)
        {
            Debug.LogWarning($"Expected {NumMeasurementsPerScan} measurements. Actually took {m_NumMeasurementsTaken}" +
                             $"and recorded {ranges.Count} ranges.");
        }

        m_NumMeasurementsTaken = 0;
        ranges.Clear();
        directionVectors.Clear();
        isScanning = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (!isScanning)
        {

            BeginScan();
        }


        var measurementsSoFar = NumMeasurementsPerScan;

        var yawBaseDegrees = transform.rotation.eulerAngles.y;
        while (m_NumMeasurementsTaken < measurementsSoFar)
        {
            var t = m_NumMeasurementsTaken / (float)NumMeasurementsPerScan;
            var yawSensorDegrees = Mathf.Lerp(m_CurrentScanAngleStart, m_CurrentScanAngleEnd, t);
            var yawDegrees = yawBaseDegrees + yawSensorDegrees;
            var directionVector = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
            var measurementStart = RangeMetersMin * directionVector + transform.position;
            var measurementRay = new Ray(measurementStart, directionVector);
            var foundValidMeasurement = Physics.Raycast(measurementRay, out var hit, RangeMetersMax);
            // Only record measurement if it's within the sensor's operating range
            if (foundValidMeasurement)
            {   
                // Debug.Log("find it! " + hit.distance);
                ranges.Add(hit.distance);
                
            }
            else
            {
                // ranges.Add(float.MaxValue);
                // Debug.Log("lidar failed to detect!");
                ranges.Add(100.0f);
            }

            // Even if Raycast didn't find a valid hit, we still count it as a measurement
            ++m_NumMeasurementsTaken;

            directionVectors.Add(directionVector);
            minRange = ranges.Min();
            var idx = ranges.IndexOf(minRange);
            minDirectionVector = directionVectors[idx];
        }

        
        
        
        if (m_NumMeasurementsTaken >= NumMeasurementsPerScan)
        {
            if (m_NumMeasurementsTaken > NumMeasurementsPerScan)
            {
                Debug.LogError($"LaserScan has {m_NumMeasurementsTaken} measurements but we expected {NumMeasurementsPerScan}");
            }
            EndScan();
        }
    }

    public float GetMinRange() 
    {   
  
        return minRange;

    }

    public Vector3 GetMinRangeDirection() 
    {   

        return minDirectionVector;
    }

    public int GetRangeSize() 
    {  
        return m_NumMeasurementsTaken;

    }


}
