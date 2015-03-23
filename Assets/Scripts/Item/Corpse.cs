using UnityEngine;
using System.Collections;

public class Corpse : Item
{
    public Power power;
    public int healthBonus = 1;

	// Use this for initialization
	void Start () 
    {
        base.Start();

        renderer.sprite = power.GetSprites().corpse;
	}

    public override void InteractWith(Player player)
    {
        player.stats.Health += healthBonus;

        GameManager.Level.RemoveItem(position.x, position.y);
        player.power = this.power;

        Destroy(this.gameObject);
    }
}
