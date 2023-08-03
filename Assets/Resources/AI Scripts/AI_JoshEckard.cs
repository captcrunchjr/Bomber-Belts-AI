using UnityEngine;
using System.Collections;

public class AI_JoshEckard : MonoBehaviour {

    public CharacterScript mainScript;

    public float[] bombSpeeds;
    public float[] buttonCooldowns;
    public float playerSpeed;
    public int[] beltDirections;
    public float[] buttonLocations;

    //******************************
    //public GameScript gameScript;

    int currPlayerHealth;
    int currOppHealth;
    string state;
    float startTime;
    float[] bombAccelerations;
    float[,] bombFirstVelocity;
    float[,] bombNextVelocity;
    float[] bombLaunchTime;
    float[] bombDistances;
    float[] timeToDetonation;
    float[] timeToButton;
    float beltLength;

	// Use this for initialization
	void Start () {
        mainScript = GetComponent<CharacterScript>();

        if (mainScript == null)
        {
            print("No CharacterScript found on " + gameObject.name);
            this.enabled = false;
        }

        buttonLocations = mainScript.getButtonLocations();

        playerSpeed = mainScript.getPlayerSpeed();

        //********************************
        //gameScript = GetComponent<GameScript>();
        //playerBlue = gameScript.bluePlayer;

        MyInitialize(buttonLocations.Length);
	}

	// Update is called once per frame
	void Update () {

        buttonCooldowns = mainScript.getButtonCooldowns();
        beltDirections = mainScript.getBeltDirections();
        //mainScript.push();

        
        //Your AI code goes here
        bombSpeeds = mainScript.getBombSpeeds(); //update speeds
        bombDistances = mainScript.getBombDistances(); //update bomb distances
        //state = setState();

        //maintain fairly current Acceleration values
        CalculateAcceleration();
        TimeToDetonation();
        TimeToButton();

	}

    void MyInitialize(int length){
        startTime = Time.time;
        state = "spike";

        bombAccelerations = new float[length];
        bombFirstVelocity = new float[length,2];
        bombNextVelocity = new float[length,2];
        bombLaunchTime = new float[length];
        bombDistances = mainScript.getBombDistances();
        beltLength = bombDistances[0]*2;
        timeToDetonation = new float[length];
        timeToButton = new float[length];

        for(int i = 0; i < bombAccelerations.Length; i++){
            bombAccelerations[i] = 0.0f;
            bombFirstVelocity[i,0] = 0.0f;
            bombFirstVelocity[i,1] = 0.0f;
            bombNextVelocity[i,0] = 0.0f;
            bombNextVelocity[i,1] = 0.0f;
            bombLaunchTime[i] = 0.0f;
            timeToDetonation[i] = 0.0f;
        }
    }

    void EvaluateVelocities(){
        float currTime;
        for(int i = 0; i < bombFirstVelocity.GetLength(0); i++){
            if(bombSpeeds[i] > 0.0f){
                //if no current first velocity detected
                if(bombFirstVelocity[i,0] == 0.0f){
                    bombFirstVelocity[i,0] = bombSpeeds[i];
                    currTime = Time.time;
                    bombFirstVelocity[i,1] = currTime;
                    SetBombLaunchTime(i, currTime);
                }
                //if first velocity has a value and next velocity doesnt
                else if(bombNextVelocity[i,0] == 0.0f){
                    /*if(Time.time == bombFirstVelocity[i,1]){
                        print("Continuing on BombNextVelo check");
                    }
                    else{*/
                        bombNextVelocity[i,0] = bombSpeeds[i];
                        currTime = Time.time;
                        bombNextVelocity[i,1] = currTime;
                    //}
                }
                //if it has both next and first, move next to first, get new next
                else{
                    /*if(Time.time == bombNextVelocity[i,1]){
                        print("Continuing on complicated else");
                    }
                    else{*/
                        bombFirstVelocity[i,0] = bombNextVelocity[i,0];
                        bombFirstVelocity[i,1]  = bombNextVelocity[i,1];

                        bombNextVelocity[i,0] = bombSpeeds[i];
                        currTime = Time.time;
                        bombNextVelocity[i,1] = currTime;
                    //}
                }
                //SetBombLaunchTime(i, currTime);
            }
            else{
                bombFirstVelocity[i,0] = 0.0f;
                bombFirstVelocity[i,1] = 0.0f;
                bombNextVelocity[i,0] = 0.0f;
                bombNextVelocity[i,1] = 0.0f;
                SetBombLaunchTime(i, 0.0f);
            }
        }
    }

    //Get time to button on current trajectory based on speed and acceleration
    void TimeToDetonation(){
        //Quadratic = (-b +/- sqrt(b^2 - 4ac)) / 2a
        //a = currAcceleration, b = currVelocity, c = (current Distance from button * -1)
        for(int i = 0; i < timeToDetonation.Length; i++){
            if(beltDirections[i] != 0){
                timeToDetonation[i] = QuadraticCalc(beltDirections[i], bombDistances[i], bombSpeeds[i], bombAccelerations[i]);
            }
            else{
                timeToDetonation[i] = 0.0f;
            }
        }
    }

    float QuadraticCalc(int direction, float position, float currVelocity, float currAcceleration){
        float endPoint;
        float time;
        if(direction == 1){
            endPoint = beltLength*2;
        }
        else{
            endPoint = 0;
        }
        position -= endPoint;
        return (((currVelocity * -1) + Mathf.Sqrt((Mathf.Pow(currVelocity, 2.0f) + 4*currAcceleration*position))) / (2*currAcceleration));
    }
    
    //Get time for player to reach target button from current position
    void TimeToButton(){
        for(int i = 0; i < timeToButton.Length; i++){
            timeToButton[i] = (buttonLocations[i] - mainScript.getCharacterLocation()) / playerSpeed;
            print("Time to button " + i + ": " + timeToButton[i]);
        }
    }

    //Calculate the acceleration periodically, necessary to plan spikes.
    void CalculateAcceleration(){
        EvaluateVelocities();

        for(int i = 0; i < bombAccelerations.Length; i++){
            if(bombNextVelocity[i,0] != 0.0f){
                bombAccelerations[i] = (bombNextVelocity[i,0]/bombFirstVelocity[i,0]);
            }
            else{
                bombAccelerations[i] = 0.0f;
            }
        }
    }

    //Evaluate each bombs score based on state 
    void ScoreTargets(){

    }

    //Evaluate and set state
    string setState(){
        string state = "spike";
        //if health is > 3 OR health is tied with Opponent, state is spike
        /*if(currPlayerHealth > 3 || currPlayerHealth == currOppHealth){
            state = "spike";
        }
        else{
            state = "defend";
        }*/
        return state;
    }

    void SetBombLaunchTime(int index, float time){
        bombLaunchTime[index] = time;
    }

}
