using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointData
{
    public GameObject pointObj;
    public bool isExist;
    public (int, int)? next;

    public Vector2 Position
    {
        get => pointObj.transform.position;
    }

    public PointData() { }

    public PointData(PointData pd)
    {
        if (pd == null)
            return;

        pointObj = pd.pointObj;
        isExist = pd.isExist;

        next = pd.next.GetValueOrDefault();
    }
}

public class GameState
{
    const int MaxN = 30;
    const int MaxM = 30;

    public (int, int) currentPoint;
    public List<List<PointData>> Field;

    public GameState()
    {
        currentPoint = (0, 0);
        Field = new List<List<PointData>>();
    }

    public GameState(GameState gs)
    {
        Field = new List<List<PointData>>();
        foreach (var currentLine in gs.Field)
        {
            List<PointData> bufLine = new List<PointData>();
            foreach (var currentElem in currentLine)
                bufLine.Add(new PointData(currentElem));
            Field.Add(bufLine);
        }

        currentPoint = (gs.currentPoint.Item1, gs.currentPoint.Item2);
    }

    public List<(int, int)> GetPossiblePoints()
    {
        List<(int, int)> possiblePoints = new List<(int, int)>();
        int N = Field.Count;
        int M = Field[0].Count;

        for (int i = currentPoint.Item1 - 1; i <= currentPoint.Item1 + 1; i++)
            for (int j = currentPoint.Item2 - 1; j <= currentPoint.Item2 + 1; j++)
            {
                if ((i == 0) || (i == N - 1) || (j == 0) || (j == M - 1))
                    continue;

                int i_ = i - currentPoint.Item1;
                int j_ = j - currentPoint.Item2;

                if (Field[i][j].isExist == false)
                {
                    if ((i_ == 0) || (j_ == 0))
                        possiblePoints.Add((i, j));
                    else if ((Field[currentPoint.Item1 + i_][currentPoint.Item2].next != (currentPoint.Item1, currentPoint.Item2 + j_))
                        && (Field[currentPoint.Item1][currentPoint.Item2 + j_].next != (currentPoint.Item1 + i_, currentPoint.Item2)))
                        possiblePoints.Add((i, j));
                }
            }

        return possiblePoints;
    }

    public void MakeMove((int, int) newPoint)
    {
        Field[newPoint.Item1][newPoint.Item2].isExist = true;
        Field[currentPoint.Item1][currentPoint.Item2].next = (newPoint.Item1, newPoint.Item2);
        currentPoint = (newPoint.Item1, newPoint.Item2);
    }
}

public class GameManager : MonoBehaviour
{
    public GameState gameState = new GameState();
    List<(int, int)> possiblePoints = new List<(int, int)>();
    float ratio = 1f / 20;
    float distance = 0.6f;

    public GameObject redPoint;
    public GameObject bluePoint;
    public GameObject grayPoint;
    GameObject ghostPoint;
    AI bot;
    LineRenderer lineRenderer;

    public int N;
    public int M;

    bool isPlayerOne = true;
    bool vsAI = true;
    bool moveWasMade = false;

    bool gameIsOver = false;
    bool moveState = false;

    public GameObject fade;

    public IEnumerator LoadSceneAfterDelay(string sceneName, float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public bool GameIsOver
    {
        get => gameIsOver ? true : possiblePoints.Count == 0;
        set => gameIsOver = value;
    }

    public bool IsPause { get; set; } = false;

    public PointData CurrentPointData
    {
        get => gameState.Field[gameState.currentPoint.Item1][gameState.currentPoint.Item2];
        set => gameState.Field[gameState.currentPoint.Item1][gameState.currentPoint.Item2] = value;
    }

    void Start()
    {
        bot = gameObject.GetComponent<AI>();
        float screenHeight = 2 * Camera.main.orthographicSize;
        float screenWidth = 2 * Camera.main.orthographicSize * (1080f / 1920f);

        float fieldHeight = distance * (N + 1);
        float fieldWidth = distance * (M + 1);

        if ((fieldHeight > (1 - ratio) * screenHeight) || (fieldWidth > screenWidth))
            distance = Math.Min((1 - ratio) * screenHeight / (N + 1), screenWidth / (M + 1));

        float leftBorder = distance - (distance * (M + 1)) / 2.0f;
        float topBorder = (distance * (N + 1)) / 2.0f - distance - ratio * screenHeight;

        float cx = leftBorder;
        float cy = topBorder;

        
        for (int i = 0; i < N + 2; i++)
        {
            List<PointData> bufLine = new List<PointData>();
            for (int j = 0; j < M + 2; j++)
            {
                PointData bufPoint = new PointData();
                bufLine.Add(bufPoint);
                if ((i == 0) || (j == 0) || (i == N + 1) || (j == M + 1))
                    continue;
                possiblePoints.Add((i, j));
                bufPoint.pointObj = Instantiate(grayPoint, new Vector2(cx, cy), Quaternion.identity);

                cx += distance;
            }
            gameState.Field.Add(bufLine);
            cx = leftBorder;
            if (i != 0)
                cy -= distance;
        }
    }

    public void RecreateGame()
    {
        isPlayerOne = true;
        GameIsOver = false;
        gameState.currentPoint = (0, 0);
        possiblePoints.Clear();
        Destroy(ghostPoint);

        if (vsAI)
            bot.ResetTree();

        for (int i = 1; i < N + 1; i++)
        {
            for (int j = 1; j < M + 1; j++)
            {
                possiblePoints.Add((i, j));
                PointData currentElem = gameState.Field[i][j];
                var pos = gameState.Field[i][j].Position;
                Destroy(currentElem.pointObj);
                currentElem.pointObj = Instantiate(grayPoint, pos, Quaternion.identity);
                currentElem.isExist = false;
                currentElem.next = null;
            }
        }
    }

    void Update()
    {
        if (!GameIsOver)
        {
            (int, int) move = (0, 0);
            moveWasMade = false;

            lineRenderer = CurrentPointData.pointObj?.GetComponent<LineRenderer>();
            

            if ((vsAI) && (!isPlayerOne))
            {
                if (lineRenderer != null)
                    lineRenderer.SetPosition(0, CurrentPointData.Position);

                isPlayerOne = !isPlayerOne;
                move = bot.ChooseMove();
                moveWasMade = true;
            }
            else if (Input.touchCount > 0)
            {
                if (lineRenderer != null)
                    lineRenderer.SetPosition(0, CurrentPointData.Position);

                Touch touch = Input.GetTouch(0);
                if (!fade.activeInHierarchy)
                {
                    if (IsPause && touch.phase == TouchPhase.Ended)
                        IsPause = false;
                    else if (!IsPause)
                    {
                        Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                        if (touch.phase == TouchPhase.Ended)
                        {
                            foreach (var possiblePoint in possiblePoints)
                                if (Vector2.Distance(touchPosition, gameState.Field[possiblePoint.Item1][possiblePoint.Item2].Position) < distance / 2)
                                {
                                    isPlayerOne = !isPlayerOne;
                                    move = possiblePoint;
                                    moveWasMade = true;
                                    break;
                                }

                            if (CurrentPointData.next == null && moveState)
                            {
                                lineRenderer.SetPosition(1, CurrentPointData.Position);
                                moveState = false;
                            }
                        }
                        else if (touch.phase == TouchPhase.Began)
                        {
                            if (gameState.currentPoint != (0, 0) && Vector2.Distance(touchPosition, CurrentPointData.Position) < distance / 2)
                                moveState = true;
                            if (lineRenderer != null)
                                lineRenderer.SetPosition(1, CurrentPointData.Position);
                        }
                        else if (touch.phase == TouchPhase.Moved && moveState)
                        {
                            if (lineRenderer != null)
                                lineRenderer.SetPosition(1, touchPosition);

                            foreach (var possiblePoint in possiblePoints)
                            {
                                if (Vector2.Distance(touchPosition, gameState.Field[possiblePoint.Item1][possiblePoint.Item2].Position) < distance / 2)
                                {
                                    Destroy(ghostPoint);
                                    ghostPoint = Instantiate(isPlayerOne ? redPoint : bluePoint, gameState.Field[possiblePoint.Item1][possiblePoint.Item2].Position, Quaternion.identity);
                                    var anim = ghostPoint.GetComponent<Animator>();
                                    if (anim != null)
                                        anim.enabled = false;
                                    lineRenderer.SetPosition(1, ghostPoint.transform.position);
                                    return;
                                }
                            }
                            Destroy(ghostPoint);
                        }
                    }
                }
            }
            if (moveWasMade)
            {
                //Debug.Log("current move" + move);
                if (vsAI)
                    bot.UpdateTree(move);

                MakeVisualEffects(move);
                gameState.MakeMove(move);
                possiblePoints = gameState.GetPossiblePoints();
            }
        }
        else
            Debug.Log(isPlayerOne ? "Player Two wins" : "Player One wins");
    }

    void MakeVisualEffects((int, int) point_)
    {
        GameObject point = Instantiate(!isPlayerOne ? redPoint : bluePoint, gameState.Field[point_.Item1][point_.Item2].Position, Quaternion.identity);

        Destroy(gameState.Field[point_.Item1][point_.Item2].pointObj);
        gameState.Field[point_.Item1][point_.Item2].pointObj = point;

        if (lineRenderer != null)
            lineRenderer.SetPosition(1, gameState.Field[point_.Item1][point_.Item2].Position);

        SetCurrentAnimation(gameState.currentPoint, false);
        SetCurrentAnimation(point_, true);
    }

    void SetCurrentAnimation((int, int) currentPoint, bool value)
    {
        var animator = gameState.Field[currentPoint.Item1][currentPoint.Item2]?.pointObj?.GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("is_current", value);
    }
}
