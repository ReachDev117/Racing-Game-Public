using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private WheelColliders colliders;
    [SerializeField] private WheelMeshes wheelMeshes;
    [SerializeField] private WheelParticles wheelParticles;

    [SerializeField] private float gasInput;
    [SerializeField] private float brakeInput;
    [SerializeField] private float steeringInput;

    [SerializeField] private float motorPower;
    private float startMotorPower;
    private float reducedMotorPower;
    [SerializeField] private float breakPower;
    private float slipAngle;
    private float speed;
    [SerializeField] AnimationCurve steeringCurve;
    [SerializeField] private float slipAllowance = 0.05f;

    private float lastFrameVelocity;
    public float Gforce;

    [SerializeField] private GameObject tireTrail;
    [SerializeField] private Material brakeMaterial;

    // Start is called before the first frame update
    void Start()
    {
        startMotorPower = motorPower;
        reducedMotorPower = (motorPower * 0.2f);
        rb = GetComponent<Rigidbody>();
        InitiateParticles();
    }

    void InitiateParticles()
    {
        if(tireTrail)
        {
            wheelParticles.FRWheelTrail = Instantiate(tireTrail, colliders.FRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.FRWheel.transform).GetComponent<TrailRenderer>();
            wheelParticles.BRWheelTrail = Instantiate(tireTrail, colliders.BRWheel.transform.position - Vector3.up * colliders.BRWheel.radius, Quaternion.identity, colliders.BRWheel.transform).GetComponent<TrailRenderer>();
            wheelParticles.FLWheelTrail = Instantiate(tireTrail, colliders.FLWheel.transform.position - Vector3.up * colliders.FLWheel.radius, Quaternion.identity, colliders.FLWheel.transform).GetComponent<TrailRenderer>();
            wheelParticles.BLWheelTrail = Instantiate(tireTrail, colliders.BLWheel.transform.position - Vector3.up * colliders.BLWheel.radius, Quaternion.identity, colliders.BLWheel.transform).GetComponent<TrailRenderer>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        speed = rb.velocity.magnitude;
        CheckInput();
        CheckParticles();
        ApplyMotor();
        ApplyBrakes();
        ApplySteering();
        ApplyWheelPositions();
    }

    void LateUpdate() 
    {
        ApplyParticlePositions();
    }

    void FixedUpdate()
    {
        float currentVelocity = GetComponent<Rigidbody>().velocity.magnitude;
        Gforce = ( currentVelocity - lastFrameVelocity ) / ( Time.deltaTime * Physics.gravity.magnitude );
        lastFrameVelocity = currentVelocity;
    }

    void CheckInput()
    {
        gasInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward, rb.velocity - transform.forward);

        float movingDirection = Vector3.Dot(transform.forward, rb.velocity);
        if(movingDirection < -0.5f && gasInput > 0f)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else if(movingDirection > 0.5f && gasInput < 0f)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else
        {
            brakeInput = 0f;
        }

        if(movingDirection < -0.5f && gasInput < 0f)
        {
            motorPower = reducedMotorPower;
        }
        else
        {
            motorPower = startMotorPower;
        }
    }

    void ApplyMotor()
    {
        colliders.BRWheel.motorTorque = motorPower * gasInput;
        colliders.BLWheel.motorTorque = motorPower * gasInput;
    }

    void ApplyBrakes()
    {
        colliders.FRWheel.brakeTorque = brakeInput * breakPower * 0.7f;
        colliders.FLWheel.brakeTorque = brakeInput * breakPower * 0.7f;

        colliders.BRWheel.brakeTorque = brakeInput * breakPower * 0.3f;
        colliders.BLWheel.brakeTorque = brakeInput * breakPower * 0.3f;

        if(brakeMaterial)
        {
            if(brakeInput > 0f)
            {
                brakeMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                brakeMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    void ApplySteering()
    {
        float steeringAngle = steeringInput * steeringCurve.Evaluate(speed);
        if(slipAngle < 120f)
        {
            steeringAngle += Vector3.SignedAngle(transform.forward, rb.velocity + transform.forward, Vector3.up);
        }
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
        colliders.FRWheel.steerAngle = steeringAngle;
        colliders.FLWheel.steerAngle = steeringAngle;
    }

    void ApplyWheelPositions()
    {
        UpdateWheel(colliders.FRWheel, wheelMeshes.FRWheel);
        UpdateWheel(colliders.BRWheel, wheelMeshes.BRWheel);
        UpdateWheel(colliders.FLWheel, wheelMeshes.FLWheel);
        UpdateWheel(colliders.BLWheel, wheelMeshes.BLWheel);
    }

    void ApplyParticlePositions()
    {
        wheelParticles.FRWheelTrail.transform.position = colliders.FRWheel.transform.position;
        wheelParticles.BRWheelTrail.transform.position = colliders.BRWheel.transform.position;
        wheelParticles.FLWheelTrail.transform.position = colliders.FLWheel.transform.position;
        wheelParticles.BLWheelTrail.transform.position = colliders.BLWheel.transform.position;
    }

    void CheckParticles()
    {
        WheelHit[] wheelHits = new WheelHit[4];

        colliders.FRWheel.GetGroundHit(out wheelHits[0]);
        colliders.FLWheel.GetGroundHit(out wheelHits[1]);
        colliders.BRWheel.GetGroundHit(out wheelHits[2]);
        colliders.BLWheel.GetGroundHit(out wheelHits[3]);

        if((Mathf.Abs(wheelHits[0].sidewaysSlip) + Mathf.Abs(wheelHits[0].forwardSlip) > slipAllowance))
        {
            wheelParticles.FRWheelTrail.emitting = true;
        }
        else
        {
            wheelParticles.FRWheelTrail.emitting = false;
        }

        if((Mathf.Abs(wheelHits[1].sidewaysSlip) + Mathf.Abs(wheelHits[1].forwardSlip) > slipAllowance))
        {
            wheelParticles.FLWheelTrail.emitting = true;
        }
        else
        {
            wheelParticles.FLWheelTrail.emitting = false;
        }

        if((Mathf.Abs(wheelHits[2].sidewaysSlip) + Mathf.Abs(wheelHits[2].forwardSlip) > slipAllowance))
        {
            wheelParticles.BRWheelTrail.emitting = true;
        }
        else
        {
            wheelParticles.BRWheelTrail.emitting = false;
        }

        if((Mathf.Abs(wheelHits[3].sidewaysSlip) + Mathf.Abs(wheelHits[3].forwardSlip) > slipAllowance))
        {
            wheelParticles.BLWheelTrail.emitting = true;
        }
        else
        {
            wheelParticles.BLWheelTrail.emitting = false;
        }
    }

    void UpdateWheel(WheelCollider coll, MeshRenderer wheelMesh)
    {
        Quaternion quat;
        Vector3 position;
        coll.GetWorldPose(out position, out quat);
        wheelMesh.transform.position = position;
        wheelMesh.transform.rotation = quat;
    }
}

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FRWheel;
    public WheelCollider BRWheel;
    public WheelCollider FLWheel;
    public WheelCollider BLWheel;
}

[System.Serializable]
public class WheelMeshes
{
    public MeshRenderer FRWheel;
    public MeshRenderer BRWheel;
    public MeshRenderer FLWheel;
    public MeshRenderer BLWheel;
}

[System.Serializable]
public class WheelParticles
{
    public TrailRenderer FRWheelTrail;
    public TrailRenderer BRWheelTrail;
    public TrailRenderer FLWheelTrail;
    public TrailRenderer BLWheelTrail;
}