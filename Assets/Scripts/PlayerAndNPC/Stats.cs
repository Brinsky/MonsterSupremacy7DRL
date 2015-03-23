using UnityEngine;
using System.Collections;

public class Stats 
{
    public int level = 1;

    public int xpMax = 100;
    public int xp = 0;

    public float chanceToHit = 0.75f;
    public int baseDamage = 5; // damage per hit

    public int piercing = 1; // damage through armor
    public int armor = 1; // damage reduction

    public int viewDist = 10;

    private int maxHealth = 100;

    public int MaxHealth
    {
        get { return maxHealth; }
        set 
        { 
            maxHealth = value;
            health = maxHealth;
        }
    }

    private int health = 100;
    public int Health
    {
        get { return health; }
        set { health = (value > maxHealth) ? maxHealth : value; }
    }

    public int previousHealth;


    // Turn-timing related variables
    public int speed = 100; // arbitrary units, since it's turn based rather than time based
    public int timeTillTurn = 100;

    public Stats(Organism self, int xpLevel)
    {
        if (self is Player)
            CalculatePlayerStats(xpLevel);
        else
            CalculateMonsterStats(self.power, xpLevel);

        this.previousHealth = health;
    }

    private void CalculateMonsterStats(Power power, int xpLevel)
    {
        level = xpLevel;
        chanceToHit += 0.01f * xpLevel;
        MaxHealth = 30 + (int)(xpLevel * (60f / GameManager.Manager.FLOOR_START));
        baseDamage = 5 + (xpLevel * 2);
        piercing = 1 + (xpLevel * 2);
        armor = 1 + (xpLevel * 2);

        if (power is BasicRanged)
        {
            piercing += 2;
            armor = System.Math.Max(0, armor - 3);
        }
        else if (power is BasicMelee)
        {
            piercing = System.Math.Max(0, piercing - 3);
            armor += 2;
        }
    }

    private void CalculatePlayerStats(int xpLevel)
    {
        level = xpLevel;
        chanceToHit += 0.01f * xpLevel;
        MaxHealth = 100 + (int)((xpLevel - 1) * (100f / GameManager.Manager.FLOOR_START));
        baseDamage = 5 + (xpLevel * 2);
        piercing = 1 + (xpLevel * 2);
        armor = 1 + xpLevel;

    }
}
