using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PathCreation;
using PathCreation.Examples;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private GameObject startButton, tapButton, player, myCamera, obstaclePrefab;

    [SerializeField]
    private TMP_Text scoreText, highScoreText;

    [SerializeField]
    private PathCreator pathCreator;

    [SerializeField]
    private RoadMeshCreator currentRoad;

    [SerializeField]
    private Vector3 startPos;

    [SerializeField]
    private float offset, maxSpawnRadius, startDistance, speed, obstacleSpawnStartDistance;
    private float distanceTravelled, obstacleDistance;

    public bool isLeftPath, hasGameStarted, canMove;

    private int score, highScore;

    private BezierPath currentPath;

    public static GameManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentPath = pathCreator.bezierPath;
    }

    private void Start()
    {
        startButton.SetActive(true);
        tapButton.SetActive(false);
        distanceTravelled = startDistance;
        obstacleDistance = obstacleSpawnStartDistance;
        isLeftPath = false;
        hasGameStarted = false;
        canMove = true;

        score = PlayerPrefs.HasKey("Score") ? PlayerPrefs.GetInt("Score") : 0;
        highScore = PlayerPrefs.HasKey("HighScore") ? PlayerPrefs.GetInt("HighScore") : 0;
        scoreText.text = score.ToString();
        highScoreText.text = highScore.ToString();

        for (int i = 0; i < 10; i++)
        {
            CreatePath();
        }

        SetInitialState();
    }
    
    private void Update()
    {
        if (!hasGameStarted) return;

        distanceTravelled += speed * Time.deltaTime;

        Vector3 playerPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        Vector3 normal = pathCreator.path.GetNormalAtDistance(distanceTravelled);
        Vector3 direction = pathCreator.path.GetDirectionAtDistance(distanceTravelled);
        Vector3 localUp = Vector3.Cross(direction, normal).normalized;

        Vector3 camerPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        camerPos += (localUp * 4f - direction * 8f);
        myCamera.transform.position = camerPos;
        myCamera.transform.rotation = Quaternion.LookRotation(direction, localUp * 4f - direction * 8f);

        if (!canMove) return;

        playerPos += (localUp + normal * (isLeftPath ? -1 : 1));
        player.transform.position = playerPos;
        player.transform.rotation = Quaternion.LookRotation(direction, localUp);

        if(distanceTravelled + 100f > pathCreator.path.length)
        {
            for (int i = 0; i < 10; i++)
            {
                CreatePath();
            }
        }
    }

    void SetInitialState()
    {
        Vector3 playerPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        Vector3 normal = pathCreator.path.GetNormalAtDistance(distanceTravelled);
        Vector3 direction = pathCreator.path.GetDirectionAtDistance(distanceTravelled);
        Vector3 localUp = Vector3.Cross(direction, normal).normalized;

        playerPos += (localUp + normal);
        player.transform.position = playerPos;
        player.transform.rotation = Quaternion.LookRotation(direction,localUp);

        Vector3 camerPos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        camerPos += (localUp*4f - direction*8f);
        myCamera.transform.position = camerPos;
        myCamera.transform.rotation = Quaternion.LookRotation(direction, localUp * 4f - direction * 8f);
    }

    void CreatePath()
    {
        int num = pathCreator.path.NumPoints;
        Vector3 currentPos = pathCreator.path.GetPoint(num - 1);
        Vector3 normal = pathCreator.path.GetNormal(num - 1).normalized;
        float distance = pathCreator.path.GetClosestDistanceAlongPath(currentPos);
        Vector3 direction = pathCreator.path.GetDirectionAtDistance(distance).normalized;
        Vector3 localUp = Vector3.Cross(normal, direction);

        Vector3 tempOffset = Random.Range(-maxSpawnRadius, maxSpawnRadius) * normal +
            Random.Range(-maxSpawnRadius, maxSpawnRadius) * localUp;
        startPos += (direction * offset + tempOffset);
        currentPath.AddSegmentToEnd(startPos);
        currentRoad.textureTiling = distance / 3f;

        while(obstacleDistance < pathCreator.path.length)
        {
            currentPos = pathCreator.path.GetPointAtDistance(obstacleDistance);
            normal = pathCreator.path.GetNormalAtDistance(obstacleDistance).normalized;
            direction = pathCreator.path.GetDirectionAtDistance(obstacleDistance).normalized;
            localUp = Vector3.Cross(direction, normal).normalized;
            currentPos += (localUp + normal * (Random.Range(0, 2) == 0 ? 1 : -1));
            GameObject tempObstacle = Instantiate(obstaclePrefab);
            tempObstacle.name = obstacleDistance.ToString();
            tempObstacle.transform.SetPositionAndRotation(currentPos, Quaternion.LookRotation(direction, localUp));
            tempObstacle.GetComponentInChildren<TMP_Text>().text = (
                (int)((obstacleDistance - obstacleSpawnStartDistance)/(offset * 2f))).ToString();
            obstacleDistance += offset * 2f;
        }
    }

    public void StartGame()
    {
        startButton.SetActive(false);
        tapButton.SetActive(true);
        hasGameStarted = true;
    }

    public void SwitchLane()
    {
        isLeftPath = !isLeftPath;
        StartCoroutine(Turn());
    }

    IEnumerator Turn()
    {
        StopCoroutine(Turn());
        canMove = false;
        Vector3 movePos = pathCreator.path.GetPointAtDistance(distanceTravelled);
        Vector3 normal = pathCreator.path.GetNormalAtDistance(distanceTravelled);
        Vector3 direction = pathCreator.path.GetDirectionAtDistance(distanceTravelled);
        Vector3 localUp = Vector3.Cross(direction, normal).normalized;
        movePos += (localUp + normal*(isLeftPath ? -1 : 1));
        Vector3 playerPos = player.transform.position;
        while(Vector3.Magnitude(playerPos - movePos) > 0.1f)
        {
            movePos = pathCreator.path.GetPointAtDistance(distanceTravelled);
            normal = pathCreator.path.GetNormalAtDistance(distanceTravelled);
            direction = pathCreator.path.GetDirectionAtDistance(distanceTravelled);
            localUp = Vector3.Cross(direction, normal).normalized;
            movePos += (localUp + normal * (isLeftPath ? -1 : 1)); 
            playerPos = player.transform.position;
            playerPos = Vector3.Lerp(playerPos, movePos, speed * Time.deltaTime * 2f);
            player.transform.position = playerPos;
            yield return null;
        }
        canMove = true;
    }

    public void UpdateScore(int passedScore)
    {
        score = passedScore;
        if(score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        PlayerPrefs.SetInt("Score", score);
    }
}
