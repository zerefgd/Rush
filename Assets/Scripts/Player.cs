using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Finish"))
        {
            GameManager.instance.hasGameStarted = false;
            int passed = int.Parse(other.gameObject.GetComponentInChildren<TMPro.TMP_Text>().text);
            GameManager.instance.UpdateScore(passed);
            Invoke("RestartGame", 2f);
        }
    }

    void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
