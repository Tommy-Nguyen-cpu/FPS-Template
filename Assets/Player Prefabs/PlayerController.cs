using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    #region Player Stats Fields

    public Slider playerHealthBar;
    public Gradient gradient;
    public Image fill;

    public float playerSpeed = 100f;
    public float jumpHeight = 10f;

    #endregion

    public CharacterController controller;

    GameObject weapon;

    public Canvas pauseMenu;
    public static bool isPaused = false;


    #region Rotation Variables

    /// <summary>
    /// How much the player rotates based on mouse movement.
    /// </summary>
    public float mouseSensitivity = 100f;

    private float xRotation = 0.0f;
    #endregion

    #region Physics Fields

    Vector3 velocity;
    public float gravity = -9.81f;

    private bool isGrounded = true;
    public LayerMask groundMask;
    public Transform GroundCheck;
    public float groundDistance = 0.4f;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        SetMaximumHealth();

        // TODO: Probably not a good way of retrieving the weapon.
        weapon = gameObject.transform.GetChild(1).gameObject;
        weapon.GetComponent<GunController>().Player = gameObject;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isPaused)
        {
            // If the player is not pausing the game, lock the mouse to the center.
            Cursor.lockState = CursorLockMode.Locked;

            #region Weapons Code
            // TODO: We can generalize this command to account for different numbers of weapons.
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                weapon.SetActive(!weapon.activeInHierarchy);
            }

            if (weapon.activeInHierarchy && Input.GetKeyDown(KeyCode.Mouse0))
            {
                weapon.GetComponent<GunController>().ShootBullet();
            }
            #endregion


            GravitationalForce();
            PlayerMovement();
            AdjustPlayerRotation();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
            pauseMenu.gameObject.SetActive(true);
            isPaused = true;
        }
    }

    /// <summary>
    /// Method responsible for allowing player movement.
    /// </summary>
    private void PlayerMovement()
    {
        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");

        // Get a vector telling us the direction the user is moving in.
        Vector3 move = transform.forward * z + transform.right * x;

        // If the player is grounded and they click a "jump" button, using the physics equation to increase players y.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            move.y += Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Move the player in the direction, with a specific speed, and independent of frame rates.
        controller.Move(move * playerSpeed * Time.deltaTime);
    }


    /// <summary>
    /// Method responsible for player rotation.
    /// </summary>
    private void AdjustPlayerRotation()
    {
        // Gets the x and y mouse coordinates, and ensures that rotation is frame rate independent.
        float MouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float MouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= MouseY;

        // Clamps the up and down rotation to 90 degrees.
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotation in the left and right direction.
        float rotationY = transform.localEulerAngles.y + MouseX;

        transform.localRotation = Quaternion.Euler(xRotation, rotationY, 0f);
    }

    private void GravitationalForce()
    {
        // Checks to see if the player hits the ground.
        isGrounded = Physics.CheckSphere(GroundCheck.position, groundDistance, groundMask);
        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Velocity increases as an object falls (free fall).
        velocity.y += gravity * Time.deltaTime;

        // Change in y is found via: 1/2*g * t^2. Where "g" is the gravitational constant and "t" is time.
        controller.Move(velocity * Time.deltaTime);
    }


    private void SetMaximumHealth()
    {
        SetHealth(1f);
    }

    private void SetHealth(float adjustAmount)
    {
        playerHealthBar.value += adjustAmount;

        fill.color = gradient.Evaluate(playerHealthBar.normalizedValue);

        if(playerHealthBar.value == 0)
        {
            // TODO: If the players health reaches 0, direct to "GAME OVER" screen;
        }
    }


    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.name.Contains("Enemy"))
        {
            float damage = collision.gameObject.GetComponent<EnemyController>().damageDealt;
            SetHealth(-1 * damage);
        }
    }
}
