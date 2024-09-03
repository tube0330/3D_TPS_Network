using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class PlayerAnimation
{
    public AnimationClip idle;
    public AnimationClip runForward;
    public AnimationClip runBackward;
    public AnimationClip runLeft;
    public AnimationClip runRight;
    public AnimationClip Sprint;
}
public class Player : MonoBehaviour
{
    public PlayerAnimation playerAnimation;

    [SerializeField] Transform tr;
    [SerializeField] Animation ani;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip clip;
    [SerializeField] CapsuleCollider col;

    [SerializeField] Transform FirePos;
    [SerializeField] GameObject Bullet;

    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotSpeed = 90f;

    [Header("PlayerInput")]
    public PlayerInput playerInput;
    public InputActionMap playerMap;
    public InputAction moveAction;
    public InputAction sprintAction;
    Vector3 moveDir = Vector3.zero;

    float h = 0f, v = 0f, r = 0f;

    void OnEnable()
    {
        GameManager.OnItemChange += UpdateSetUp;
    }

    void UpdateSetUp()
    {
        moveSpeed = GameManager.G_Instance.gameData.speed;
    }

    void Start()
    {
        tr = transform;
        ani = GetComponent<Animation>();
        moveSpeed = GameManager.G_Instance.gameData.speed;
        col = GetComponent<CapsuleCollider>();

        ani.Play(playerAnimation.idle.name);
        /*ani.clip = playerAnimation.idle;
        ani.Play(playerAnimation.idle.name);*/

        FirePos = GameObject.Find("FirePos").GetComponent<Transform>();

        playerInput = GetComponent<PlayerInput>();
        playerMap = playerInput.actions.FindActionMap("Player");
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
    }

    void Update()
    {
        /* h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
        r = Input.GetAxis("Mouse X");

        Vector3 moveDir = (h * Vector3.right) + (v * Vector3.forward);
        tr.Translate(moveDir.normalized * moveSpeed * Time.deltaTime, Space.Self);
        tr.Rotate(Vector3.up * r * Time.deltaTime * rotSpeed);

        MoveAni();
        Sprint(); */

        Vector2 dir = moveAction.ReadValue<Vector2>();
        moveDir = new Vector3(dir.x, 0, dir.y).normalized; // 방향 벡터 정규화

        if (moveDir != Vector3.zero)
        {
            tr.Translate(moveDir * moveSpeed * Time.deltaTime, Space.Self);
            tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime);
        }
    
        MoveAni();
        Sprint();
    }

    private void Sprint()
    {
        bool isSprinting = sprintAction.ReadValue<float>() > 0; // 스프린트 액션의 상태를 읽어옴

        if (isSprinting && moveDir != Vector3.zero)
        {
            moveSpeed = 10f;
            ani.CrossFade(playerAnimation.Sprint.name, 3.0f);
        }

        else
            moveSpeed = 5f;
    }

    private void MoveAni()
    {
        if (moveDir.z > 0.1f)
            ani.CrossFade(playerAnimation.runForward.name, 0.3f);
        else if (moveDir.z < -0.1f)
            ani.CrossFade(playerAnimation.runBackward.name, 0.3f);

        if (moveDir.x > 0.1f)
            ani.CrossFade(playerAnimation.runRight.name, 0.3f);
        else if (moveDir.x < -0.1f)
            ani.CrossFade(playerAnimation.runLeft.name, 0.3f);

        if (moveDir == Vector3.zero)
            ani.CrossFade(playerAnimation.idle.name, 0.3f);
    }

}
