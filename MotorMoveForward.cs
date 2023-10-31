using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorMoveForward : MonoBehaviour
{   
    ArticulationBody bd;
    const float RAD2DEG = 57.295779513f;
    // float smooth = 1.0f;
    // float speed = 1000f;
    float stiffness = 0f;
    float damping = 1000f;
    float forceLimit = 100f;
    float voltage=0;

    void Awake()
    {
        bd = GetComponent<ArticulationBody>();
        

    }

    public void SetVoltage(float newvoltage)
    {
        voltage = newvoltage;
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        ArticulationDrive currentDrive = bd.xDrive;
        currentDrive.stiffness = stiffness;
        currentDrive.damping = damping;
        currentDrive.forceLimit = forceLimit;
        bd.xDrive = currentDrive;
    }

    // Update is called once per frame
    void Update()
    {
        setSpeed(voltage);
    }

    void setSpeed(float voltage_wheel){
        var drive = bd.xDrive;
        // Debug.Log("bd: "+bd+" "+"voltage: "+voltage_wheel);
        drive.targetVelocity = voltage_wheel;
        
        bd.xDrive = drive;
    }
}
