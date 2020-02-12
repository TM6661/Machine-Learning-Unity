using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PenguinAgent : Agent
{
    public GameObject heartPrefab;
    public GameObject regurgitatedFishPrefab;
    [SerializeField]
    private PenguinArea penguinArea;
    private Animator animator;
    private RayPerception3D rayPerception3D;
    private GameObject baby;
    private bool isFull; // true => penguin has full stomach 

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        //Convert action to axis values
        float forward = vectorAction[0];
        float leftOrRight = 0f;
        
        if(vectorAction[1] == 1f)
        {
            leftOrRight = -1f;
        }
        else if (vectorAction[1] == 2f)
        {
            leftOrRight = 1f;
        }

        //Set animator parameters
        animator.SetFloat("Vertical", forward);
        animator.SetFloat("Horizontal", leftOrRight);

        //Tiny negative reward every step
        AddReward(-1f / agentParameters.maxStep);
    }

    public override void AgentReset()
    {
        isFull = false;
        penguinArea.ResetArea();
    }

    public override void CollectObservations()
    {
        //Has the penguin eaten
        AddVectorObs(isFull);

        //Distance to the babby
        AddVectorObs(Vector3.Distance(baby.transform.position, transform.position));

        //Direction to the baby
        AddVectorObs((baby.transform.position - transform.position).normalized);

        //Direction penguin is facing
        AddVectorObs(transform.forward);

        //RayPerception (sight)
        //=====================
        //rayDistance : how far to raycast
        //rayAngles : angles to raycast (0 => right, 90 => forward, 180 => left)
        //detectableObjects : list of tags which correspond to object types agent can see
        //startOffset : starting height offset of ray from center of agent
        //endOffset : ending height offset of ray from center to agent
        float rayDistance = 20f;
        float[] rayAngles = { 30f, 60f, 90f, 120f, 150f};
        string[] detectableObjects = { "Baby", "Fish", "Wall" };

        AddVectorObs(rayPerception3D.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));
        
    }

    private void Start() 
    {
        // penguinArea = GetComponent<PenguinArea>();
        baby = penguinArea.penguinBaby;
        animator = GetComponent<Animator>();
        rayPerception3D = GetComponent<RayPerception3D>();
    }

    private void FixedUpdate() 
    {
        if (Vector3.Distance(transform.position, baby.transform.position) < penguinArea.feedRadius)
        {
            //Close to feed the baby
            RegurgitateFish();
        }
    }

    private void OnCollisionEnter(Collision col) {
        if (col.transform.CompareTag("Fish"))
        {
            EatFish(col.gameObject);
        }
        else if (col.transform.CompareTag("Baby"))
        {
            //Try to feef the baby
            RegurgitateFish();
        }
    }
    private void RegurgitateFish()
    {
        if (!isFull) return; //nothing to regurgitate
        isFull = false;

        //Spawn regurgitate fish
        GameObject regurgitatedFish = Instantiate<GameObject>(regurgitatedFishPrefab);
        regurgitatedFish.transform.parent = transform.parent;
        regurgitatedFish.transform.position = baby.transform.position;

        //Spawn heart
        GameObject heart = Instantiate<GameObject>(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = baby.transform.position + Vector3.up;    
        Destroy(heart, 4f);

        AddReward(1f);
    }

    private void EatFish(GameObject fishObject)
    {
        if (isFull) return; //cant eat another fish while is full
        isFull = true;

        penguinArea.RemoveSpecificFish(fishObject);
        AddReward(1f);
    }
}
