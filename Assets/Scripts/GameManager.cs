using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static protected GameManager _instance;
    static public GameManager instance { get { return _instance; } }

    public gState[] gStates;

    // Serialize the top state name
    [SerializeField] private string activeState;
    public gState topState { get { return gStatesStack.Count > 0 ? gStatesStack[gStatesStack.Count - 1] : null; } }

    protected List<gState> gStatesStack = new List<gState>();
    protected Dictionary<string, gState> gStatesDict = new Dictionary<string, gState>();

    protected virtual void OnEnable()
    {
        _instance = this;
        gStatesDict.Clear(); //state switcher by name

        if (gStates.Length == 0)
        {
            Debug.LogError("No states found in GameManager");
            return;
        }

        for (int i = 0; i < gStates.Length; i++)
        {
            gStates[i].gm = this;
            gStatesDict.Add(gStates[i].GetName(), gStates[i]);
        }

        gStatesStack.Clear();
        pushState(gStates[0].GetName());
    }

    // Push a new state onto the stack
    public void pushState(string name)
    {
        if (!tryFindState(name, out gState state)) return;

        gState previousState = topState;
        previousState?.Exit(state);
        state.Enter(previousState);

        gStatesStack.Add(state);
        activeState = state.GetName();
    }

    // Pop the current state off the stack, returning to the previous state
    public void popState()
    {
        if (gStatesStack.Count < 2) return;  // Guard clause

        gState currentState = topState;
        gState previousState = gStatesStack[gStatesStack.Count - 2];

        currentState.Exit(previousState);
        previousState.Enter(currentState);

        gStatesStack.RemoveAt(gStatesStack.Count - 1);
        activeState = topState?.GetName();
    }

    // Find a state by name in the dictionary
    public gState findState(string name)
    {
        return gStatesDict.TryGetValue(name, out var state) ? state : null;
    }

    // Switch the current state with a new state
    public void switchState(string newStateName)
    {
        if (!tryFindState(newStateName, out gState newState)) return;

        gState currentState = topState;
        currentState.Exit(newState);

        newState.Enter(currentState);

        gStatesStack[gStatesStack.Count - 1] = newState;
        activeState = topState?.GetName();
    }

    // Helper method for finding a state and logging errors if not found
    private bool tryFindState(string name, out gState state)
    {
        if (gStatesDict.TryGetValue(name, out state)) return true;

        Debug.LogError($"Can't find the state named {name}");
        return false;
    }

    // Update the top state on each frame
    protected virtual void Update()
    {
        if (gStatesStack.Count > 0)
        {
            gStatesStack[gStatesStack.Count - 1].Execute();
        }
    }
}

public abstract class gState : MonoBehaviour
{
    [HideInInspector]
    public GameManager gm;

    // State transition methods
    public abstract void Enter(gState from);
    public abstract void Execute(); // Tick function for the state 
    public abstract void Exit(gState to);
    public abstract string GetName();

    private Queue<(Vector3 endPos, Vector3 endRot, float duration)> transitionQueue = new Queue<(Vector3, Vector3, float)>();
    private bool isTransitioning = false;

    public void QueueCameraTransition(Vector3 endPosition, Vector3 endRotation, float duration)
    {
        transitionQueue.Enqueue((endPosition, endRotation, duration));
        if (!isTransitioning)
        {
            StartCoroutine(ProcessTransitionQueue());
        }
    }

    private IEnumerator ProcessTransitionQueue()
    {
        isTransitioning = true;

        while (transitionQueue.Count > 0)
        {
            var transition = transitionQueue.Dequeue();
            yield return StartCoroutine(CameraTransition(transition.endPos, transition.endRot, transition.duration));
        }

        isTransitioning = false;
    }

    private IEnumerator CameraTransition(Vector3 endPosition, Vector3 endRot, float transitionDuration)
    {
        Transform cameraTransform = Camera.main.transform;
        float elapsedTime = 0f;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;

            cameraTransform.position = Vector3.Lerp(startPos, endPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, Quaternion.Euler(endRot), t);

            yield return null;
        }

        cameraTransform.position = endPosition;
        cameraTransform.rotation = Quaternion.Euler(endRot);
    }
}
