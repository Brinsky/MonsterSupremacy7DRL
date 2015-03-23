using UnityEngine;
using System.Collections;

public class EnemyMonster : NPC
{
    // Where the player was last spotted - used to help with following
    Point lastPlayerLocationSeen = null;

	void Start () 
    {
        base.Start();

        renderer.sprite = power.GetSprites().monster;
        renderer.sortingLayerName = "Creatures";

        // Add a collider so that the player can raycast at us
        //var collider = gameObject.AddComponent<CircleCollider2D>();
        //collider.radius = 0.5f;
	}
	
	void Update () 
    {
        base.Update();
	}

    protected override void OnDeath()
    {
        base.OnDeath();

        // Give XP to the player
        // XP is scaled based on the difference between the player and monster levels
        Stats playerStats = GameManager.Player.stats;
        int levelDifference = this.stats.level - playerStats.level;
        float xpModifier = Mathf.Pow(2, levelDifference);
        playerStats.xp += (int) Mathf.Ceil(playerStats.xpMax / (float)GameManager.Manager.KILLS_PER_LEVEL * xpModifier);

        // Create corpse item
        Corpse corpse = GameManager.Level.Place<Corpse>(position.x, position.y);
        corpse.power = this.power;
        corpse.healthBonus = this.stats.MaxHealth;
    }

    public override void TakeTurn()
    {
        base.TakeTurn();

        // If we are at the player's last seen location then it's no longer a valid position to target
        if (lastPlayerLocationSeen != null && this.position == lastPlayerLocationSeen)
            lastPlayerLocationSeen = null;

        // If the player is in sight
        if (IsVisible(GameManager.Player))
        {
            // Set their last seen location in case they go out of view
            lastPlayerLocationSeen = new Point(GameManager.Player.position.x, GameManager.Player.position.y);

            // If within attack range, then attack. Else move toward player
            if (!AttemptAttack(GameManager.Player))
                AttemptMoveTowards(GameManager.Player.position);
        }
        else // If we cannot see the player...
        {
            if (lastPlayerLocationSeen == null) // And we don't know where to look, then move randomly
            {
                AttemptMove(Random.Range(-1, 2), Random.Range(-1, 2));
            }
            else  // Otherwise move towards the spot they were last seen
            {
                AttemptMoveTowards(lastPlayerLocationSeen);
            }
        }
    }
}
