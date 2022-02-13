using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tiles;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour 
{
    // Useful static members
    public static GameManager Manager { get; private set; }
    public static SpriteManager Sprites { get; private set; }
    public static Player Player { get; private set; }
    public static Level Level { get; private set; }

    // Various UI prefabs
    public GameObject canvasTemplate;
    public GameObject healthBar;
    public GameObject damageIndicator;
    public GameObject introPanel;

    // Prefab used to retain data between floors
    public GameObject dataRetainer;

    public int Floor { get; private set; }
    public int FLOOR_START;
    public int KILLS_PER_LEVEL;

    public bool waitingPreTurn = false; // Used for delay before the Player's turn
    public bool waitingPostTurn = false; // Used for delay after the Player's turn
    public const float totalWaitTime = 0.15f; // Wait time of the pre and post delays combined
    public float waitTimeLeft = 0;

    private bool zoomedOut = false;

	void Start () 
    {
        Manager = this;

        // If we find data from a previous floor, load it in
        GameObject retainer = GameObject.FindGameObjectWithTag("DataRetainer");
        PersistentData previousFloorData = null;
        if (retainer != null)
        {
            previousFloorData = retainer.GetComponent<DataRetainer>().data;
            Destroy(retainer);
        }

        // Initialize the sprite manager (possibly based on the previous floor's data)
        Sprites = GameObject.FindGameObjectWithTag("SpriteManager").GetComponent<SpriteManager>();
        if (previousFloorData != null)
            Sprites.InitializeColors(previousFloorData.colors);
        else
            Sprites.InitializeColors();

        // Initialize the tile handler
        TileHandler.Initialize();
        
        // Update the floor number if there was a previous floor
        Floor = (previousFloorData != null) ? previousFloorData.floor : FLOOR_START;

        // Victory condition
        if (Floor == 0)
            SceneManager.LoadScene("Victory");

        // Generate the new level
        Level = new Level(Floor, Random.Range(30, 40), Random.Range(30, 40));

        // Find the newly created player
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        // Retreive the player's data if there was a previous floor
        if (previousFloorData != null)
        {
            Player.oldStats = previousFloorData.playerStats;
            Player.oldPower = previousFloorData.playerPower;
            Player.poisonDuration = previousFloorData.poisonDuration;
            Player.poisonDamagePerTurn = previousFloorData.poisonDamage;
        }

        // Update the floor number on the UI
        Text floorText = GameObject.FindGameObjectWithTag("FloorValue").GetComponent<Text>();
        floorText.text = Floor.ToString();

        // Get a handle on the intro panel
        introPanel = GameObject.FindGameObjectWithTag("IntroPanel");
        if (previousFloorData != null)
            introPanel.SetActive(false);

        ShowPlayer();
	}

    void Update()
    {
        // Wait before the player's turn to allow monster movements to complete and
        // to maintain a reasonable pace
        if (waitingPreTurn)
        {
            waitTimeLeft -= Time.deltaTime;

            if (waitTimeLeft <= 0)
            {
                waitingPreTurn = false;
                Player.isTurn = true;
            }
        }
        // We must wait after the turn to allow bounce-back movements to complete
        else if (waitingPostTurn)
        {
            waitTimeLeft -= Time.deltaTime;

            if (waitTimeLeft <= 0)
                waitingPostTurn = false;
        }
        else if (Player.isTurn == false)
        {
            // Update all organism times-to-turn
            while (!(Level.turnOrder[0] is Player))
            {
                // Take a turn and reset time till turn to the speed of the organism
                Level.turnOrder[0].TakeTurn();

                // Perform updates to the turn list
                EndTurn();
            }

            // Wait before the player's turn each round (for 2/3 of the total wait time)
            waitingPreTurn = true;
            waitTimeLeft = 2 * (totalWaitTime / 3f);
        }

        if (Input.anyKeyDown)
            introPanel.SetActive(false);

    #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            zoomedOut = !zoomedOut;

            if (zoomedOut)
                ShowLevel();
            else
                ShowPlayer();
        }
    #endif
    }

    public void EndTurn()
    {
        // Use the time left for the npc to move next to update the timeTillTurn for all other organisms
        int timeDiff = Level.turnOrder[0].stats.timeTillTurn;
        for (int i = 1; i < Level.turnOrder.Count; i++)
            Level.turnOrder[i].stats.timeTillTurn -= timeDiff;

        // Reset the time on the organism whose turn just ended
        Level.turnOrder[0].stats.timeTillTurn = Level.turnOrder[0].stats.speed;

        // Place the organism at the proper spot in the list
        Organism temp = Level.turnOrder[0];
        Level.turnOrder.RemoveAt(0);

        // Find the suggested spot through BinarySearch
        int index = Level.turnOrder.BinarySearch(temp);
        if (index < 0)
            index = -(index + 1);
        // Scan further along the list in case multiple organisms with the same speed.
        while (index < Level.turnOrder.Count && Level.turnOrder[index].stats.speed == temp.stats.speed)
            index++;

        Level.turnOrder.Insert(index, temp);
    }

    public void LoadNextFloor()
    {
        // Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Prepare the transition data
        PersistentData transition = new PersistentData();
        transition.floor = Floor - 1;
        transition.playerStats = Player.stats;
        transition.playerPower = Player.power;
        transition.poisonDamage = Player.poisonDamagePerTurn;
        transition.poisonDuration = Player.poisonDuration;
        transition.colors = SpriteManager.colors;

        // Prepare the data retainer for the next floor and make it persistent
        GameObject retainer = Instantiate(dataRetainer) as GameObject;
        retainer.GetComponent<DataRetainer>().data = transition;
        DontDestroyOnLoad(retainer);

        Level = null;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Centers the camera on the player and zooms it in
    private void ShowPlayer()
    {
        Camera.main.transform.position = Player.transform.position + new Vector3(0, 0, -10);
        Camera.main.transform.SetParent(Player.transform);
        Camera.main.orthographicSize = 4.25f;
    }

    // Fits the entire level to the camera
    private void ShowLevel()
    {
        // Center the camera and size it to match the level
        Camera.main.transform.position = new Vector3((Level.Size.x - 1) / 2.0f, (Level.Size.y - 1) / 2.0f, -10);
        Camera.main.transform.SetParent(transform);
        Camera.main.orthographicSize = Level.Size.y / 2.0f;
    }

    // Data that carries over from floor to floor
    public class PersistentData
    {
        public Power playerPower;
        public Stats playerStats;
        public int poisonDamage;
        public int poisonDuration;
        public int floor;
        public Dictionary<Type, SpriteManager.MonsterColor> colors;
    }
}
