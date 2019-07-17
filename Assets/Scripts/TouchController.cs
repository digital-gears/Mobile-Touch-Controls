using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TouchController : MonoBehaviour
{
    
    public Transform ScrollablePlane;
    public float TouchTolerance = 0.5f;
    public float RotationSpeed = 1f;
    public float ZoomSpeed = 1f;

    private Vector2 lastTouch0;
    private Vector2 lastTouch1;
    private Vector2 newTouch0;
    private Vector2 newTouch1;

    private Vector3 newTouchDrag;
    private Vector3 lastTouchDrag;
    private Vector3 direction;

    private bool StaticTouch=false;
    private bool moving = false;
    private Transform _focus;
    private GameManager _gameManager;
    private CameraFollow _camFollow;

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
        _camFollow = gameObject.GetComponent<CameraFollow>();
        _focus = _camFollow.target;
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
                        Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                        LayerMask mask = LayerMask.GetMask("Floor");
                        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,mask))
                        {
                            if (moving)
                            {
                                lastTouchDrag = hit.point;
                                direction = newTouchDrag - lastTouchDrag;
                                direction = new Vector3(direction.x, 0f, direction.z);
                                _focus.position = _focus.position + direction;
                            }
                            else
                            {
                                newTouchDrag = hit.point;
                            }
                        }
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
            switch (Input.touchCount)
            {
                case 1:
                    ChangeState(TouchState.Idle);
                    break;
                case 2:
                    bool moved = false;
                    if (Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        newTouch0 = Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position);
                        lastTouch0 = newTouch0;
                    }
                    if (Input.GetTouch(1).phase == TouchPhase.Began)
                    {
                        newTouch1 = Camera.main.ScreenToViewportPoint(Input.GetTouch(1).position);
                        lastTouch0 = newTouch1;
                    }
                    if (Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        lastTouch0 = Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position);
                        moved = true;
                    }
                    if (Input.GetTouch(1).phase == TouchPhase.Moved)
                    {
                        lastTouch1 = Camera.main.ScreenToViewportPoint(Input.GetTouch(1).position);
                        moved = true;
                    }
                    if (moved)
                    {
                        ZoomOrRotate();
                    }
                    break;
                default:
                    ChangeState(TouchState.Idle);
                    break;
            }
            yield return 0;
        }
    }

    private void ZoomOrRotate()
    {
         if(Vector3.Magnitude(lastTouch0-newTouch0)>TouchTolerance&& Vector3.Magnitude(lastTouch1 - newTouch1) > TouchTolerance)
        {
            ChangeState(TouchState.Zoom);
        }
        if ((Vector3.Magnitude(lastTouch0 - newTouch0) > 4*TouchTolerance && Vector3.Magnitude(lastTouch1 - newTouch1) < TouchTolerance*0.1f)|| (Vector3.Magnitude(lastTouch1 - newTouch1) > 4*TouchTolerance && Vector3.Magnitude(lastTouch0 - newTouch0) < TouchTolerance * 0.1f))
        {
            //ChangeState(TouchState.Rotate);
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
            switch (Input.touchCount)
            {
                case 1:
                    ChangeState(TouchState.Idle);
                    break;
                case 2:
                    if (Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        lastTouch0 = Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position);
                        Zooming();
                    }
                    if (Input.GetTouch(1).phase == TouchPhase.Moved)
                    {
                        lastTouch1 = Camera.main.ScreenToViewportPoint(Input.GetTouch(1).position);
                        Zooming();
                    }
                    break;
                default:
                    ChangeState(TouchState.Idle);
                    break;
            }
            yield return 0;
        }
    }

    private void Zooming()
    {
        float zoom = Vector3.Magnitude(lastTouch0 - lastTouch1) - Vector3.Magnitude(newTouch0 - newTouch1);
            //Vector3.Distance(lastTouch0, lastTouch1)- Vector3.Distance(newTouch0, newTouch1);
        Debug.Log(zoom);
        if (_camFollow.zoom + zoom > 5)
        {
            _camFollow.zoom = 5f;
        }
        else
        {
            if (_camFollow.zoom + zoom < 1)
            {
                _camFollow.zoom = 1f;
            }
            else
            {
                _camFollow.zoom += zoom;
            }
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
