using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public List<(int, int)> FieldSize;

    public GameObject LeftArrow;
    public GameObject RightArrow;
    public Text TextFieldSize;
    public GameObject ExitPanel;


    int curIndexSize = 0;

    void Start()
    {
        FieldSize = new List<(int, int)>()
        {
            (3, 3),
            (7, 5),
            (9, 9),
            (11, 7),
            (13, 9),
            (15, 9),
            (18, 9)
        };
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ExitPanel.SetActive(true);

        if (curIndexSize == FieldSize.Count - 1)
            RightArrow.SetActive(false);
        else if (curIndexSize == 0)
            LeftArrow.SetActive(false);
        else
        {
            RightArrow.SetActive(true);
            LeftArrow.SetActive(true);
        }
        TextFieldSize.text = FieldSize[curIndexSize].Item1 + " X " + FieldSize[curIndexSize].Item2;
        GameManager.N = FieldSize[curIndexSize].Item1;
        GameManager.M = FieldSize[curIndexSize].Item2;
    }

    public void NextSize()
    {
        if (curIndexSize != FieldSize.Count - 1)
            ++curIndexSize;
    }

    public void PrevSize()
    {
        if (curIndexSize != 0)
            --curIndexSize;
    }

    public void HumanGame()
    {
        GameManager.vsAI = false;
        SceneManager.LoadScene("SampleScene");
    }

    public void ComputerGame()
    {
        GameManager.vsAI = true;
        SceneManager.LoadScene("SampleScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
