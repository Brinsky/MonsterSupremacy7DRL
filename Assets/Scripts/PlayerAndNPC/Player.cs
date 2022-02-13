using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : Organism
{
    public bool isTurn = false;

    // Data from previous floors
    public Power oldPower = null;
    public Stats oldStats = null;

    // Hooks for UI elements
    private Text healthText;
    private Text xpText;
    private Text powerText;
    private Text expLevelText;
    protected Slider xpSlider;
    protected Image xpImage;

    void Start()
    {
        base.Start();

        // Get rid of the default health bar and link up with the big one
        Destroy(healthBar);
        healthBar = GameObject.FindGameObjectWithTag("HealthBar");
        healthSlider = healthBar.GetComponent<Slider>();
        Image[] bars = healthBar.GetComponentsInChildren<Image>();
        barImage = bars[1];
        healthText = GameObject.FindGameObjectWithTag("HealthValue").GetComponent<Text>();

        // Link up with the big XP bar
        GameObject xpBar = GameObject.FindGameObjectWithTag("ExperienceBar");
        xpSlider = xpBar.GetComponent<Slider>();
        bars = xpBar.GetComponentsInChildren<Image>();
        xpImage = bars[1];
        xpText = GameObject.FindGameObjectWithTag("ExperienceValue").GetComponent<Text>();
        expLevelText = GameObject.FindGameObjectWithTag("LevelValue").GetComponent<Text>();

        // Link up with the power panel
        powerText = GameObject.FindGameObjectWithTag("PowerValue").GetComponent<Text>();

        // Since we don't have a health bar, the damage indicator should be closer to our head
        damageIndicator.gameObject.transform.position += new Vector3(0, -0.3f, 0);

        renderer.sprite = GameManager.Sprites.player;

        // Use stats from the previous floor if they are available
        if (oldStats != null)
            stats = oldStats;
        else
            stats = new Stats(this, 1);

        // Use the old power if available
        if (oldPower != null)
            power = oldPower;
    }

    void Update()
    {
        base.Update();

        if (Input.GetAxisRaw("Cancel") == 1)
            SceneManager.LoadScene("Menu");

    #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha2))
            stats.Health += 10;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            stats.xp += 10;
    #endif

        // Handle XP overflow/levelup
        if (stats.xp >= stats.xpMax)
            stats = new Stats(this, stats.level + 1);


        // Update slider values
        xpSlider.value = stats.xp;
        xpSlider.maxValue = stats.xpMax;

        // Update various stat texts
        xpText.text = "EXP: " + stats.xp + "/" + stats.xpMax;
        expLevelText.text = stats.level.ToString();
        healthText.text = "HP: " + stats.Health + "/" + stats.MaxHealth;
        powerText.text = power.Name();

        // Allow turn actions to be performed during the turn
        if (isTurn)
            DuringTurn();
    }

    public void DuringTurn()
    {
        int deltaX = (int)Input.GetAxisRaw("Horizontal");
        int deltaY = (int)Input.GetAxisRaw("Vertical");

        // If the player is not trying to move orthogonally, check for diagonal movement
        if (deltaX == 0 && deltaY == 0)
        {
            int upRight = (int)Input.GetAxisRaw("/ Diagonal");
            int upLeft = (int)Input.GetAxisRaw("\\ Diagonal");

            if (upRight != 0)
            {
                deltaX = upRight;
                deltaY = upRight;
            }
            else if (upLeft != 0)
            {
                deltaX = -upLeft;
                deltaY = upLeft;
            }
        }

        // If movement or a directional attack is being attempted
        if (deltaX != 0 || deltaY != 0)
        {
            Organism victim = GameManager.Level.GetOrganism(position.x + deltaX, position.y + deltaY);

            if (victim != null)
                AttemptAttack(deltaX, deltaY);
            else
                AttemptMove(deltaX, deltaY);

            EndTurn();
        }
        // If the player is trying to attack based on mouse targeting
        else if (Input.GetAxisRaw("Fire") == 1)
        {
            // Find the mouse position and round it to a world position
            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int targetX = (int)(target.x + 0.5f);
            int targetY = (int)(target.y + 0.5f);

            Organism victim = GameManager.Level.GetOrganism(targetX, targetY);

            // Attack if a valid organism is found and it isn't us
            if (victim != null && !(victim is Player))
                if (AttemptAttack(victim))
                    EndTurn();
        }
        // If the player is trying to use stairs
        else if (Input.GetAxisRaw("Use Stairs") == 1)
        {
            if (position == GameManager.Level.Upstairs)
                GameManager.Manager.LoadNextFloor();
        }
        // If the player is trying to eat a corpse/pick up an item
        else if (Input.GetAxisRaw("Start / Consume") == 1)
        {
            Item item = GameManager.Level.GetItem(position.x, position.y);

            if (item != null)
                item.InteractWith(this);

            EndTurn();
        }
        else if (Input.GetAxisRaw("Wait") == 1)
        {
            EndTurn();
        }
    }

    // Post-turn clean-up, call when the turn should be ended
    private void EndTurn()
    {
        isTurn = false;
        base.TakeTurn();
        GameManager.Manager.EndTurn();

        // Engage the post-turn wait period (so that bounce-backs can complete)
        GameManager.Manager.waitingPostTurn = true;
        GameManager.Manager.waitTimeLeft = GameManager.totalWaitTime / 3f;
    }

    protected override void OnDeath()
    {
        SceneManager.LoadScene("Menu");
    }
}
