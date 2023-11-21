using UnityEngine;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    private Transform cam;
    private WorldSc world;
    public GameObject inventory;
    public GameObject cursorSlot;
    public DragAndDropHandler myCursor;

    public float walkSpeed = 3;
    public float sprintSpeed = 6;
    public float jumpForce = 5;
    public float gravity = -9.82f;

    public float playerWidth = 0.5f;

    public int orientation;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform HighlightedBlock;
    public Transform PlaceBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8;

    private bool hasReleasedUIButton = true;

    public Toolbar toolbar;

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<WorldSc>();

        world.inUI = false;
    }

    private void FixedUpdate()
    {
        if (!world.inUI)
        {
            CalculateVelocity();
            if (jumpRequest)
            {
                Jump();
            }

            transform.Rotate(Vector3.up * mouseHorizontal);
            cam.Rotate(Vector3.right * -mouseVertical);
            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {
        if (Input.GetAxis("Menu") > 0 && hasReleasedUIButton)
        {
            if (world.inUI)
                myCursor.returnClicked();

            world.inUI = !world.inUI;
            inventory.SetActive(world.inUI);
            cursorSlot.SetActive(world.inUI);
            hasReleasedUIButton = false;
        }
        if (Input.GetAxis("Menu") == 0)
        {
            hasReleasedUIButton = true;
        }

        if (!world.inUI)
        {
            GetPlayerInputs();
            placeCursorBlocks();
        }

        Vector3 XZDirection = transform.forward;
        XZDirection.y = 0;
        if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
            orientation = 1;
        else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
            orientation = 5;
        else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
            orientation = 0;
        else
            orientation = 4;
    }

    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity()
    {
        //Affect vertical momentum with gravity
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }
        //If We'rw Sprinting, Use The Sprint Multiplier
        if (isSprinting)
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        }
        else
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        }

        //Apply vertical momentum (falling/jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
        {
            velocity.z = 0;
        }
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        if (velocity.y < 0)
        {
            velocity.y = checkDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = checkupSpeed(velocity.y);
        }
    }

    private void GetPlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X") * world.settings.mouseSensitivity;
        mouseVertical = Input.GetAxis("Mouse Y") * world.settings.mouseSensitivity;

        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
        }
        if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }

        if (HighlightedBlock.gameObject.activeSelf)
        {
            //Destroy Block.
            if (Input.GetMouseButtonDown(0))
                world.getChunkFromVector3(HighlightedBlock.position).EditVoxel(HighlightedBlock.position, 0);

            //Place Block.
            if (Input.GetMouseButtonDown(1))
            {
                if (toolbar.slots[toolbar.slotIndex].HasItem)
                {
                    world.getChunkFromVector3(PlaceBlock.position).EditVoxel(PlaceBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                    toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                }
            }
        }
    }

    private void placeCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos))
            {
                HighlightedBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                PlaceBlock.position = lastPos;

                HighlightedBlock.gameObject.SetActive(true);
                PlaceBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }

        HighlightedBlock.gameObject.SetActive(false);
        PlaceBlock.gameObject.SetActive(false);
    }

    private float checkDownSpeed(float downSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
           )
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float checkupSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2 + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2 + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2 + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2 + upSpeed, transform.position.z + playerWidth))
           )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z + playerWidth))
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z - playerWidth))
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1, transform.position.z))
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1, transform.position.z))
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
