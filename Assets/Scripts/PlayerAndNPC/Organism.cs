using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// An organism is an entity that takes a turn each round (and probably also moves)
/// </summary>
public class Organism : Entity, IComparable<Organism>
{
    public Stats stats;
    public Power power;
    public int poisonDamagePerTurn = 0;
    public int poisonDuration = 0;

    protected Transform canvasTransform;
    protected GameObject healthBar;
    protected Slider healthSlider;
    protected Image barImage;
    protected TextFade damageIndicator;

    // Use this for initialization
    protected void Start()
    {
        base.Start();

        if (stats == null)
            stats = new Stats(this, 1);

        renderer.sortingLayerName = "Creatures";

        canvasTransform = ((GameObject)Instantiate(GameManager.Manager.canvasTemplate)).transform;
        canvasTransform.position = transform.position;
        canvasTransform.SetParent(transform);

        healthBar = Instantiate(GameManager.Manager.healthBar) as GameObject;
        healthBar.transform.position = transform.position + new Vector3(0, 0.7f, 0);
        healthBar.transform.SetParent(canvasTransform);

        Image[] bars = healthBar.GetComponentsInChildren<Image>();
        barImage = bars[1];

        healthSlider = healthBar.GetComponent<Slider>();
        healthSlider.maxValue = stats.MaxHealth;

        GameObject damageDisplay = Instantiate(GameManager.Manager.damageIndicator) as GameObject;
        damageDisplay.transform.position = transform.position + new Vector3(0, 1, 0);
        damageDisplay.transform.SetParent(canvasTransform);

        damageIndicator = damageDisplay.GetComponent<TextFade>();
    }

    protected void Update()
    {
        healthSlider.value = stats.Health;
        healthSlider.maxValue = stats.MaxHealth;

        // Death behavior
        if (stats.Health <= 0)
            OnDeath();

        // Turn the health bar blue while poisoned
        if (poisonDuration > 0)
            barImage.color = Color.blue;
        else
            barImage.color = Color.green;

        // Show an indication on the screen if an organism's health changes
        if (stats.Health != stats.previousHealth)
        {
            damageIndicator.HealthChange(stats.previousHealth - stats.Health);
            stats.previousHealth = stats.Health;
        }
    }

    protected virtual void OnDeath()
    {
        GameManager.Level.RemoveOrganism(position.x, position.y);
        GameManager.Level.turnOrder.Remove(this);
        Destroy(gameObject);
    }

    public void OnMiss()
    {
        damageIndicator.Miss();
    }

    public virtual void TakeTurn()
    {
        // Handle poison
        if (poisonDuration > 0)
        {
            poisonDuration--;
            stats.Health -= poisonDamagePerTurn;
        }
    }

    protected bool AttemptMove(int deltaX, int deltaY)
    {
        Point target = new Point(position.x + deltaX, position.y + deltaY);

        if (GameManager.Level.IsOpen(target.x, target.y))
        {
            Point original = position;

            // Logically move the organism to the new location
            GameManager.Level.RemoveOrganism(position.x, position.y);
            GameManager.Level.AddOrganism(this, target.x, target.y);

            // Show a smooth movement towards the new location
            StartCoroutine(SmoothMove(original, target, GameManager.totalWaitTime / 3f));

            return true;
        }

        return false;
    }

    protected bool AttemptMoveTowards(Point target)
    {
        int deltaX = 0;
        int deltaY = 0;

        if (target.x < position.x)
            deltaX = -1;
        else if (target.x > position.x)
            deltaX = 1;

        if (target.y < position.y)
            deltaY = -1;
        else if (target.y > position.y)
            deltaY = 1;

        return AttemptMove(deltaX, deltaY);
    }

    // Targeted attack
    protected bool AttemptAttack(Organism enemy)
    {
        if (IsVisible(enemy))
            return this.power.Attack(this, enemy);

        return false;
    }
    
    // Directional attack
    protected bool AttemptAttack(int deltaX, int deltaY)
    {
        Organism enemy = GameManager.Level.GetOrganism(position.x + deltaX, position.y + deltaY);

        if (enemy != null && IsVisible(enemy))
            return power.Attack(this, enemy);

        return false;
    }

    // Find the distance to another tile based on how many 'rings' away it is. 
    // To be used with attack range to determine if enemy is attackable.
    public int RangeTo(Point other)
    {
        int xDist = Math.Abs(other.x - this.position.x);
        int yDist = Math.Abs(other.y - this.position.y);

        return Math.Max(xDist,yDist);
    }

    public int CompareTo(Organism o)
    {
        return this.stats.timeTillTurn - o.stats.timeTillTurn;
    }

    // Determines whether or not a given entity is visible from this entity's current location
    protected bool IsVisible(Entity entity)
    {
        // Ray from this NPC to the entity
        Vector2 ray = (Vector2)entity.position - (Vector2)this.position;

        // Don't even try if the entity is farther away than our view distance
        if (ray.magnitude > stats.viewDist)
            return false;

        // The entity is visible if no collisions occurred (Raycast2DHit == false)
        return !Physics2D.Raycast(this.position, ray, ray.magnitude);

    }
}
