using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FlareHand : MonoBehaviour
{
    public GameObject flarePrefab;
    public Transform firePoint;
    public float launchForce = 20f;
    public float cooldown = 0.5f;

    private float cooldownTimer = 0f;

    public Animator anim;
    public MobileIcons mobileIcons;


    public Material ammo5;
    public Material ammo4;
    public Material ammo3;
    public Material ammo2;
    public Material ammo1;
    public Material ammo0;

    public MeshRenderer screen;

    public int maxAmmo = 5;
    public float reloadTime = 3f;
    public int ammo = 5;

    private bool isReloading = false;

    private AudioSource audioSource;
    public AudioClip flareshotsfx;

    public GameObject mobileButton;

    public ParticleSystem muzzleflash;

    public Animator playeranimations;

    void OnEnable()
    {

        mobileButton.SetActive(true);

        isReloading = false;

        if (ammo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    void OnDisable()
    {
        mobileButton.SetActive(false);

    }

    void Start()
    {
        UpdateAmmo();
        audioSource = GetComponent<AudioSource>();

    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (!mobileIcons.isMobile)
        {
            if (ammo > 0 && !isReloading)
            {
                if (Input.GetMouseButtonDown(1) && cooldownTimer <= 0f)
                {
                    FireFlare();
                    cooldownTimer = cooldown;
                }
            }
        }
    }

    void FireFlare()
    {
        GameObject flare = Instantiate(flarePrefab, firePoint.position, firePoint.rotation);
        playeranimations.SetTrigger("fireright");

        Rigidbody rb = flare.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * launchForce;
        }
        muzzleflash.Play();
        anim.SetTrigger("fire");
        ammo -= 1;
        audioSource.PlayOneShot(flareshotsfx, 1.0f);
        UpdateAmmo();
    }

    public void UpdateAmmo()
    {
        if (ammo == 5)
        {
            screen.material = ammo5;
        }
        else if(ammo == 4)
        {
            screen.material = ammo4;
        }
        else if (ammo == 3)
        {
            screen.material = ammo3;
        }
        else if (ammo == 2)
        {
            screen.material = ammo2;
        }
        else if (ammo == 1)
        {
            screen.material = ammo1;
        }
        else if (ammo == 0)
        {
            screen.material = ammo0;
            StartCoroutine(Reload());

        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        float timePerAmmo = reloadTime / maxAmmo;

        while (ammo < maxAmmo)
        {
            yield return new WaitForSeconds(timePerAmmo);

            ammo++;
            UpdateAmmo();
        }

        isReloading = false;
    }

    public void FireMobile()
    {
        if (ammo > 0 && !isReloading && cooldownTimer <= 0f)
        {
            FireFlare();
            cooldownTimer = cooldown;
        }
    }
}