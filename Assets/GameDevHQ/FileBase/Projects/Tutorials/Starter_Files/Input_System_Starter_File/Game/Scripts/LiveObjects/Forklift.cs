using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // Подключаем новую систему ввода

namespace Game.Scripts.LiveObjects
{
    public class Forklift : MonoBehaviour
    {
        [SerializeField]
        private GameObject _lift, _steeringWheel, _leftWheel, _rightWheel, _rearWheels;
        [SerializeField]
        private Vector3 _liftLowerLimit, _liftUpperLimit;
        [SerializeField]
        private float _speed = 5f, _liftSpeed = 1f;
        [SerializeField]
        private CinemachineVirtualCamera _forkliftCam;
        [SerializeField]
        private GameObject _driverModel;
        private bool _inDriveMode = false;
        [SerializeField]
        private InteractableZone _interactableZone;

        [Header("New Input Actions")]
        [SerializeField]
        private InputActionReference _moveAction;      // Vector2 (WASD / Стрелки) для движения и руления
        [SerializeField]
        private InputActionReference _liftAction;      // Axis (R / T) для подъема и спуска вилки
        [SerializeField]
        private InputActionReference _exitDriveAction; // Button (Escape) для выхода из машины

        public static event Action onDriveModeEntered;
        public static event Action onDriveModeExited;

        private void OnEnable()
        {
            InteractableZone.onZoneInteractionComplete += EnterDriveMode;
        }

        private void EnterDriveMode(InteractableZone zone)
        {
            if (_inDriveMode != true && zone.GetZoneID() == 5) //Enter ForkLift
            {
                _inDriveMode = true;
                _forkliftCam.Priority = 11;
                onDriveModeEntered?.Invoke();
                _driverModel.SetActive(true);
                _interactableZone.CompleteTask(5);
            }
        }

        private void ExitDriveMode()
        {
            _inDriveMode = false;
            _forkliftCam.Priority = 9;            
            _driverModel.SetActive(false);
            onDriveModeExited?.Invoke();
        }

        private void Update()
        {
            if (_inDriveMode == true)
            {
                LiftControls();
                CalcutateMovement();

                // Отслеживаем триггер выхода (вызывается один раз за кадр нажатия)
                if (_exitDriveAction != null && _exitDriveAction.action.triggered)
                {
                    ExitDriveMode();
                }
            }
        }

        private void CalcutateMovement()
        {
            if (_moveAction == null) return;

            // Читаем вектор движения: x = Horizontal (руль), y = Vertical (газ/тормоз)
            Vector2 moveInput = _moveAction.action.ReadValue<Vector2>();
            float h = moveInput.x;
            float v = moveInput.y;

            var direction = new Vector3(0, 0, v);
            var velocity = direction * _speed;

            transform.Translate(velocity * Time.deltaTime);

            if (Mathf.Abs(v) > 0.01f)
            {
                var tempRot = transform.rotation.eulerAngles;
                tempRot.y += h * _speed / 2;
                transform.rotation = Quaternion.Euler(tempRot);
            }
        }

        private void LiftControls()
        {
            if (_liftAction == null) return;

            // Читаем значение оси управления подъемником (1 — вверх, -1 — вниз)
            float liftInput = _liftAction.action.ReadValue<float>();

            if (liftInput > 0.01f)       // Аналог зажатой кнопки R
                LiftUpRoutine();
            else if (liftInput < -0.01f) // Аналог зажатой кнопки T
                LiftDownRoutine();
        }

        private void LiftUpRoutine()
        {
            if (_lift.transform.localPosition.y < _liftUpperLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y += Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y >= _liftUpperLimit.y)
                _lift.transform.localPosition = _liftUpperLimit;
        }

        private void LiftDownRoutine()
        {
            // Исправлена логика: проверяем нижний лимит вместо верхнего
            if (_lift.transform.localPosition.y > _liftLowerLimit.y)
            {
                Vector3 tempPos = _lift.transform.localPosition;
                tempPos.y -= Time.deltaTime * _liftSpeed;
                _lift.transform.localPosition = new Vector3(tempPos.x, tempPos.y, tempPos.z);
            }
            else if (_lift.transform.localPosition.y <= _liftLowerLimit.y)
                _lift.transform.localPosition = _liftLowerLimit;
        }

        private void OnDisable()
        {
            InteractableZone.onZoneInteractionComplete -= EnterDriveMode;
        }
    }
}