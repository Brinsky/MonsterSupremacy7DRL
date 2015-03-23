using UnityEngine;
using System.Collections;

public class ReturnToMenu : MonoBehaviour 
{
	void Start () 
    {
	
	}

	void Update () 
    {
        if (Input.GetAxisRaw("Cancel") == 1)
            Application.LoadLevel("Menu");
	}
}
