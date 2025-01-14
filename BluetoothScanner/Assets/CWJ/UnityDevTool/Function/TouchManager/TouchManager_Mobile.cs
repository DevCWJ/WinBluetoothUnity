﻿using System.Collections;

using UnityEngine;
using UnityEngine.Events;

namespace CWJ
{
    public class TouchManager_Mobile : TouchManager
    {
        //멀티터치O
        protected int detectMultiTouchNeedCount = 0; //멀티터치 인식 필요한 리스너 수

        private UnityEvent[] multiTouchEvents = new UnityEvent[5]
        {
            new UnityEvent(), //Began
            new UnityEvent(), //Moved
            new UnityEvent(), //Stationary
            new UnityEvent(), //Ended
            new UnityEvent() //Canceled
        };

        private UnityEvent multi_onUpdateEnded = new UnityEvent();

        public override sealed bool AddTouchListener(TouchListener listener)
        {
            if (!base.AddTouchListener(listener))
            {
                return false;
            }

            if (listener.isMultiTouchOnly) //멀티터치 인식/처리할지
            {
                for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
                {
                    multiTouchEvents[i].AddListener_New(listener.touchEvents[i].Invoke, false);
                }

                multi_onUpdateEnded.AddListener_New(listener.onUpdateEnded.Invoke, false);
                detectMultiTouchNeedCount += 1;
            }
            else
            {
                for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
                {
                    touchEvents[i].AddListener_New(listener.touchEvents[i].Invoke, false);
                }

                onUpdateEnded.AddListener_New(listener.onUpdateEnded.Invoke, false);
            }

            ArrayUtil.Add(ref touchListeners, listener);
            return true;
        }

        public override sealed bool RemoveTouchListener(TouchListener listener)
        {
            if (!base.RemoveTouchListener(listener))
            {
                return false;
            }

            ArrayUtil.Remove(ref touchListeners, listener);

            if (listener.isMultiTouchOnly) //멀티터치 인식/처리 했던것인지
            {
                detectMultiTouchNeedCount -= 1;
                for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
                {
                    multiTouchEvents[i].RemoveListener_New(listener.touchEvents[i].Invoke);
                }

                multi_onUpdateEnded.RemoveListener_New(listener.onUpdateEnded.Invoke);
            }
            else
            {
                for (int i = 0; i < 5; i++) //5 = EnumUtil.GetLength<TouchPhase>()
                {
                    touchEvents[i].RemoveListener_New(listener.touchEvents[i].Invoke);
                }

                onUpdateEnded.RemoveListener_New(listener.onUpdateEnded.Invoke);
            }
            return true;
        }

        public override sealed void UpdateInputSystem()
        {
            if (Input.touchCount == 0) //for S-Pen input (★☆ Only supported on Unity 2018.4.13f1 and later ☆★) ..Tlqkf
            {
                MousePhase mousePhase = MousePhase.None;

                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    mousePhase = MousePhase.Began;
                }
                else if (Input.GetKey(KeyCode.Mouse0))
                {
                    mousePhase = GetPhaseWhenMousePressed();
                }
                else if (Input.GetKeyUp(KeyCode.Mouse0))
                {
                    mousePhase = MousePhase.Ended;
                    if (CO_StationaryCheck != null)
                    {
                        StopCoroutine(CO_StationaryCheck);
                        CO_StationaryCheck = null;
                    }
                }

                isHoldDown = mousePhase < MousePhase.Ended;
                isMoving = mousePhase == MousePhase.Moved;

                if (mousePhase != MousePhase.None)
                {
                    touchEvents[mousePhase.ToInt()]?.Invoke();
                }
            }
            else if (Input.touchCount > 0)
            {// Touch
                TouchPhase touchPhase = Input.GetTouch(0).phase;
                isHoldDown = touchPhase < TouchPhase.Ended;
                isMoving = touchPhase == TouchPhase.Moved;
                touchEvents[touchPhase.ToInt()]?.Invoke();

                if (detectMultiTouchNeedCount > 0) //멀티터치이벤트 있을경우
                {
                    int touchCount = Input.touchCount;
                    for (int j = 0; j < touchCount; j++)
                    {
                        multiTouchEvents[Input.GetTouch(j).phase.ToInt()]?.Invoke();
                    }
                }
            }

            onUpdateEnded?.Invoke();
            if (detectMultiTouchNeedCount > 0)
            {
                multi_onUpdateEnded?.Invoke();
            }

        }

        Vector3 prevMousePos;
        //(Input.GetAxisRaw("Mouse X") == .0f && Input.GetAxisRaw("Mouse Y") == .0f); touch되는 windows에서 touch 커서드래그를 인식못함
        private bool GetMouseStationary()
        {
            Vector3 prevPos = prevMousePos;
            prevMousePos = Input.mousePosition;
            return prevPos == Input.mousePosition;
        }

        //GetAxisRaw 가 버그가있어서 코루틴필요
        private MousePhase GetPhaseWhenMousePressed()
        {
            if (GetMouseStationary())
            {
                if (isMoving)
                {
                    if (CO_StationaryCheck == null)
                    {
                        CO_StationaryCheck = DO_StationaryCheck();
                        StartCoroutine(CO_StationaryCheck);
                    }
                    return MousePhase.Moved;
                }
                else
                {
                    return MousePhase.Stationary;
                }
            }
            else
            {
                if (CO_StationaryCheck != null)
                {
                    StopCoroutine(CO_StationaryCheck);
                    CO_StationaryCheck = null;
                }

                return MousePhase.Moved;
            }
        }

        private IEnumerator CO_StationaryCheck = null;
        private IEnumerator DO_StationaryCheck()
        {
            yield return null; //이상하면 WaitForEndOfFrame() 로 바꾸기

            if (GetMouseStationary())
            {
                isMoving = false;
            }

            CO_StationaryCheck = null;
        }
    }
}