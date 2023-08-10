using UnityEngine;
using System.Collections;

public class AI_JoshEckard : MonoBehaviour {

    public CharacterScript mainScript;

    public float[] bombSpeeds;
    public float[] buttonCooldowns;
    public float playerSpeed;
    public int[] beltDirections;
    public float[] buttonLocations;

    //vvvvvvvvvvvvvvvvMy Stuffvvvvvvvvvvvvvvvvv
    //text element needed to track health values for state change
    HealthBarScript blueHealthIndicator, redHealthIndicator;
    bool playerId;
    int currPlayerHealth;
    int currOppHealth;
    float oppPos;
    float playerPos;
    [SerializeField]
    string state;
    public float[] bombAccelerations;
    float[,] bombFirstVelocity;
    float[,] bombNextVelocity;
    float[] bombLaunchTime;
    float[] bombDistances;
    [SerializeField]
    float[] timeToDetonation;
    float[] timeToButton;
    float beltLength;
    float[] bombScores;
    bool launchedBomb3;
    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

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

        //vvvvvvvvvvvvvvvvMy Stuffvvvvvvvvvvvvvvvvv
        blueHealthIndicator = GameObject.Find("Indicator_Blue(Clone)").GetComponent<HealthBarScript>();
        redHealthIndicator = GameObject.Find("Indicator_Red(Clone)").GetComponent<HealthBarScript>();
        MyInitialize(buttonLocations.Length);
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
	}

	// Update is called once per frame
	void Update () {
        buttonCooldowns = mainScript.getButtonCooldowns();
        beltDirections = mainScript.getBeltDirections();

        //Your AI code goes here
        UpdateHealth();
        state = setState(); //set state, based on health values
        oppPos = mainScript.getOpponentLocation(); //opponent location at top of update
        playerPos = mainScript.getCharacterLocation(); //player position at top of update
        bombSpeeds = mainScript.getBombSpeeds(); //update bomb speeds
        bombDistances = mainScript.getBombDistances(); //update bomb distances

        //move to bomb 4 triggering them as it moves, then states take effect
        if(!launchedBomb3){
            TriggerBomb3OnStart();
        }
        else{
            CalculateAcceleration(); //maintain fairly current Acceleration values
            TimeToDetonation(); //calculate time until detonation in its current direction: attempted to make it more precise by including the acceleration.
            TimeToButton(); //time for player to reach each button from its location - necessary for scoring

            //decide scoring method based on state
            if(state == "spike"){
                for(int i = 0; i < bombScores.Length; i++){
                    SpikeStateScoring(i);
                }
            }
            else{
                for(int i = 0; i < bombScores.Length; i++){
                    DefendStateScoring(i);
                }
            }
            //get the index of the best bomb score
            int best = FindBestBomb();
            if(Mathf.Abs(mainScript.getCharacterLocation() - buttonLocations[best]) < .04){
                mainScript.push();
            }
            else{
                MoveTo(buttonLocations[best]);
            }
            ResetScores();
        }
        
        

	}

    void MyInitialize(int length){
        state = "spike";
        playerId = mainScript.isBlue;
        UpdateHealth();
        oppPos = mainScript.getOpponentLocation();
        playerPos = mainScript.getCharacterLocation();
        launchedBomb3 = false;

        bombAccelerations = new float[length];
        bombFirstVelocity = new float[length,2];
        bombNextVelocity = new float[length,2];
        bombLaunchTime = new float[length];
        bombDistances = mainScript.getBombDistances();
        beltLength = bombDistances[0]*2;
        timeToDetonation = new float[length];
        timeToButton = new float[length];
        bombScores = new float[length];

        for(int i = 0; i < bombAccelerations.Length; i++){
            bombAccelerations[i] = 0.0f;
            bombFirstVelocity[i,0] = 0.0f;
            bombFirstVelocity[i,1] = 0.0f;
            bombNextVelocity[i,0] = 0.0f;
            bombNextVelocity[i,1] = 0.0f;
            bombLaunchTime[i] = 0.0f;
            timeToDetonation[i] = 0.0f;
            bombScores[i] = 1.0f;
        }
    }

    void TriggerBomb3OnStart(){
        if(Mathf.Abs(playerPos - buttonLocations[3]) < .05){
            mainScript.push();
            launchedBomb3 = true;
        }
        else{
            mainScript.push();
            MoveTo(buttonLocations[3]);
        }
    }

    void UpdateHealth(){
        if(playerId){
            //player is blue
            currPlayerHealth = int.Parse(blueHealthIndicator.healthText.text);
            currOppHealth = int.Parse(redHealthIndicator.healthText.text);
        }
        else{
            //player is red
            currOppHealth = int.Parse(redHealthIndicator.healthText.text);
            currPlayerHealth = int.Parse(blueHealthIndicator.healthText.text);
        }
    }

    void ResetScores(){
        for(int i = 0; i < bombScores.Length; i++){
            bombScores[i] = 1.0f;
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
                    bombNextVelocity[i,0] = bombSpeeds[i];
                    currTime = Time.time;
                    bombNextVelocity[i,1] = currTime;
                }
                //if it has both next and first, move next to first, get new next
                else{
                    bombFirstVelocity[i,0] = bombNextVelocity[i,0];
                    bombFirstVelocity[i,1]  = bombNextVelocity[i,1];

                    bombNextVelocity[i,0] = bombSpeeds[i];
                    currTime = Time.time;
                    bombNextVelocity[i,1] = currTime;
                }
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
        //FinalPos = accel * t^2 + initialveloc * t + initialPos
        //a = currAcceleration, b = currVelocity, c = (current Distance from button * -1)
        for(int i = 0; i < timeToDetonation.Length; i++){
            if(beltDirections[i] != 0){
                timeToDetonation[i] = QuadraticCalc(beltDirections[i], bombDistances[i], bombSpeeds[i], bombAccelerations[i], i);
            }
            else{
                timeToDetonation[i] = 0.0f;
            }
        }
    }

    float QuadraticCalc(int direction, float position, float currVelocity, float currAcceleration, int index){
        float endPoint;
        if(direction == 1){
            endPoint = beltLength;

        }
        else{
            endPoint = 0;
        }
        position = (position - endPoint) * direction; 
        return (((currVelocity * -1) + Mathf.Sqrt((Mathf.Pow(currVelocity, 2.0f) - (4*currAcceleration*position)))) / (2*currAcceleration));
    }
    
    //Get time for player to reach target button from current position
    void TimeToButton(){
        for(int i = 0; i < timeToButton.Length; i++){
            timeToButton[i] = DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation()) / playerSpeed;
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
        if(currPlayerHealth > 3 || currPlayerHealth == currOppHealth){
            state = "spike";
        }
        else{
            state = "defend";
        }
        return state;
    }

    void SetBombLaunchTime(int index, float time){
        bombLaunchTime[index] = time;
    }

    void SpikeStateScoring(int i){
        /* Evaluating bomb scores is based on state and higher scores are preferrable: 
            Spike State:
            Idea is to focus attention on returning bombs at high speeds, reducing the chance the opponent can save them
                Non-active bombs are ignored except for bomb 3 at the very beginning. idk, it needs to get in position and do something while its at it
                Active bombs who's time to detonation is less than the time to reach the corresponding button are disregarded, cant reach them.
                Active bombs who's button is on cooldown and who's time to detonation is less than the time until the button is available are disregarded.
                Active bombs who's time to detonation, should they be reversed towards the opponent, is less than the cooldown (aka Spike Bombs) time AND the play can reach the button in time are given greatest preference.
                Bombs moving towards the opponent, aka no action available, are also ignored.

                //Numbers are a WIP
                Starting score: 1;
                Non-active bombs = score + 1;
                Active bombs that will detonate before the player can reach it = score * .2
                Active bombs that will detonate before the button is pressable = score * .1
                Spike bombs = score * 20
                Bombs moving towards the player = score * 0;
                Maybe: Bombs get a "multimove" modifier based on the immediate neighbors scores. modifier is + .10*(neighborscores total); To incentivize focusing on a bomb clusters.
                Point about Spikes: to determine if a ball is a spike, it might be necessary to calculate the time to detonation AFTER including the time to reach that button which gets complicated
                Additionally: it may be best to add in a tolerance for # of bombs that will detonate on the players side if a Spike is chosen instead, but that's a bit counter to the idea of aggressive attacking
        */

        //if bomb is moving
        if(beltDirections[i] != 0){
            //if bombs are approaching us: important for setting up spikes
            if(beltDirections[i] == -1){
                //if we cant reach it and the button wont be usable before detonation
                if(timeToDetonation[i] < timeToButton[i] || timeToDetonation[i] < buttonCooldowns[i]){
                    //Score them low because we can't get there to do anything, but modify them by their distance, ordering them closer = more preferrable
                    bombScores[i] = bombScores[i] * .04f * (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
                }
                //if can reach button before detonation
                else if(timeToDetonation[i] > timeToButton[i]){
                    //assuming we reset the bomb when will the bomb detonate on the opponent
                    float timeToDetOnEnemy = QuadraticCalc(1, (bombDistances[i] * bombSpeeds[i] * Mathf.Pow(bombAccelerations[i], timeToButton[i])), (bombSpeeds[i] * Mathf.Pow(bombAccelerations[i], timeToButton[i])), bombAccelerations[i], i);
                    //current opponent position to compare to position at start of frame: hoping this will be enough to give us a direction
                    float currentOppPos = mainScript.getOpponentLocation();
                    //determine direction
                    int opponentDirection = getOpponentDirection(currentOppPos);
                    //if opponent is moving towards the button or at it and not moving
                    if(movingToward(currentOppPos, opponentDirection, buttonLocations[i])){
                        //if the opponent will reach the bomb before the projected detonation time then its less useful, but might work
                        if((DistanceToButton(buttonLocations[i], currentOppPos)/playerSpeed) < timeToDetOnEnemy){
                            bombSpeeds[i] *= 0.1f;
                        }
                        //if opponent wont reach before detonation, much better target; guaranteed point
                        else{
                            bombSpeeds[i] *= 20;
                        }
                    }
                    //if opponent is moving away from the bomb, nice target; admittedly not thorough, they may still be close enough to do something but im lazy right now. lowering value to offset
                    else{
                        bombScores[i] *= 15;
                    }
                }
            }
            //if bombs are approaching enemny, less useful as little actions can be taken
            else{
                //bombs scored based on distance from us, generally hoping to not need to rely on these
                bombScores[i] = bombScores[i] * .1f * (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
            }
        }
        //bombs that aren't moving get a modified by .05 just cause we want to spread the bombs out but there may be cases where starting a new bomb is better
        else{
            bombScores[i] = bombScores[i] * .75f * (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
        }
    }

    void DefendStateScoring(int i){
        /*Defend State:
            Idea is to try to prevent as many detonations as possible, while redirecting any convenient bombs on that path (assuming there's no time cost to pushing a button);
                Starting score: 1;
                Non-active bombs = score * 1; Implementing this will require determining the best goal first so itll have to be iterative based on "best bomb" being the same
                Time to detonation < time to arrival/time to button released = score * .1;
                Time to detonation > time to arrival/time = (score * 10) - timeToDetonation; //Attempting to make the bombs with the lowest time to detonate worth more
                Time to target = score - timeToTarget (farther a target is, the lower the score)*/

        //if bomb is moving
        if(beltDirections[i] != 0){
            //if bomb is approaching me
            if(beltDirections[i] == -1){
                //if we can get to it and the button will be pressable, we prefer it. weighted by closeness to player current position
                if(timeToDetonation[i] > timeToButton[i] && timeToDetonation[i] > buttonCooldowns[i]){
                    bombScores[i] *= 20 * (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
                }
                //if its moving at us and we can't stop it, adjust score based on distance from us
                else{
                    bombScores[i] *= (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
                }
            }
            //approaching opponent so not helpful
            else if(beltDirections[i] == 1){
                bombScores[i] *= 0.06f * (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
            }
        }
        //bomb is not active
        else{
            bombScores[i] *= (1-((DistanceToButton(buttonLocations[i], mainScript.getCharacterLocation())/playerSpeed)/10));
        }

    }

    int getOpponentDirection(float currentOppPos){
            if(currentOppPos > oppPos){
                //opponent moving to up
                return 1;
                
            }
            else if(currentOppPos < oppPos){
                //opponent moving to down
                return -1;

            }
            else{
                //opponent not moving
                return 0;
            }
    }
    bool movingToward(float moverPos, int moverDir, float targetPos){
        if(moverPos > targetPos && moverDir == -1){
            //if mover is above the target and moving down, true
            return true;
        }
        else if(moverPos < targetPos && moverDir == 1){
            //if mover is below the target and moving up, true
            return true;
        }
        else if(moverPos == targetPos && moverDir == 0){
            //if mover is at target and not moving, true
            return true;
        }
        else{
            return false;
        }
    }

    float DistanceToButton(float buttonPos, float characterPos){
        if(buttonPos >= characterPos){
            return (buttonPos - characterPos);
        }
        else{
            return (characterPos - buttonPos);
        }
    }

    int FindBestBomb(){
        int indexOfBest = 0;
        for(int i = 1; i < bombScores.Length; i++){
            if(bombScores[i] > bombScores[indexOfBest]){
                indexOfBest = i;
            }
        }

        return indexOfBest;
    }

    void MoveTo(float target){
        if(playerPos > target){
            mainScript.moveDown();
        }
        else if(playerPos < target){
            mainScript.moveUp();
        }
        else{
            //no move needed why are you here?
        }
    }
}
