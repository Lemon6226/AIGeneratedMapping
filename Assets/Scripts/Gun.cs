using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    public float bulletSpeed;
    public GameObject bulletPrefab;
    public Transform shootPoint;

    // zásobník = 12 nábojů, reload trvá 2 sekundy
    public int maxAmmo = 12;
    public float reloadTime = 2f;

    [SerializeField] TMP_Text ammoText;

    private int currentAmmo;
    private bool isReloading = false;
    private float reloadTimer = 0f;

    void Start()
    {
        currentAmmo = maxAmmo;

        // automatické hledání UI textu pokud není přiřazený
        if (ammoText == null)
        {
            var go = GameObject.Find("AmmoText");
            if (go != null) ammoText = go.GetComponent<TMP_Text>();
        }

        UpdateAmmoUI();
    }

    void Update()
    {
        // čekání na dokončení reloadu
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
                UpdateAmmoUI();
            }
            return;
        }

        // manuální reload klávesou R
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            StartReload();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // prázdný zásobník = automatický reload
            if (currentAmmo <= 0)
            {
                StartReload();
                return;
            }

            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.Euler(transform.forward));
            bullet.GetComponent<Rigidbody>().velocity = transform.forward * bulletSpeed;
            currentAmmo--;
            UpdateAmmoUI();
        }
    }

    void StartReload()
    {
        if (isReloading || currentAmmo == maxAmmo) return;
        isReloading = true;
        reloadTimer = reloadTime;
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        ammoText.text = isReloading ? "Reloading..." : $"{currentAmmo} / {maxAmmo}";
    }
}
