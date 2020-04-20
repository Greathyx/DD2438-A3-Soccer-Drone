using UnityEngine;
using System;
using System.Linq;
using Panda;


[RequireComponent(typeof(DroneController))]
public class DroneAISoccer_blue : MonoBehaviour
{
    private DroneController m_Drone; // the drone controller we want to use

    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;

    public GameObject[] friends;
    public string friend_tag;
    public GameObject[] enemies;
    public string enemy_tag;

    public GameObject own_goal;
    public GameObject other_goal;
    public GameObject ball;

    PandaBehaviour pandaBT;
    private GameObject goalie;
    private float k_v = 5f;
    private float k_p = 5f;
    private Rigidbody rb;

    private void Start()
    {
        pandaBT = GetComponent<PandaBehaviour>();
        rb = GetComponent<Rigidbody>();

        // get the drone controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();

        friend_tag = gameObject.tag;
        if (friend_tag == "Blue")
            enemy_tag = "Red";
        else
            enemy_tag = "Blue";

        friends = GameObject.FindGameObjectsWithTag(friend_tag);
        enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

        ball = GameObject.FindGameObjectWithTag("Ball");

        // Set the friend closet to the own goal to be the goalie.
        float disToOwnGoal_sqr = float.PositiveInfinity;
        foreach (var friend in friends)
        {
            float dis_sqr = (friend.transform.position - own_goal.transform.position).sqrMagnitude;
            if (dis_sqr < disToOwnGoal_sqr)
            {
                disToOwnGoal_sqr = dis_sqr;
                goalie = friend;
            }
        }
    }

    private void Update()
    {
        pandaBT.Reset();
        pandaBT.Tick();
    }


    void PDGotoState(Vector3 desiredPosition, Vector3 desiredVelocity)
    {
        Vector3 positionError = desiredPosition - transform.position;
        Vector3 velocityError = desiredVelocity - rb.velocity;
        m_Drone.Move_vect(positionError * k_p + velocityError * k_v);
    }

    void GotoPosition(Vector3 desiredPosition)
    {
        m_Drone.Move_vect(desiredPosition - transform.position);
    }

    /**
     * Defend
     */
    [Task]
    bool IsGoalie()
    {
        return goalie.name == gameObject.name;
    }

    [Task]
    void Defend()
    {
        // 如果球离球门距离 < 15f 或 球离守门员距离 < 5f
        float ballToOwnGoal = (own_goal.transform.position - ball.transform.position).sqrMagnitude;
        float ballToGoalie = (transform.position - ball.transform.position).sqrMagnitude;
        if (ballToOwnGoal < 15f * 15f || ballToGoalie < 5f * 5f)
        {
            // 将球踢走（方向为对方球门）
            //this.GotoPosition(ball.transform.position);
            Vector3 ballVelocity = ball.GetComponent<Rigidbody>().velocity;
            Vector3 desiredDirection = (other_goal.transform.position - ball.transform.position).normalized;
            Vector3 desiredPosition = ball.transform.position - desiredDirection * 2.5f + 0.0f * ballVelocity.normalized;

            PDGotoState(desiredPosition, ballVelocity);
        }
        // 否则守门员呆在距离自家球门10f处 并 与球保持同一直线
        else
        {
            Vector3 defendDirection = (ball.transform.position - own_goal.transform.position).normalized;
            Vector3 desiredPosition = defendDirection * 10f + own_goal.transform.position;
            this.GotoPosition(desiredPosition);
        }
    }

    /**
     * Chase
     */
    [Task]
    bool IsChaser()
    {
        // not goalie
        if (gameObject.name != goalie.name)
        {
            // 找到距离球最近的一个队友并计算他与球之间的距离
            float closestDisToBall = float.PositiveInfinity;
            foreach (var friend in friends)
            {
                float dis = (friend.transform.position - ball.transform.position).magnitude;
                if (dis < closestDisToBall)
                {
                    closestDisToBall = dis;
                }
            }

            float disToBall = (transform.position - ball.transform.position).magnitude;
            if (disToBall < closestDisToBall + 30f)
                return true;
        }
        return false;
    }

    [Task]
    bool HasControlledBall()
    {
        float disToBall_sqr = (transform.position - ball.transform.position).sqrMagnitude;
        return disToBall_sqr < 5f * 5f;
    }

    [Task]
    void Dribble()
    {
        Vector3 ballVelocity = ball.GetComponent<Rigidbody>().velocity;
        Vector3 relativeBallPosition = ball.transform.position - transform.position;
        Vector3 desiredDirection = (other_goal.transform.position - ball.transform.position).normalized;

        Vector3 desiredPosition = ball.transform.position - desiredDirection * 2.5f + 0.0f * ballVelocity.normalized;

        if (relativeBallPosition.magnitude > 5f)
            PDGotoState(desiredPosition, ballVelocity);
        else
            GotoPosition(desiredPosition);
    }

    [Task]
    void ChaseBall()
    {
        //Vector3 ballPos = ball.transform.position;
        //this.GotoPosition(ballPos);
        Vector3 ballVelocity = ball.GetComponent<Rigidbody>().velocity;
        Vector3 desiredDirection = (other_goal.transform.position - ball.transform.position).normalized;
        Vector3 desiredPosition = ball.transform.position - desiredDirection * 2.5f + 0.0f * ballVelocity.normalized;

        PDGotoState(desiredPosition, ballVelocity);
    }

    /**
     * Chase Enemy
     */
    [Task]
    void ChaseEnemy()
    {
        GameObject closestEnemy = enemies[0];
        float closestDisToEnemy_square = float.PositiveInfinity;
        foreach (var enemy in enemies)
        {
            float dis = (enemy.transform.position - transform.position).sqrMagnitude;
            if (dis < closestDisToEnemy_square)
            {
                closestDisToEnemy_square = dis;
                closestEnemy = enemy;
            }
        }
        this.GotoPosition(closestEnemy.transform.position);
    }
}