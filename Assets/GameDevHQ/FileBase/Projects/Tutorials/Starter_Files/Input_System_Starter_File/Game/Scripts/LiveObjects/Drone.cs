using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Game.Scripts.UI;
using UnityEngine.InputSystem; // Подключаем новую систему ввода

namespace Game.Scripts.LiveObjects
{
    public class Drone : MonoBehaviour
    {
        private enum Tilt
        {
            NoTilt, Forward, Back, Left, Right
        }

        [SerializeField]
        private Rigidbody _rigidbody;
        [SerializeField]
        private float _speed = 5f;
        private bool _inFlightMode = false;
        [SerializeField]
        private Animator _propAnim;
        [SerializeField]
        private CinemachineVirtualCamera _droneCam;
        [SerializeField]
        private InteractableZone _interactableZone;

        [Header("New Input Actions")]
        [SerializeField]
        private InputActionReference _tiltAction;       // Vector2 (WASD) для наклонов
        [SerializeField]
        private InputActionReference _yawAction;        // Axis (Left/Right Arrows) для разворота
        [SerializeField]
        private InputActionReference _throttleAction;   // Axis (Space/V) для высоты
        [SerializeField]
        private InputActionReference _exitFlightAction; // Button (Escape) для выхода
        

        public static event Action OnEnterFlightMode;
        public static event Action onExitFlightmode;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterFlightMode;
        }

        private void EnterFlightMode(InteractableZone zone)
        {
            if (_inFlightMode != true && zone.GetZoneID() == 4) // drone Scene
            {
                _propAnim.SetTrigger("StartProps");
                _droneCam.Priority = 11;
                _inFlightMode = true;
                OnEnterFlightMode?.Invoke();
                UIManager.Instance.DroneView(true);
                _interactableZone.CompleteTask(4);
            }
        }

        private void ExitFlightMode()
        {            
            _droneCam.Priority = 9;
            _inFlightMode = false;
            UIManager.Instance.DroneView(false);            
        }

        private void Update()
        {
            if (_inFlightMode)
            {
                CalculateTilt();
                CalculateMovementUpdate();

                // Выход из режима полета по триггеру экшена
                if (_exitFlightAction != null && _exitFlightAction.action.triggered)
                {
                    _inFlightMode = false;
                    onExitFlightmode?.Invoke();
                    ExitFlightMode();
                }
            }
        }

        private void FixedUpdate()
        {
            _rigidbody.AddForce(transform.up * (9.81f), ForceMode.Acceleration);
            if (_inFlightMode)
                CalculateMovementFixedUpdate();
        }

        private void CalculateMovementUpdate()
        {
            if (_yawAction == null) return;

            // Читаем значение оси разворота (-1 — влево, 1 — вправо)
            float yawInput = _yawAction.action.ReadValue<float>();

            if (Mathf.Abs(yawInput) > 0.01f)
            {
                var tempRot = transform.localRotation.eulerAngles;
                tempRot.y += yawInput * (_speed / 3);
                transform.localRotation = Quaternion.Euler(tempRot);
            }
        }

        private void CalculateMovementFixedUpdate()
        {
            if (_throttleAction == null) return;

            // Читаем значение вертикальной оси (1 — вверх, -1 — вниз)
            float throttleInput = _throttleAction.action.ReadValue<float>();

            if (throttleInput > 0.01f) // Аналог Space
            {
                _rigidbody.AddForce(transform.up * _speed, ForceMode.Acceleration);
            }
            else if (throttleInput < -0.01f) // Аналог V
            {
                _rigidbody.AddForce(-transform.up * _speed, ForceMode.Acceleration);
            }
        }

        private void CalculateTilt()
        {
            if (_tiltAction == null) return;

            // Читаем двумерный вектор направления (X: A/D, Y: S/W)
            Vector2 tiltInput = _tiltAction.action.ReadValue<Vector2>();
            float currentY = transform.localRotation.eulerAngles.y;

            // Сохраняем исходный приоритет проверок старого скрипта (A -> D -> W -> S)
            if (tiltInput.x < -0.1f)      // Нажата A
                transform.rotation = Quaternion.Euler(0, currentY, 30);
            else if (tiltInput.x > 0.1f)  // Нажата D
                transform.rotation = Quaternion.Euler(0, currentY, -30);
            else if (tiltInput.y > 0.1f)  // Нажата W
                transform.rotation = Quaternion.Euler(30, currentY, 0);
            else if (tiltInput.y < -0.1f) // Нажата S
                transform.rotation = Quaternion.Euler(-30, currentY, 0);
            else                          // Ничего не нажато
                transform.rotation = Quaternion.Euler(0, currentY, 0);
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterFlightMode;
        }
    }
}