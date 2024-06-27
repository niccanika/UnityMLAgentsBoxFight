using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public enum AgentSide
{
    Blue = 0,
    Red = 1
}

public class AgentBoxer : Agent
{
    // Detectable tags for the boxer agent
    public string opponentTag;
    public string wallTag;
    public string groundTag;

    private Animator Anim;

    public enum Action
    {
        MoveForward,
        MoveBackward,
        TurnLeft,
        TurnRight,
        JabLeft,
        JabRight
    }

    [HideInInspector]
    public AgentSide side;
    float m_PunchPower;
    public float initialMaxHealth;
    float maxHealth;
    [SerializeField] HealthBar healthBar;

    const float k_MoveSpeed = 1.3f;
    const float k_RotationSpeed = 100.0f;
    const float k_Power = 2000f;
    float m_Existential = 1f / 25000;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        Anim = GetComponent<Animator>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)AgentSide.Red)
        {
            side = AgentSide.Red;
            initialPos = new Vector3(transform.position.x, 0.5f, transform.position.z); // Adjust initial position for red boxer
            rotSign = 1f;
        }
        else
        {
            side = AgentSide.Blue;
            initialPos = new Vector3(transform.position.x, 0.5f, transform.position.z); // Adjust initial position for blue boxer
            rotSign = -1f;
        }

        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        maxHealth = initialMaxHealth;
        healthBar = GetComponentInChildren<HealthBar>();
        healthBar.UpdateHealthBar(maxHealth, initialMaxHealth);

    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var moveDir = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_PunchPower = 0f;

        var moveForward = act[0];
        var turn = act[1];
        var punch = act[2];

        switch (moveForward)
        {
            case 1:
                moveDir = transform.forward * k_MoveSpeed;
                Anim.SetBool("Walk", true);
                Anim.SetBool("left", false);
                Anim.SetBool("right", false);
                break;
            case 2:
                moveDir = transform.forward * -k_MoveSpeed;
                Anim.SetBool("Walk", true);
                Anim.SetBool("left", false);
                Anim.SetBool("right", false);
                break;
        }

        switch (turn)
        {
            case 1:
                rotateDir = transform.up * k_RotationSpeed * rotSign;
                Anim.SetBool("Walk", true);
                Anim.SetBool("left", false);
                Anim.SetBool("right", false);
                break;
            case 2:
                rotateDir = transform.up * -k_RotationSpeed * rotSign;
                Anim.SetBool("Walk", true);
                Anim.SetBool("left", false);
                Anim.SetBool("right", false);
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(moveDir, ForceMode.VelocityChange);

        // Handle punches based on the third action
        switch (punch)
        {
            case 1: // JabLeft
                m_PunchPower = 0.5f;
                Anim.SetBool("left", true);
                Anim.SetBool("Walk", false);
                Anim.SetBool("right", false);

                break;
            case 2: // JabRight
                m_PunchPower = 0.5f;
                Anim.SetBool("right", true);
                Anim.SetBool("Walk", false);
                Anim.SetBool("left", false);

                break;
            //case 5: // HookLeft
               // m_PunchPower = 1.0f;
                //break;
            //case 6: // HookRight
              //  m_PunchPower = 1.0f;
               // break;
        }
    }

    bool IsGrounded()
    {
        RaycastHit hit;
        float distance = 0.1f; // Adjust distance for ground detection

        // Raycast straight down from the agent's center
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distance))
        {
            // Check if the collided object has the groundTag
            return hit.collider.CompareTag(groundTag);
        }

        return false;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);

        // Check for grounding (optional, depending on your environment setup)
        //if (!IsGrounded())
        //{
            //AddReward(-0.1f); // Penalize for being in the air
        //}
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }
        //punch
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 2;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(opponentTag))
        {
            AgentBoxer opponentScript = collision.gameObject.GetComponent<AgentBoxer>(); 
            // Hit the opponent
            if (m_PunchPower > 0f) // Agent just threw a punch
            {
                //AddReward(m_PunchPower); // Reward based on punch power
                AddReward(0.2f);
                if (opponentScript.maxHealth <= 0f)
                {
                    AddReward(1f);
                    EndEpisode();
                }
            }
            if (opponentScript.m_PunchPower > 0f) // Got hit by opponent (punched by opponent or collided with opponent's body)
            {
                maxHealth = maxHealth - 1;
                healthBar.UpdateHealthBar(maxHealth, initialMaxHealth);
                //Debug.Log(maxHealth);
                AddReward(-0.2f); // Penalty for getting hit
            }
            if (maxHealth <= 0f)
            {
                AddReward(-1f);
                EndEpisode();
            }
        }
        else if (collision.gameObject.CompareTag(wallTag))
        {
            AddReward(-0.2f);
            agentRb.velocity = Vector3.zero;
            transform.position = initialPos;
            //EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent position and rotation
        transform.position = initialPos;
        transform.rotation = Quaternion.identity;
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        maxHealth = initialMaxHealth;
        healthBar.UpdateHealthBar(maxHealth, initialMaxHealth);
    }

}
