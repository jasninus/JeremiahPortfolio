using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CornMazeClient : BaseClient
{
    [SerializeField] private AudioClip[] SFX;

    private AudioSource audioSource;

    private Text accelerationText;

    private bool centered;

    private Transform playerTransform;

    [SerializeField] private float centeredSensitivity, travelTime, movementThreshold = 0.5f;

    [SerializeField] private GameObject dummyPrefab;

    private Vector3Int gridPos;

    private Animator playerAnim;

    private Rigidbody dummyRB;

    public bool isMoving;

    // Order is sorted so higher index is rotating left
    private readonly Vector2Int[] orthogonalDirections =
    {
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
    };

    private byte currentDirectionIdx;

    public void AwakeSetup()
    {
        gridPos = new Vector3Int(0, 0, 0);
        //accelerationText = GameObject.FindWithTag("Text").GetComponent<Text>();
        audioSource = GameObject.FindWithTag("AudioPlayer").GetComponent<AudioSource>();
        CmdSpawnDummy(GameObject.FindWithTag("CornMazeDummySpawnpoint").transform.position);
        StartCoroutine(FindDummy());

        if (owner.isServer)
        {
            GameObject canvas = GameObject.Find("Canvas");
            canvas.FindObject("ForwardButton").SetActive(false);
            canvas.FindObject("ForwardButton2").SetActive(false);
        }
    }

    private IEnumerator FindDummy()
    {
        yield return new WaitUntil(() => GameObject.FindWithTag("CornMazeDummy"));

        playerTransform = GameObject.FindWithTag("CornMazeDummy").transform;
        playerAnim = playerTransform.gameObject.GetComponent<Animator>();
        dummyRB = playerTransform.gameObject.GetComponent<Rigidbody>();
    }

    [Command]
    private void CmdSpawnDummy(Vector3 spawnPos)
    {
        if (!GameObject.FindWithTag("CornMazeDummy"))
        {
            GameObject g = Instantiate(dummyPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(g);
        }
    }

    private void Update()
    {
        if (playerTransform)
        {
            dummyRB.angularVelocity = dummyRB.velocity = Vector3.zero;
        }

        if (Settings.isUsingAccelerometer)
        {
            UpdateAccelerometerMovement();
        }
    }

    private void UpdateAccelerometerMovement()
    {
        if (centered && !isMoving)
        {
            // Check for tilts
            if (Input.acceleration.z < -movementThreshold)
            {
                CmdMoveForward();
                centered = false;
            }
            else if (Input.acceleration.x < -movementThreshold)
            {
                CmdRotateY(-1);
                centered = false;
            }
            else if (Input.acceleration.x > movementThreshold)
            {
                CmdRotateY(1);
                centered = false;
            }
        }
        else
        {
            centered = PhoneIsUpright();
        }
    }

    private bool PhoneIsUpright()
    {
        return Mathf.Abs(Input.acceleration.x) < centeredSensitivity &&
               (Mathf.Abs(Input.acceleration.y) - 1) < centeredSensitivity &&
               Mathf.Abs(Input.acceleration.z) < centeredSensitivity;
    }

    public void Forward()
    {
        // Send forward command only from client => blind person
        if (!owner.isServer)
        {
            CmdMoveForward();
        }
    }

    [Command]
    private void CmdMoveForward()
    {
        Vector2Int desiredLocation = new Vector2Int(gridPos.x + orthogonalDirections[currentDirectionIdx].x, gridPos.y + orthogonalDirections[currentDirectionIdx].y);

        if (LocationIsWithinBounds(desiredLocation))
        {
            RpcMoveForward(new Vector3((
                gridPos.x + orthogonalDirections[currentDirectionIdx].x) * TractorPartManager.MazeCellSize + TractorPartManager.MazeStartingPos.x,
                playerTransform.position.y,
                (gridPos.y + orthogonalDirections[currentDirectionIdx].y) * TractorPartManager.MazeCellSize + TractorPartManager.MazeStartingPos.z)
            );
        }
    }

    private static bool LocationIsWithinBounds(Vector2Int desiredLocation)
    {
        return desiredLocation.x >= 0 && desiredLocation.y >= 0 &&
               desiredLocation.x != TractorPartManager.MazeObjects.GetLength(0) && desiredLocation.y != TractorPartManager.MazeObjects.GetLength(0) &&
               TractorPartManager.MazeObjects[desiredLocation.x, desiredLocation.y] != TractorPartManager.MazeObject.Obstacle;
    }

    // All movement methods need to be called on the server in order for the syncing to work properly
    [ClientRpc]
    private void RpcMoveForward(Vector3 targetPos)
    {
        StartCoroutine(MoveToPosition(targetPos));
        gridPos += new Vector3Int(orthogonalDirections[currentDirectionIdx].x, orthogonalDirections[currentDirectionIdx].y, 0);
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        isMoving = true;
        playerAnim.SetBool("IsMoving", true);
        float startTime = Time.time;
        Vector3 startPos = playerTransform.position;
        while ((playerTransform.position - targetPos).magnitude > 0.01f)
        {
            playerTransform.position = Vector3.Lerp(startPos, targetPos, (Time.time - startTime) / travelTime);
            yield return new WaitForFixedUpdate();
        }

        playerTransform.position = targetPos;
        playerAnim.SetBool("IsMoving", false);
        isMoving = false;
    }

    public void RotateY(sbyte rotation)
    {
        // Send rotate command only from client => blind person
        if (!owner.isServer)
        {
            CmdRotateY(rotation);
        }
        else
        {
            CmdPlaySound(rotation);
        }
    }

    [Command] private void CmdRotateY(sbyte rotation) => RpcRotateY(rotation);

    [ClientRpc]
    private void RpcRotateY(sbyte rotation)
    {
        if (currentDirectionIdx + rotation < 0)
        {
            currentDirectionIdx = (byte)(orthogonalDirections.Length - (currentDirectionIdx - rotation) % orthogonalDirections.Length);
        }
        else
        {
            currentDirectionIdx = (byte)((currentDirectionIdx + rotation) % orthogonalDirections.Length);
        }

        playerTransform.Rotate(0, rotation < 0 ? 90 : -90, 0);
    }

    [Command]
    public void CmdPlaySound(sbyte num)
    {
        RpcPlaySound(num);
    }

    [ClientRpc]
    public void RpcPlaySound(sbyte num)
    {
        // Play sound only on server => blind
        if (!owner.isServer)
        {
            audioSource.panStereo = -num;
            audioSource.clip = SFX[Random.Range(0, SFX.Length)];
            audioSource.Play();
        }
    }
}