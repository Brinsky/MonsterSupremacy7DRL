using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract class Power
{
    public Power(float damageModifier)
    {
        this.damageModifier = damageModifier;
    }

    public int attackRange = 1;

    protected float damageModifier; // How much of the player's base damage is done by the power

    public abstract bool Attack(Organism self, Organism enemy);

    protected int CalculateDamage(Organism self, Organism enemy)
    {
        if (UnityEngine.Random.value > self.stats.chanceToHit) // If self misses the enemy
            return 0;
        else
        {
            // armor, piercing, baseDamage, damageModifier +/- 10%
            int damageReduction = Math.Max(enemy.stats.armor - self.stats.piercing, 0);
            float modifiedDamage = self.stats.baseDamage * self.power.damageModifier;
            modifiedDamage *= (UnityEngine.Random.value / 5 + 0.9f);
            modifiedDamage -= damageReduction;
            return (int)Math.Ceiling(modifiedDamage);
        }
    }

    protected void ApplyDamage(Organism enemy, int damage)
    {
        if (damage <= 0)
            enemy.OnMiss();
        else
            enemy.stats.Health -= damage;
    }

    public SpriteManager.MonsterColor GetSprites()
    {
        return SpriteManager.GetPowerColors(this.GetType());
    }

    public abstract string Name();
}

public class Poison : Power
{
    int duration = 4;

    public Poison(float damageModifier, int duration) : base(damageModifier)
    {
        this.duration = duration;
    }

    public override bool Attack(Organism self, Organism enemy)
    {
        int range = self.RangeTo(enemy.position);

        if (range <= attackRange)
        {
            enemy.poisonDuration = this.duration;
            enemy.poisonDamagePerTurn = (int)Math.Ceiling(damageModifier * self.stats.baseDamage);
            return true;
        }
        return false;
    }

    public override string Name()
    {
        return "Poison";
    }
}

public class BasicRanged : Power
{
    public BasicRanged(float damageModifier, int attackRange) : base(damageModifier)
    {
        this.attackRange = attackRange;
    }

    public override bool Attack(Organism self, Organism enemy)
    {
        int range = self.RangeTo(enemy.position);

        if (range <= attackRange)
        {
            Effect projectile = GameManager.Level.Place<Effect>(self.position.x, self.position.y);
            projectile.startSprite = GameManager.Sprites.fireball;
            projectile.target = enemy.position;
            projectile.lifetime = GameManager.totalWaitTime;

            // Apply damage and display it
            ApplyDamage(enemy, CalculateDamage(self, enemy));
            return true;
        }
        return false;
    }

    public override string Name()
    {
        return "Fireball";
    }
}

public class BasicMelee : Power
{
    public BasicMelee(float damageModifier) : base(damageModifier)
    {
        
    }

    public override bool Attack(Organism self, Organism enemy)
    {
        int range = self.RangeTo(enemy.position);

        if (range <= attackRange)
        {
            // Apply damage and display it
            ApplyDamage(enemy, CalculateDamage(self, enemy));
            return true;
        }
        return false;
    }

    public override string Name()
    {
        return "Claw";
    }
}

public class BounceBack : Power
{
    public BounceBack(float damageModifier) : base(damageModifier)
    {
        
    }

    public override bool Attack(Organism self, Organism enemy)
    {
        int range = self.RangeTo(enemy.position);

        if (range <= attackRange)
        {
            // Apply damage and display it
            ApplyDamage(enemy, CalculateDamage(self, enemy));

            // Bounce away
            int deltaX = enemy.position.x - self.position.x;
            int deltaY = enemy.position.y - self.position.y;

            // If possible, push the enemy away
            if (GameManager.Level.IsOpen(enemy.position.x + deltaX, enemy.position.y + deltaY))
            {
                GameManager.Level.RemoveOrganism(enemy.position.x, enemy.position.y);
                GameManager.Level.AddOrganism(enemy, enemy.position.x + deltaX, enemy.position.y + deltaY);
            }
            // Otherwise, try to push self away
            else if (GameManager.Level.IsOpen(self.position.x - deltaX, self.position.y - deltaY))
            {
                GameManager.Level.RemoveOrganism(self.position.x, self.position.y);
                GameManager.Level.AddOrganism(self, self.position.x - deltaX, self.position.y - deltaY);
            }

            return true;
        }
        return false;
    }

    public override string Name()
    {
        return "Bounce";
    }
}