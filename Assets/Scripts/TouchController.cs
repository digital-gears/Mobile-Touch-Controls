using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TouchController : MonoBehaviour
{
    public float TouchTolerance = 0.05f;
    public float RotationSpeed = 1f;
    public float ZoomSpeed = 1f;

    private Vector2 lastTouch0;
    private Vector2 lastTouch1;
    private Vector2 newTouch0;
    private Vector2 newTouch1;

    private bool StaticTouch=false;
    private bool moving = false;
    private GameManager _gameManager;

    private enum TouchState
    {
        Idle, //No touch or more than two touches +++Goes to: Select, Order, Swipe, TwoFingerIdle+++
        Swipe,//One Touch and drag over scrollable plane +++Goes to: Idle, TwoFingerIdle+++
        TwoFingerIdle, //Two still touches +++Goes to : Rotate, Zoom, Idle+++
        Rotate, //One fixed finger and another one in motion
        Zoom //Pinch closing or opening two fingers    
    }

    TouchState CurrentState = TouchState.Idle;

    private void Start()
    {
        _gameManager = GameManager.instance;
        StartCoroutine("FSM");
    }

    IEnumerator FSM() //This is the state machine
    {
        while (true)
        {
            yield return StartCoroutine(CurrentState.ToString());
        }
    }

    private void ChangeState(TouchState NextState) //This is the changer of states
    {
        Debug.Log("Changed from " + CurrentState.ToString() + " state to " + NextState.ToString() + " state.");
        CurrentState = NextState;
    }


    IEnumerator Idle() //This loops in the Idle state
    {
        while(CurrentState == TouchState.Idle)
        {
            switch (Input.touchCount)
            {
                case 1:
                    lastTouch0 = newTouch0;
                    if (Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        newTouch0 = Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position);
                        StaticTouch = true;
                    }
                    if (Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        if (!NearTouches(Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position), lastTouch0))
                        {
                            StaticTouch = false;
                            ChangeState(TouchState.Swipe);
                        }                        
                    }
                    if (Input.GetTouch(0).phase == TouchPhase.Ended)
                    {
                        if (NearTouches(Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position), lastTouch0)||StaticTouch)
                        {
                            OrderOrSelection();
                        }
                        StaticTouch = false;
                    }
                    if (Input.GetTouch(0).phase == TouchPhase.Canceled)
                    {
                        newTouch0 = Vector2.zero;
                        lastTouch0 = Vector2.zero;
                        StaticTouch = false;
                    }
                    break;
                case 2:
                    ChangeState(TouchState.TwoFingerIdle);
                    break;
                default:
                    break;
            }
            yield return 0; 
        }
    }



    IEnumerator Swipe()
    {
        switch (Input.touchCount)
        {
            case 1:
                switch (Input.GetTouch(0).phase)
                {
                    case TouchPhase.Moved:
                        newTouch0 = Input.GetTouch(0).position;
                        if (moving)
                        {
                            RaycastHit hit;
                            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                            if (Physics.Raycast(ray, out hit))
                            {

                            }
                        }
                        lastTouch0 = newTouch0;
                        moving = true;
                        break;
                    case TouchPhase.Stationary:
                        break;
                    default:
                        ChangeState(TouchState.Idle);
                        moving = false;
                        break;
                }
                break;
            case 2:
                ChangeState(TouchState.TwoFingerIdle);
                moving = false;
                break;
            default:
                ChangeState(TouchState.Idle);
                moving = false;
                break;
        }
        yield return 0;     
    }

    IEnumerator TwoFingerIdle()
    {
        while (CurrentState == TouchState.TwoFingerIdle)
        {
            Debug.Log("Two Finger Al pedo");
            yield return 0;
        }
    }
    IEnumerator Rotate()
    {
        while (CurrentState == TouchState.Rotate)
        {
            Debug.Log("Rotation");
            yield return 0;
        }
    }
    IEnumerator Zoom()
    {
        while (CurrentState == TouchState.Zoom)
        {
            Debug.Log("Zoom");
            yield return 0;
        }
    }

    private void OrderOrSelection()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
        if (Physics.Raycast(ray, out hit))
        {
            Transform objectHit = hit.transform;
            if (objectHit.tag == "Player") //What happens if I touch a player Character
            {
                Character CharacterHit = objectHit.GetComponent<Character>();
                if (CharacterHit.selected)
                {
                    _gameManager.UnselectAll();
                }
                else
                {
                    _gameManager.UnselectAll();
                    CharacterHit.Select(true);
                }
            }
            else
            {
                if (_gameManager.SelectedCharacter != 0)
                {
                    if (objectHit.tag == "Enemy")
                    {
                        //What happens if I touch an Enemy
                    }
                    else
                    {
                        if (objectHit.tag == "Interactable")
                        {
                            //What happens if I touch an Interactable Object
                        }
                        else
                        {
                            WalkTo(hit.point);
                        }
                    }
                }
            }
        }
    }
    private void WalkTo(Vector3 target)
    {
        target = new Vector3(target.x, 1f, target.z);
        _gameManager.Characters[_gameManager.SelectedCharacter - 1].MoveTo(target);
    }

    private bool NearTouches(Vector2 aux1,Vector2 aux2)
    {
        return (Vector2.Distance(aux1, aux2) < TouchTolerance);
    }
}
