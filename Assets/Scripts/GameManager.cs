using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
    public class GameManager : MonoBehaviour
{

    public static GameManager instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.

    public List<Character> Characters = new List<Character>();
    public int SelectedCharacter=0;
   // public GameObject[] Characters;

    //Awake is always called before any Start functions
    void Awake()
    {
        //Check if instance already exists
        if (instance == null)

            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        InitGame();
    }

    //Initializes the game for each level.
    void InitGame()
    {
 
    }

    public void UnselectAll()
    {
        foreach (Character item in Characters)
        {
            item.Select(false);
        }
        SelectedCharacter = 0;
    }
    public void AddCharacter(Character newItem)
    {
        Characters.Add(newItem);
    }

}
