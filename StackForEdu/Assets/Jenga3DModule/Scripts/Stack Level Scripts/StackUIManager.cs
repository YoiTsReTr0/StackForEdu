using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jenga3DModule.Scripts
{
    public class StackUIManager : MonoBehaviour
    {
        public static StackUIManager Instance;

        #region Serialized Fields

        [SerializeField] private GameObject StackSetupPanel;
        [SerializeField] private GameObject StackSelectionPanel;
        [SerializeField] private GameObject StackSelectedPanel;

        [SerializeField] private Button TestMyStackBtn;
        [SerializeField] private Button BackToStacksBtn;

        //[Space(20)] [SerializeField] private List<Button> StackOfClassButtons;

        #endregion


        private StackSceneManager stackSceneManager;


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

            stackSceneManager.stackInitializationEvent.AddListener(OnInitializeStackVoid);
            stackSceneManager.stackSelectEvent.AddListener(OnSelectStackVoid);
            stackSceneManager.stackDeselectEvent.AddListener(OnDeselectStackVoid);
        }

        #region Main Functions

        public void InitializeStacksWithData()
        {
            StackSceneManager.Instance.stackInitializationEvent?.Invoke();

            if (!TestMyStackBtn.enabled)
                TestMyStackBtn.enabled = true;

            StartCoroutine(stackSceneManager.TempDelayForRaycastColliders());
        }

        public void TestMyStackVoid()
        {
            StackSceneManager.Instance.testMyStackEvent?.Invoke();

            TestMyStackBtn.enabled = false;
        }

        public void SetStackByNumAndSelect(int num = 0)
        {
            SetBaseLookAtOfSceneManager(num);

            stackSceneManager.stackSelectEvent?.Invoke();
        }

        //helper
        private void SetBaseLookAtOfSceneManager(int num)
        {
            stackSceneManager.SelectedStackBaseLookAt = stackSceneManager.StackLookAtTransforms[num];
        }

        #endregion


        #region UI Event Functions

        private void OnInitializeStackVoid()
        {
            BackToStacksBtn.onClick.AddListener(() => stackSceneManager.stackDeselectEvent?.Invoke());

            StackSetupPanel.SetActive(false);
            StackSelectionPanel.SetActive(true);
            StackSelectedPanel.SetActive(false);
        }

        private void OnSelectStackVoid()
        {
            StackSelectionPanel.SetActive(false);
            StackSelectedPanel.SetActive(true);
        }

        private void OnDeselectStackVoid()
        {
            StackSelectionPanel.SetActive(true);
            StackSelectedPanel.SetActive(false);
        }

        #endregion
    }
}