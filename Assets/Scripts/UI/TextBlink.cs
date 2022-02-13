using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TextBlink : MonoBehaviour 
{
    private bool blink = true;
    private Text text;
    public float blinkSpeed = 1f; // Alpha change per second

	void Start () 
    {
        text = GetComponent<Text>();
        StartCoroutine(SmoothBlink());
	}
    
    void Update()
    {
        if (Input.GetAxisRaw("Start / Consume") == 1)
            SceneManager.LoadScene("Main");

        if (Input.GetAxisRaw("Cancel") == 1)
            Application.Quit();
    }

    private IEnumerator SmoothBlink()
    {
        while (blink)
        {
            while (text.color.a > 0)
            {
                Color toBlink = text.color;
                toBlink.a -= blinkSpeed * Time.deltaTime;
                text.color = toBlink;

                yield return null;
            }

            while (text.color.a < 1)
            {
                Color toBlink = text.color;
                toBlink.a += blinkSpeed * Time.deltaTime;
                text.color = toBlink;

                yield return null;
            }
        }
    }
}
