﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool isGameOver = false;
    private void Update()
    {
        if(isGameOver  && Input.GetKeyDown(KeyCode.R))
        {
            // Scene - Game
            SceneManager.LoadScene(1);
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    public void GameOver()
    {
        isGameOver = true;
    }
}
