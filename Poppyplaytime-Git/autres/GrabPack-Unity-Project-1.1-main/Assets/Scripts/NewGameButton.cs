using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class NewGameButton : MonoBehaviour
{
    public int sceneindex;
    public GameObject FadeScreen;
    private AudioSource audiosource;

    public AudioClip startsfx;

    public Animator musicAnim;

    public float volume = 1.0f;

    public bool loading = false;

    void Awake()
    {
        audiosource = GetComponent<AudioSource>();
    }

    public void NewGameStart()
    {
        FadeScreen.SetActive(true);
        audiosource.PlayOneShot(startsfx, volume);
        if (musicAnim != null)
        {
            musicAnim.SetTrigger("load");

        }
        if (loading) return;
        StartCoroutine(Loading());
        

    }

    IEnumerator Loading()
    {
        yield return new WaitForSeconds(3.5f);
        SceneManager.LoadScene(sceneindex);

    }

}
