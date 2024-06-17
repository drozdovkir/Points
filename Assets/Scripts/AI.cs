using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyNode
{
    public GameState gameState;
    public List<(int, int)> possiblePoints;
    public List<MyNode> children;

    public float rate;
    public int counter;
    public int depth;
    public bool isMyMove;

    public MyNode()
    {
        gameState = new GameState();
        possiblePoints = new List<(int, int)>();
        children = new List<MyNode>();
    }

    public MyNode(GameState gs, float r, int c, int d, bool ismymove)
    {
        gameState = new GameState(gs);
        possiblePoints = new List<(int, int)>();
        children = new List<MyNode>();

        rate = r;
        counter = c;
        depth = d;
        isMyMove = ismymove;
    }
    
    public MyNode GoToNode((int, int) nextPoint)
    {
        foreach (var child in children)
            if (child.gameState.Field[gameState.currentPoint.Item1][gameState.currentPoint.Item2].next == nextPoint)
            {
                child.counter = 0;
                child.rate = child.isMyMove ? 0f : 1f;
                child.depth = depth + 1;
                return child;
            }

        GameState newGameState = new GameState(gameState);
        newGameState.MakeMove(nextPoint);

        MyNode newNode = new MyNode(newGameState, (isMyMove? 1f : 0f), 0, depth + 1, !isMyMove);
        children.Add(newNode);

        return newNode;
    }

    public (int, int) ChooseBestMove()
    {
        float maxRate = -1f;
        (int, int) maxRateMove = (0,0);

        foreach (var child in children)
        {
            if (child.rate == maxRate)
            {
                System.Random rng = new System.Random();
                if (rng.Next(2) == 1)
                    maxRateMove = ((int, int))child.gameState.Field[gameState.currentPoint.Item1][gameState.currentPoint.Item2].next;
            }

            if (child.rate > maxRate)
            {
                maxRate = child.rate;
                maxRateMove = ((int, int))child.gameState.Field[gameState.currentPoint.Item1][gameState.currentPoint.Item2].next;
            }
        }
        
        return maxRateMove;
    }

    public float GetHeuristic()
    {
        GameState bufGameState = new GameState(gameState);
        Queue<(int, int)> points = new Queue<(int, int)>();
        int pointsCounter = -1;

        points.Enqueue(bufGameState.currentPoint);
        while (points.Count > 0)
        {
            bufGameState.currentPoint = points.Peek();
            possiblePoints = bufGameState.GetPossiblePoints();
            foreach (var possiblePoint in possiblePoints)
            {
                bufGameState.Field[possiblePoint.Item1][possiblePoint.Item2].isExist = true;
                points.Enqueue(possiblePoint);
            }

            points.Dequeue();
            pointsCounter++;
        }

        float correction = pointsCounter == 0f ? 1f : 1f / pointsCounter;
        bool res = isMyMove ^ pointsCounter % 2 == 1;
        if (res)
            return Math.Max(0f, 0.5f - correction);
        else
            return Math.Min(1f, 0.5f + correction);
    }
}

public class AI : MonoBehaviour
{
    public int maxDepth = 2;

    MyNode root;
    GameManager gameManager;

    void Start()
    {
        gameManager = gameObject.GetComponent<GameManager>();
    }

    MyNode SearchTree(MyNode firstNode)
    {
        Stack<MyNode> stack = new Stack<MyNode>();
        MyNode currentNode = firstNode;

        stack.Push(firstNode);

        while (stack.Count > 0)
        {
            currentNode = stack.Peek();

            if (currentNode.depth == maxDepth)
            {
                currentNode.rate = currentNode.GetHeuristic();
                stack.Pop();
                continue;
            }

            if (currentNode.counter == 0)
                currentNode.possiblePoints = currentNode.gameState.GetPossiblePoints();

            switch (currentNode.possiblePoints.Count)
            {
                case 0:
                    currentNode.rate = currentNode.isMyMove ? 0f : 1f;
                    stack.Pop();
                    break;
                case 1:
                    GameState bufGameState = new GameState(currentNode.gameState);
                    List<(int, int)> pp = new List<(int, int)>();
                    int moveCounter = 0;

                    pp.Add(currentNode.possiblePoints[0]);
                    while (pp.Count == 1)
                    {
                        bufGameState.MakeMove(pp[0]);
                        pp = bufGameState.GetPossiblePoints();
                        moveCounter++;
                    }
                    
                    MyNode newNode;
                    bool res = currentNode.isMyMove ^ moveCounter % 2 == 1;
                    newNode = new MyNode(bufGameState, (res ? 0f : 1f), 0, currentNode.depth, moveCounter % 2 == 0? currentNode.isMyMove : !currentNode.isMyMove);

                    stack.Pop();
                    if (stack.Count > 0)
                    {
                        currentNode = stack.Peek();
                        currentNode.children[currentNode.counter - 1] = newNode;
                    }
                    stack.Push(newNode);

                    break;
                default:
                    if (currentNode.counter != 0)
                        if (currentNode.isMyMove)
                            currentNode.rate = Math.Max(currentNode.rate, currentNode.children[currentNode.counter - 1].rate);
                        else
                            currentNode.rate = Math.Min(currentNode.rate, currentNode.children[currentNode.counter - 1].rate);

                    if (currentNode.counter >= currentNode.possiblePoints.Count)
                    {
                        stack.Pop();
                        continue;
                    }

                    MyNode nextNode = currentNode.GoToNode(currentNode.possiblePoints[currentNode.counter]);
                    stack.Push(nextNode);

                    currentNode.counter++;
                    break;
            }
        }

        return currentNode;
    }

    public (int, int) ChooseMove()
    {
        if (root == null)
            root = new MyNode(gameManager.gameState, 0f, 0, 0, true);

        if (root.gameState.currentPoint != gameManager.gameState.currentPoint)
            return ((int, int))root.gameState.Field[gameManager.gameState.currentPoint.Item1][gameManager.gameState.currentPoint.Item2].next;

        MyNode newRoot = SearchTree(root);
        if (newRoot != root)
        {
            root = newRoot;
            return ((int, int))newRoot.gameState.Field[gameManager.gameState.currentPoint.Item1][gameManager.gameState.currentPoint.Item2].next;
        }
        return root.ChooseBestMove();
    }

    public void UpdateTree((int, int) move)
    {
        if (root == null)
            return;

        if (root.gameState.Field[gameManager.gameState.currentPoint.Item1][gameManager.gameState.currentPoint.Item2].next == (0, 0))
        {
            root.depth--;
            root = root.GoToNode(move);
        }
     
    }

    public void ResetTree()
    {
        root = null;
    }
}
