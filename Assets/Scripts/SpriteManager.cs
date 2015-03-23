using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using Random = UnityEngine.Random;

public class SpriteManager : MonoBehaviour
{
    public static Dictionary<Type, MonsterColor> colors;

    // Generate a new set of power types - colors
    public void InitializeColors()
    {
        colors = new Dictionary<Type, MonsterColor>();

        List<MonsterColor> options = new List<MonsterColor>();
        options.Add(new MonsterColor(monsterCyan, corpseCyan));
        options.Add(new MonsterColor(monsterBlue, corpseBlue));
        options.Add(new MonsterColor(monsterRed, corpseRed));
        options.Add(new MonsterColor(monsterGreen, corpseGreen));

        int removeIndex;

        removeIndex = Random.Range(0, options.Count);
        MonsterColor poison = options[removeIndex];
        options.RemoveAt(removeIndex);
        colors.Add(typeof(Poison), poison);

        removeIndex = Random.Range(0, options.Count);
        MonsterColor melee = options[removeIndex];
        options.RemoveAt(removeIndex);
        colors.Add(typeof(BasicMelee), melee);

        removeIndex = Random.Range(0, options.Count);
        MonsterColor ranged = options[removeIndex];
        options.RemoveAt(removeIndex);
        colors.Add(typeof(BasicRanged), ranged);

        removeIndex = Random.Range(0, options.Count);
        MonsterColor bounce = options[removeIndex];
        options.RemoveAt(removeIndex);
        colors.Add(typeof(BounceBack), bounce);
    }

    // Use a set of power types - colors from a previous level
    public void InitializeColors(Dictionary<Type, MonsterColor> oldColors)
    {
        colors = oldColors;
    }

    public static MonsterColor GetPowerColors(Type power)
    {
        return colors[power];
    }

    public struct MonsterColor
    {
        public MonsterColor(Sprite monster, Sprite corpse)
        {
            this.monster = monster;
            this.corpse = corpse;
        }

        public Sprite monster;
        public Sprite corpse;
    }

    [Header("Organism sprites")]
    public Sprite player;
    public Sprite enemyMonster;
    public Sprite enemyMonsterCorpse;

    [Header("Monster sprites")]
    public Sprite monsterCyan;
    public Sprite monsterBlue;
    public Sprite monsterRed;
    public Sprite monsterGreen;

    [Header("Corpse sprites")]
    public Sprite corpseCyan;
    public Sprite corpseBlue;
    public Sprite corpseRed;
    public Sprite corpseGreen;

    [Header("Level tiles")]
    public Sprite ground;
    public Sprite wall;
    public Sprite upstairs;
    public Sprite downstairs;

    [Header("Item sprites")]
    public Sprite ring;
    public Sprite scroll;

    [Header("Effect sprites")]
    public Sprite shuriken;
    public Sprite fireball;
}
