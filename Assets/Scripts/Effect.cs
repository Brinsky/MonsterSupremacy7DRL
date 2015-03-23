using UnityEngine;
using System.Collections;

public class Effect : Entity 
{
    public Sprite startSprite;
    public Point target;
    public float lifetime;

	// Use this for initialization
	void Start () 
    {
        base.Start();

        renderer.sortingLayerName = "Effects";
        renderer.sprite = startSprite;

        StartCoroutine(SmoothMove(position, target, lifetime));
	}
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}
