using Cinemachine;
using UnityEngine;


namespace Jenga3DModule.Scripts
{
    public class StackCameraManager : MonoBehaviour
    {
        public static StackCameraManager Instance;

        private StackSceneManager stackSceneManager;

        #region Public References

        public CinemachineFreeLook FreeLookCam;
        public GameObject DefaultCam;

        #endregion


        [SerializeField] private Transform idlePosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(Instance);

            if (Instance == null)
                Instance = this;
        }

        private void Start()
        {
            stackSceneManager = StackSceneManager.Instance;

            stackSceneManager.stackSelectEvent.AddListener(() =>
                SetCameraAsPerStackSelection(true));

            stackSceneManager.stackDeselectEvent.AddListener(() =>
                SetCameraAsPerStackSelection(false));

            stackSceneManager.testMyStackEvent.AddListener(() => SetCameraAsPerStackSelection(false));
        }


        /// <summary>
        /// Enable or Disable camera movement with mouse 
        /// </summary>
        /// <param name="isStackSelected">Allows movement when true</param>
        private void SetCameraAsPerStackSelection(bool isStackSelected)
        {
            DefaultCam.SetActive(!isStackSelected);
            FreeLookCam.gameObject.SetActive(isStackSelected);

            if (isStackSelected)
            {
                FreeLookCam.LookAt = stackSceneManager.SelectedStackBaseLookAt;
            }
        }
    }
}