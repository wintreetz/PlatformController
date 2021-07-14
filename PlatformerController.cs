using System;
using System.Collections;
using System.Collections.Generic;
using Bolt;
using PlayFab;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Event = UnityEngine.Event;

public class PlatformerController : Bolt.EntityEventListener<IPlatformerPlayer>
{
    //how fast player goes
    private int speed = 5;

    private float turnSpeed = 95.0f;
    
    //user joystick
    public Joystick joystick;
    
    //laser button
    public LaserButton laserButton;

    public static Boolean laserClicked = false;

    //velocity for jumping
    private float velocity = 0;

    private Text coordinateText;

    private LaserBeam laserBeam;

    private IEnumerable interalDataCoroutine;

    //private float circleCastRadius = 2.0f;

    //This is for when we may need to use temporal anti-aliasing.
    //[SerializeField]
    //Transform renderTransform;


    //mask for player collisions with objects
    //[SerializeField]
    //private LayerMask mask;

    //whether or not player is grounded, need to be grounded to jump
    //private bool grounded = false;



    public override void Attached()
    {
        joystick = GameObject.Find("Fixed Joystick").GetComponent<Joystick>();
        laserButton = GameObject.Find("LaserButton").GetComponent<LaserButton>();
        laserButton.platformerController = gameObject;

        if(this.entity.HasControl)
        {
            coordinateText = GameObject.Find("Text").GetComponent<Text>();
            //GameController is marked as BoltSingleton and can be found in resources
            CameraController.instance.player = gameObject;
            
            //This is where we set the sorting layer to ensure that the player is at the foreground
            gameObject.GetComponent<Renderer>().sortingLayerID = SortingLayer.NameToID("Player");
        }
        state.SetTransforms(state.transform, transform);

        laserBeam = GetComponentInChildren<LaserBeam>();
        
        treetz.UI.PlayFabManager.StartGetReadOnlySpawnLocation(gameObject);

    }
    
    void OnApplicationQuit()
    {
        var position = transform.position;
        treetz.UI.PlayFabManager.StartSetReadOnlySpawnLocation(position.x, position.y);
    }
    

    public override void SimulateController()
    {
        IplatformerPlayerCommandInput input = platformerPlayerCommand.Create();
        
        input.left = joystick.Horizontal;
        input.right = joystick.Horizontal;
        input.up = joystick.Vertical;
        input.laser = laserClicked;

        entity.QueueInput(input);
    }

    private void Update()
    {
        if (BoltNetwork.IsClient)
        {
            if (!(coordinateText is null))
                coordinateText.text =
                    "(" + transform.position.x.ToString() + "," + transform.position.y.ToString() + ")";
        }
    }

    //helper method to check if player is colliding with something
    /*
    public bool RayCheck(Vector3 rayOrigin, float rad, Vector3 dir)
    {
        Debug.DrawRay(rayOrigin, dir, Color.red);
        return Physics2D.CircleCast(rayOrigin, rad, dir, dir.magnitude, mask);

    }
    */

    public override void ExecuteCommand(Command command, bool resetState)
    {
        platformerPlayerCommand cmd = (platformerPlayerCommand)command;

        if (resetState)
        {
            //owner has sent a correction to the controller
            transform.position = cmd.Result.position;
            transform.rotation = cmd.Result.rotation;
            velocity = cmd.Result.velocity;
            //grounded = cmd.Result.grounded;
        }
        else
        {

            if(velocity > 0)
            {
                velocity -= Time.fixedDeltaTime * 2 * speed;
            }

            /*
            bool collisionLeft;
            bool collisionRight;
            bool collisionAbove;
            */

            //check above collision
            /*collisionAbove = RayCheck(transform.position, circleCastRadius, transform.up);

            if (collisionAbove == true)
            {
                velocity = -1;
            }


            //check right side collision

            collisionRight = RayCheck(transform.position, circleCastRadius, transform.right);



            //check left side collision

            collisionLeft = RayCheck(transform.position, circleCastRadius, -transform.right);
            */
            

            if (cmd.Input.left < 0.0f) //if you add back collisions in outer world you need to check here for a collision
            {
                transform.Rotate(Vector3.forward, turnSpeed * Time.fixedDeltaTime);
            }
            else if (cmd.Input.right > 0.0f) //if you add back collisions in outer world you need to check here for a collision
            {
                transform.Rotate(Vector3.forward, -turnSpeed * Time.fixedDeltaTime);
            }
            
            if (cmd.Input.up > 0)
            {
                velocity = 1.3f * speed;
            }

            switch (cmd.Input.laser)
            {
                case true:
                    laserBeam.FireLaser(this.entity.Controller);
                    break;
                case false:
                    laserBeam.StopLaser();
                    break;
            }

            //check collision below and set grounding.
            //bool groundHit = RayCheck(transform.position, circleCastRadius, -transform.up);

            /*
            if (!groundHit)
            {
                grounded = false;
            }
            else
            */
            
            if (velocity < 0)
            {
                velocity = 0;
                //grounded = true;
            }

            transform.Translate(Vector2.up * Time.deltaTime * velocity);

            cmd.Result.position = transform.position;
            cmd.Result.rotation = transform.rotation;
            //cmd.Result.grounded = grounded;
            cmd.Result.velocity = velocity;
        }
        //when jump pressed, velocity goes to 1 and gradually goes down until terminal velocity

    }
}
