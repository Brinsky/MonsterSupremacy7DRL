using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TextFade : MonoBehaviour 
{
    private Text text;
    public const float duration = 2 * (GameManager.totalWaitTime / 3f);
    public float fadeSpeed = 1.0f / duration; // Alpha change per second

	void Start () 
    {
        text = GetComponent<Text>();
        text.color = Color.clear;
	}

	void Update () 
    {
        if (text.color.a > 0)
        {
            Color toFade = text.color;
            toFade.a -= fadeSpeed * Time.deltaTime;
            text.color = toFade;
        }
	}

    public void HealthChange(int deltaHP)
    {
        // Set the appropriate color (and reset the alpha)
        if (deltaHP > 0)
            text.color = Color.red;
        else
            text.color = Color.green;

        // Keep healing values from having a negative sign
        if (deltaHP < 0)
            deltaHP = -deltaHP;

        text.text = deltaHP.ToString();
    }

    // Special display when an attack misses
    public void Miss()
    {
        text.text = "0";
        text.color = Color.yellow;
    }
}
