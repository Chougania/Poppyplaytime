using UnityEngine;

public class SkipButton : MonoBehaviour
{
    public NewGameButton newgamebutton;

    void Update()
    {
        if (Input.anyKeyDown)
        {
            newgamebutton.NewGameStart();
        }
    }
}
