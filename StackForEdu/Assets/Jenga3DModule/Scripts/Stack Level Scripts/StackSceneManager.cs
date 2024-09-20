using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Jenga3DModule.Scripts.Runtime.Utils;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace Jenga3DModule.Scripts
{
    public class StackSceneManager : MonoBehaviour
    {
        public static StackSceneManager Instance;

        #region public events

        /// <summary>
        /// Called for the setup of the stacks
        /// </summary>
        public UnityEvent stackInitializationEvent = new();

        /// <summary>
        /// Called on Stack selection when clicking on a stack (when in idle condition)
        /// </summary>
        public UnityEvent stackSelectEvent = new();

        /// <summary>
        /// Called on deselection of the Stack. 
        /// </summary>
        public UnityEvent stackDeselectEvent = new();

        /// <summary>
        /// Called when Test My Stack button is pressed
        /// </summary>
        public UnityEvent testMyStackEvent = new();

        #endregion

        #region Editor Fields

        [SerializeField] private StackBlockObject StackObjectPrefab;
        [SerializeField] private List<Material> StackMaterialsList;
        [SerializeField] private List<Transform> StackParents;

        #endregion

        #region Local Variables

        //Data sets
        private Dictionary<int, ExtendedObjectPool<StackBlockObject>> pools;
        private List<StackData> completeData = new();
        private List<Queue<StackData>> dataQueues = new();


        //Gameplay Variables
        private bool isAnyStackSelected = true;

        public bool IsAnyStackSelected
        {
            get { return isAnyStackSelected; }
        }

        private Coroutine DelayCoroutine;

        #endregion

        #region Stack Selection Requisites

        public List<Transform> StackLookAtTransforms;
        [HideInInspector] public Transform SelectedStackBaseLookAt;

        #endregion


        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(Instance);

            if (Instance == null)
                Instance = this;

            stackInitializationEvent.RemoveAllListeners();
            stackSelectEvent.RemoveAllListeners();
            stackDeselectEvent.RemoveAllListeners();
            testMyStackEvent.RemoveAllListeners();
        }


        private void Start()
        {
            for (int i = 0; i < 3; i++)
                dataQueues.Add(new());

            // Events setup
            {
                stackInitializationEvent.AddListener(PreStacksInitialization);
                stackSelectEvent.AddListener(() =>
                {
                    isAnyStackSelected = true;
                    for (int i = 0; i < StackParents.Count; i++)
                    {
                        StackParents[i].GetChild(0).GetChild((1)).gameObject.SetActive(false);
                    }
                });
                stackDeselectEvent.AddListener(() =>
                {
                    isAnyStackSelected = false;

                    if (DelayCoroutine != null)
                        StopCoroutine(DelayCoroutine);

                    DelayCoroutine = StartCoroutine(TempDelayForRaycastColliders());
                });
                testMyStackEvent.AddListener(TestMyStackVoid);
            }
        }

        private void Update()
        {
            if (!isAnyStackSelected && Input.GetMouseButtonUp(0))
                SendRaycast();
        }


        #region Stack Setup Area

        /// <summary>
        /// Call the function to update stacks data and continue with stacks initialization
        /// </summary>
        /// <param name="data">Input the updated data</param>
        private void PreStacksInitialization()
        {
            isAnyStackSelected = false;

            // Update data and sort as required 
            List<StackData> data =
                JsonConvert.DeserializeObject<List<StackData>>(Resources.Load<TextAsset>("APIDataJSON").text);
            completeData = SortStackData(data);

            dataQueues[0] =
                new Queue<StackData>(completeData.Where(completeData => completeData.grade == "6th Grade").ToList());
            dataQueues[1] =
                new Queue<StackData>(completeData.Where(completeData => completeData.grade == "7th Grade").ToList());
            dataQueues[2] =
                new Queue<StackData>(completeData.Where(completeData => completeData.grade == "8th Grade").ToList());

            if (pools == null)
                // Initialize the pools dictionary
                pools = new Dictionary<int, ExtendedObjectPool<StackBlockObject>>
                {
                    { 6, CreateNewPool(0) },
                    { 7, CreateNewPool(1) },
                    { 8, CreateNewPool(2) }
                };

            else // if already present the clear pool and update
            {
                foreach (var pool in pools.Values)
                {
                    pool.ReleaseAll();
                }

                /*pools[6] = CreateNewPool(0);
                pools[7] = CreateNewPool(1);
                pools[8] = CreateNewPool(2);*/
            }

            StacksInitialization();
        }


        /// <summary>
        /// Main initialization function
        /// </summary>
        private void StacksInitialization()
        {
            InitDataQueues(0);
            InitDataQueues(1);
            InitDataQueues(2);
        }


        /// <summary>
        /// Set Physics to on and test the integrity of the stack
        /// </summary>
        private void TestMyStackVoid()
        {
            foreach (var obj in pools)
            {
                var objList = obj.Value.GetActiveItems();

                for (int i = 0; i < objList.Count; i++)
                {
                    if (!objList[i].MasteryLevelText.transform.parent.gameObject.activeSelf)
                    {
                        obj.Value.Release(objList[i]);
                        i -= 1;
                    }

                    else
                        objList[i].TestMyStackVoid();
                }
            }
        }

        #endregion


        #region Stack Selection & Raycasting Area

        /// <summary>
        /// Sends a raycast when no stacks are selected, On mouse click
        /// </summary>
        private void SendRaycast()
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(camRay, out RaycastHit hit, 70))
            {
                if (!hit.transform.CompareTag("StackSelection"))
                    return;

                SelectedStackBaseLookAt = hit.transform.parent.GetChild(0); //Specifically made to follow pattern

                stackSelectEvent?.Invoke();
            }
        }

        #endregion


        #region Helper Functions

        #region Stack Setup Helpers

        /// <summary>
        /// Function creates new objects from pool
        /// </summary>
        /// <param name="index"></param>
        private void InitDataQueues(int index)
        {
            int x = dataQueues[index].Count; //helper
            for (int i = 0; i < x; i++)
            {
                var obj = pools[index + 6].Get();
                obj.transform.SetParent(StackParents[index]);

                obj.ResetMyStackVoid();

                // set positions in jenga format
                AlignBlockInJengaFormat(obj.transform, i);
            }
        }


        /// <summary>
        /// Creates a new pool and setups required information
        /// </summary>
        /// <param name="num">Index in dataQueues</param>
        /// <returns></returns>
        private ExtendedObjectPool<StackBlockObject> CreateNewPool(int num = 0)
        {
            Queue<StackData> queueToUse = new();


            return new ExtendedObjectPool<StackBlockObject>(
                createFunc: () => Instantiate(StackObjectPrefab),
                onGet: obj =>
                {
                    queueToUse = dataQueues[num];
                    AssignDataToObject(obj, queueToUse.Dequeue());
                    obj.gameObject.SetActive(true);
                },
                onRelease: obj => obj.gameObject.SetActive(false),
                onDestroy: obj => Destroy(obj.gameObject),
                true,
                50,
                120,
                fillToCapacity: false
            );
        }


        /// <summary>
        /// Setup new positions and rotations for the object in the form of Jenga blocks
        /// </summary>
        /// <param name="obj">Transform of the object</param>
        /// <param name="i">Index of the object from the list</param>
        private void AlignBlockInJengaFormat(Transform obj, int i)
        {
            int yPosi;
            yPosi = i / 3;

            int rot;

            //i += 1;

            switch ((i + 1) % 3)
            {
                case 1: //exclusive 1st position
                    obj.localPosition = calculatePosi(1, i / 3);
                    obj.localRotation = Quaternion.Euler(calculateRotation(1, i / 3));
                    break;
                case 2: // exclusive middle line 
                    obj.localPosition = new(0, 0.6f + (yPosi * 0.65f), 0);

                    // calc rotation and set it to 90 or 0
                    rot = (90 * ((i / 3)));
                    rot = rot % 180 == 0 ? 0 : rot;

                    obj.localRotation = Quaternion.Euler(0, rot, 0);
                    break;
                case 0: //exclusive 3rd position
                    obj.localPosition = calculatePosi(3, i / 3);
                    obj.localRotation = Quaternion.Euler(calculateRotation(3, i / 3));
                    break;
            }
        }


        /// <summary>
        /// Assign Data to StackBlockObject
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="stackData"></param>
        private void AssignDataToObject(StackBlockObject obj, StackData stackData)
        {
            obj.ObjMeshRenderer.material = StackMaterialsList[stackData.mastery];

            switch (stackData.mastery)
            {
                case 0:
                    obj.MasteryLevelText.text = "";
                    obj.MasteryLevelText.transform.parent.gameObject.SetActive(false);
                    break;
                case 1:
                    obj.MasteryLevelText.text = "Learned";
                    obj.MasteryLevelText.transform.parent.gameObject.SetActive(true);

                    break;
                case 2:
                    obj.MasteryLevelText.text = "Mastered";
                    obj.MasteryLevelText.transform.parent.gameObject.SetActive(true);

                    break;
            }

            ;

            // Log assigned data
            Debug.Log(
                $"Assigned data to object: Mastery={stackData.mastery}, Material={StackMaterialsList[stackData.mastery].name}");
        }


        /// <summary>
        /// Sorts the StackData based on domain, cluster, and standard ID.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<StackData> SortStackData(List<StackData> data)
        {
            return data
                .OrderBy(stackData => stackData.domain)
                .ThenBy(stackData => stackData.cluster)
                .ThenBy(stackData => stackData.standardid)
                .ToList();
        }


        /// <summary>
        /// Returns new position for the object in Jenga Stack
        /// </summary>
        /// <param name="posiIndex">Position index in a row - (1, 2 or 3)</param>
        /// <param name="rowIndex">Index of row in the Jenga</param>
        /// <returns></returns>
        private Vector3 calculatePosi(int posiIndex, int rowIndex)
        {
            float y = 0.6f + (rowIndex * 0.65f);

            if (rowIndex % 2 == 0)
                return posiIndex == 1 ? new Vector3(0, y, 1.375f) : new Vector3(0, y, -1.375f);


            else
                return posiIndex == 1 ? new Vector3(1.375f, y, 0) : new Vector3(-1.375f, y, 0);


            return default;
        }


        /// <summary>
        /// Returns new rotation for the object in Jenga Stack
        /// </summary>
        /// <param name="posiIndex">Position index in a row - (1, 2 or 3)</param>
        /// <param name="rowIndex">Index of row in the Jenga</param>
        /// <returns></returns>
        private Vector3 calculateRotation(int posiIndex, int rowIndex)
        {
            if (rowIndex % 2 == 0)
                return posiIndex == 1 ? new Vector3(0, 0, 0) : posiIndex == 3 ? new Vector3(0, 180, 0) : default;


            else
                return posiIndex == 1 ? new Vector3(0, 90, 0) : posiIndex == 3 ? new Vector3(0, -90, 0) : default;


            return default;
        }

        #endregion

        #endregion

        public IEnumerator TempDelayForRaycastColliders()
        {
            yield return new WaitForSeconds(2f);

            for (int i = 0; i < StackParents.Count; i++)
            {
                StackParents[i].GetChild(0).GetChild((1)).gameObject.SetActive(true);
            }
        }
    }
}