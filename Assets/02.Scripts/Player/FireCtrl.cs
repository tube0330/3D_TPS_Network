using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Photon.Pun;

[System.Serializable]
public struct PlayerSound
{
    public AudioClip[] fire;
    public AudioClip[] reload;
}
public class FireCtrl : MonoBehaviourPun
{
    public enum weaponType
    {
        RIFLE = 0, SHOTGUN = 1
    }
    public weaponType curWeaponType = weaponType.SHOTGUN;
    public PlayerSound playerSound;

    [SerializeField] float firetime;    //쿨타임
    [SerializeField] private Transform firePos;
    //[SerializeField] AudioClip fireclip;
    //[SerializeField] AudioSource Source;
    [SerializeField] Player _player;
    [SerializeField] private ParticleSystem muzzleFlash;
    private readonly string enemyTag = "ENEMY";
    private readonly string WallTag = "WALL";
    private readonly string BarrelTag = "BARREL";
    private const float DIST = 20f;

    public Image magazineImage;
    public Text magazineTxt;
    public float reloadTime = 2.0f;
    public bool isReload = false;
    public int maxBullet = 10;
    public int curBullet = 10;

    public Sprite[] weaponIcon;
    public Image weaponImg;

    [Header("Raycast to Attack Enemies")]
    public bool isFire = false;
    private float nextFire;         //다음 발사 시간 저장 변수
    public float fireRate = 0.1f;   //총알 발사 간걱
    private int enemyLayer;     //적 레이어 번호를 받을 변수
    private int boxLayer;
    private int barrelLayer;
    private int layerMask;

    [Header("playerInput")]
    PlayerInput playerInput;
    InputActionMap playerMap;
    InputAction fireAction;

    void Start()
    {
        firetime = Time.time;
        //fireclip = Resources.Load("Sounds/p_ak_1") as AudioClip;
        _player = GetComponent<Player>();
        muzzleFlash.Stop();
        magazineImage = GameObject.Find("Canvas_UI").transform.GetChild(1).GetChild(2).GetComponent<Image>();
        magazineTxt = GameObject.Find("Canvas_UI").transform.GetChild(1).GetChild(0).GetComponent<Text>();
        weaponIcon = Resources.LoadAll<Sprite>("WeaponIcons");
        weaponImg = GameObject.Find("Canvas_UI").transform.GetChild(3).GetChild(0).GetComponent<Image>();

        enemyLayer = LayerMask.NameToLayer("ENEMY");    //레이어의 이름을 레이어의 인덱스(정수 값)로 변환
        barrelLayer = LayerMask.NameToLayer("BARREL");
        boxLayer = LayerMask.NameToLayer("BOXES");
        layerMask = 1 << enemyLayer | 1 << barrelLayer | 1 << boxLayer | 1 << layerMask;

        playerInput = GetComponent<PlayerInput>();
        playerMap = playerInput.actions.FindActionMap("Player");
        fireAction = playerInput.actions["Fire"];
    }
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;   //UI에 특정 이벤트가 발생되면 빠져나감

        #region Fire to RayCastHit
        /* RaycastHit hit; //광선에 맞은 오브젝트의 위치와 거리 정보가 담긴 구조체
        if (Physics.Raycast(firePos.position, firePos.forward, out hit, 25f, layerMask))
            isFire = hit.collider.CompareTag(enemyTag);

        else
            isFire = false; */

        #region 0.1초마다 발사
        /* if (!isReload && isFire)
        {
            if (Time.time > nextFire)
            {
                --curBullet;
                Fire();
                nextFire = Time.time + fireRate;  //0.1초마다 발사

                if (curBullet == 0)
                    StartCoroutine(Reloading());
            }
        } */
        #endregion
        #endregion

        #region Fire to MouseButtonDown(0)
        /* if (Input.GetMouseButtonDown(0) && !isReload)
        {
            if (!isReload)
            {
                --curBullet;

                Fire();
                muzzleFlash.Play();

                if (curBullet == 0)
                {
                    StartCoroutine(Reloading());
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
            muzzleFlash.Stop();
        else
            muzzleFlash.Stop(); */
        #endregion

        #region playerInputSystem
        if (photonView.IsMine)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // 발사 조건: 발사 버튼이 눌린 상태이고, 리로드 중이 아니며, 다음 발사 시간이 지난 경우
            if (fireAction.ReadValue<float>() > 0f && !isReload && Time.time > nextFire)
            {
                if (curBullet > 0)
                {
                    Fire();  // 총알 발사
                    muzzleFlash.Play();
                    nextFire = Time.time + fireRate;  // 다음 발사 시간 설정
                    photonView.RPC(nameof(Fire), RpcTarget.Others);
                    if (curBullet == 0)
                        StartCoroutine(Reloading());
                }
            }

            if (curBullet == 0 || fireAction.ReadValue<float>() == 0f)
                muzzleFlash.Stop();

            UpdateBulletTxt();
        }
        #endregion
    }

    private void UpdateBulletTxt()
    {
        magazineImage.fillAmount = (float)curBullet / (float)maxBullet;
        magazineTxt.text = $"<color=#64FFFF>{curBullet}</color>/{maxBullet}";
    }

    public void OnChangeWeapon()
    {
        curWeaponType = (weaponType)((int)++curWeaponType % 2);
        weaponImg.sprite = weaponIcon[(int)curWeaponType];  //스프라이트 이미지
    }

    IEnumerator Reloading()
    {
        isReload = true;
        SoundManager.S_instance.PlaySound(transform.position, playerSound.reload[(int)curWeaponType]);
        yield return new WaitForSeconds(playerSound.reload[(int)curWeaponType].length + 0.0f);

        curBullet = maxBullet;
        magazineImage.fillAmount = 1.0f;
        UpdateBulletTxt();
        isReload = false;
    }
    [PunRPC]
    private void Fire()
    {
        isFire = true;
        --curBullet;  // 한 발씩 총알 감소

        RaycastHit hit; // 광선이 오브젝트에 맞으면 충돌지점이나 거리들을 알려주는 광선 구조체
        if (Physics.Raycast(firePos.position, firePos.forward, out hit, DIST))
        {
            // 적을 맞췄을 때 처리
            if (hit.collider.CompareTag(enemyTag))
            {
                object[] _params = new object[2];
                _params[0] = hit.point; // 첫번째 배열에는 맞은 위치를 전달
                _params[1] = 25f; // 데미지 값을 전달
                hit.collider.SendMessage("OnDamage", _params, SendMessageOptions.DontRequireReceiver);
            }

            // 벽을 맞췄을 때 처리
            else if (hit.collider.CompareTag(WallTag))
            {
                object[] _params = new object[2];
                _params[0] = hit.point; // 첫번째 배열에는 맞은 위치를 전달
                hit.collider.SendMessage("OnDamage", _params, SendMessageOptions.DontRequireReceiver);
            }

            // 배럴을 맞췄을 때 처리
            else if (hit.collider.CompareTag(BarrelTag))
            {
                object[] _params = new object[2];
                _params[0] = firePos.position; // 발사 위치
                _params[1] = hit.point; // 맞은 위치
                hit.collider.SendMessage("OnDamage", _params, SendMessageOptions.DontRequireReceiver);
            }
        }

        // 사운드 재생
        SoundManager.S_instance.PlaySound(transform.position, playerSound.fire[(int)curWeaponType]);

        UpdateBulletTxt();

        #region Projectile Movement Method
        /* 오브젝트 풀링이 아닐때
        Instantiate(bulletprfab, firePosTr.position, firePosTr.rotation); */
        /* 오브젝트 풀링일 때
        var _bullet = ObjectPoolingManager.poolingManager.GetBulletPool();
        if (_bullet != null)
        {
           _bullet.transform.position = firePosTr.position;
           _bullet.transform.rotation = firePosTr.rotation;
           _bullet.SetActive(true);
           if (_bullet)
               muzzleFlash.Play();
        } */
        #endregion
    }
}