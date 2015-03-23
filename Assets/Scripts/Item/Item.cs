using UnityEngine;
using System.Collections;

public class Item : Entity
{
    public string name;
    public Sprite startSprite;

    protected void Start()
    {
        base.Start();

        renderer.sortingLayerName = "Items";

        if (startSprite != null)
            renderer.sprite = startSprite;
    }

    public Item()
    {
        name = "";
    }

    public virtual void InteractWith(Player player)
    {

    }
}
