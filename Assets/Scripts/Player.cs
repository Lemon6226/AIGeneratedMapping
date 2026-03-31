using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    Animator animator;

    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float currentSpeed;

    public float mouseSensitivity = 2f;
    public Camera playerCamera;

    public StaminaControler staminaController;
    public HealthController healthController;

    private float mouseX;
    private float xInput;
    private float yInput;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        staminaController = GetComponent<StaminaControler>();
        healthController = GetComponent<HealthController>();

        currentSpeed = walkSpeed;

        // zamknutí kurzoru
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        GetMovementInput();
        HandleCameraLook();
        HandleSprinting();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void GetMovementInput()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");

        moveDirection = transform.right * xInput + transform.forward * yInput;

        // animace pohybu podle velikosti vstupu
        float movementAmount = new Vector2(xInput, yInput).magnitude;
        animator.SetFloat("Speed", movementAmount);
    }

    void MovePlayer()
    {
        rb.velocity = moveDirection * currentSpeed;
    }

    void HandleCameraLook()
    {
        // otáčení hráče podle pohybu myši
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleSprinting()
    {
        bool isMoving = moveDirection.magnitude > 0.1f;
        bool isHoldingShift = Input.GetKey(KeyCode.LeftShift);

        // sprint vyžaduje pohyb a držení shiftu
        if (!isHoldingShift || !isMoving)
        {
            staminaController.weAreSprinting = false;
            currentSpeed = walkSpeed;
            return;
        }

        if (staminaController.stamina > 0)
        {
            staminaController.weAreSprinting = true;
            staminaController.Sprinting();
            currentSpeed = runSpeed;
        }
        else
        {
            staminaController.weAreSprinting = false;
            currentSpeed = walkSpeed;
        }
    }

    public void SetRunSpeed(float speed)
    {
        currentSpeed = speed;
    }
}
