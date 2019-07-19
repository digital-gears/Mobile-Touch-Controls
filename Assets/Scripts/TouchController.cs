using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TouchController : MonoBehaviour
{
    
    public Transform ScrollablePlane;
    public float TouchTolerance = 0.5f;
    public float BorderLimit = 1f;
    public float RotationSpeed = 0.7f;
    public float ZoomSpeed = 1f;
    public bool Rotate=true;

    private Vector2 lastTouch0;
    private Vector2 lastTouch1;
    private Vector2 newTouch0;
    private Vector2 newTouch1;

    private Vector3 newTouchDrag;
    private Vector3 lastTouchDrag;
    private Vector3 direction;

    private bool StaticTouch=false;
    private bool moving = false;
    private bool zoomingAndRotating = false;
    private Transform _focus;
    private GameManager _gameManager;
    private CameraFollow _camFollow;

    private enum TouchState
    {
        Idle, //No touch or more than two touches +++Goes to: Select, Order, Swipe, TwoFingerIdle+++
        Swipe,//One Touch and drag over scrollable plane +++Goes to: Idle, TwoFingerIdle+++
        TwoFingerIdle, //Two still touches +++Goes to : ZoomAndRotate, Idle+++
        ZoomAndRotate //Two finger moving touches +++Goes to: Idle+++
    }

    TouchState CurrentState = TouchState.Idle;

    #region Finite State Machine Setup
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
        CurrentState = NextState;
    }
    #endregion

    #region Idle

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

    //Sorts the touch as an order or a selection
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

    //Moves the selected Character to a target destination
    private void WalkTo(Vector3 target)
    {
        target = new Vector3(target.x, 1f, target.z);
        _gameManager.Characters[_gameManager.SelectedCharacter - 1].MoveTo(target);
    }

    //Returns true if two touches are near from each other
    private bool NearTouches(Vector2 aux1, Vector2 aux2)
    {
        return (Vector2.Distance(aux1, aux2) < TouchTolerance);
    }
    #endregion

    #region Swipe

    IEnumerator Swipe() //This loops in Swipe state
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
                                //_focus.position = _focus.position + direction;
                                Move(_focus, direction);
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

    private void Move(Transform focus,Vector3 dir)
    {
        Vector3 newPosition = focus.position + dir;
        float xedge = ScrollablePlane.transform.lossyScale.x * 5f;
        float zedge = ScrollablePlane.transform.lossyScale.z * 5f;
        if (newPosition.x > xedge-BorderLimit)
            newPosition.x = xedge-BorderLimit;
        if (newPosition.x < -xedge+BorderLimit)
            newPosition.x = -xedge+BorderLimit;
        if (newPosition.z > zedge-BorderLimit)
            newPosition.z = zedge-BorderLimit;
        if (newPosition.z < -zedge+BorderLimit)
            newPosition.z = -zedge+BorderLimit;
        focus.position = newPosition;
    }

    #endregion

    #region TwoFingerIdle

    IEnumerator TwoFingerIdle() //This loops while thwo fingers are touching the screen but not moving enough to rotate or zoom
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
                        ChangeState(TouchState.ZoomAndRotate);
                    }
                    break;
                default:
                    ChangeState(TouchState.Idle);
                    break;
            }
            yield return 0;
        }
    }

    #endregion

    #region Zoom And Rotate

    IEnumerator ZoomAndRotate()
    {
        while (CurrentState == TouchState.ZoomAndRotate)
        {
            switch (Input.touchCount)
            {
                case 1:
                    ChangeState(TouchState.Idle);
                    zoomingAndRotating = false;
                    break;
                case 2:
                    if (Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        lastTouch0 = Camera.main.ScreenToViewportPoint(Input.GetTouch(0).position);
                    }
                    if (Input.GetTouch(1).phase == TouchPhase.Moved)
                    {
                        lastTouch1 = Camera.main.ScreenToViewportPoint(Input.GetTouch(1).position);
                    }
                    if (zoomingAndRotating)
                    {
                        Zooming();
                        if (Rotate)
                        {
                            Rotating();
                        }
                    }
                    newTouch0 = lastTouch0;
                    newTouch1 = lastTouch1;
                    zoomingAndRotating = true;
                    break;
                default:
                    ChangeState(TouchState.Idle);
                    zoomingAndRotating = false;
                    break;
            }
            yield return 0;
        }
    }

    /* private void Rotating()
     {
         if (Vector3.Magnitude(lastTouch1 - newTouch1) < TouchTolerance * 0.1f){
             Vector2 V1 = newTouch0 - newTouch1;
             Vector2 V2 = lastTouch0 - newTouch1;
             float alpha = Mathf.Atan2(V1.y, V1.x);
             float beta = Mathf.Atan2(V2.y, V2.x);
             float gamma = beta - alpha;
             _camFollow.angle = _camFollow.angle - gamma;
         } 
         if (Vector3.Magnitude(lastTouch0 - newTouch0) < TouchTolerance * 0.1f){
             Vector2 V1 = newTouch1 - newTouch0;
             Vector2 V2 = lastTouch1 - newTouch0;
             float alpha = Mathf.Atan2(V1.y,V1.x);
             float beta = Mathf.Atan2(V2.y,V2.x);
             float gamma = beta - alpha;
             _camFollow.angle = _camFollow.angle - gamma;
         }
     }*/

    private void Rotating()
    {
        Vector2 C1 = (newTouch0 + newTouch1) / 2f;
        Vector2 C2 = (lastTouch0 + lastTouch1) / 2f;
        Vector2 V1 = newTouch1 - C1;
        if (V1 == Vector2.zero)
        {
            V1 = C1 - newTouch0;
        }
        Vector2 V2 = lastTouch1 - C2;
        if (V2 == Vector2.zero)
        {
            V2 = C2 - lastTouch0;
        }
        float alpha = Mathf.Atan2(V1.y, V1.x);
        float beta = Mathf.Atan2(V2.y, V2.x);
        float gamma = beta - alpha;
        _camFollow.angle = _camFollow.angle - gamma;

    }

        private void Zooming()
    {
        float zoom = Vector3.Magnitude(lastTouch0 - lastTouch1) - Vector3.Magnitude(newTouch0 - newTouch1);
        if (_camFollow.zoom - zoom > 5)
        {
            _camFollow.zoom = 5f;
        }
        else
        {
            if (_camFollow.zoom - zoom < 1)
            {
                _camFollow.zoom = 1f;
            }
            else
            {
                _camFollow.zoom -= zoom*ZoomSpeed;
            }
        }
    }
    #endregion
}
