using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour 
{
	void Start () 
    {
	
	}

	void Update () 
    {
        if (Input.GetAxisRaw("Cancel") == 1)
            SceneManager.LoadScene("Menu");
	}
}
