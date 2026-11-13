using UnityEngine;

public class AIBrain : MonoBehaviour
{
    [SerializeField] private float senseInterval = 0.2f;
    [SerializeField] private float memoryDecayRate = 0.1f;

    public Blackboard Blackboard { get; private set; }
    public MemorySystem MemorySystem { get; private set; }

    private SensorBase[] sensors;

    private float senseTimer;

    private UtilityBrain utilityBrain;

    private void Start()
    {
        utilityBrain?.StartBrain();
    }

    private void Awake()
    {
        Blackboard = new Blackboard();
        MemorySystem = new MemorySystem(Blackboard);

        sensors = GetComponents<SensorBase>();

        foreach (SensorBase sensor in sensors)
        {
            sensor.Initialize(Blackboard, MemorySystem);
        }

        utilityBrain = GetComponent<UtilityBrain>();

        if (utilityBrain != null)
        {
            utilityBrain.Initialize(this);
        }
    }

    private void Update()
    {
        senseTimer += Time.deltaTime;

        if (senseTimer >= senseInterval)
        {
            senseTimer = 0f;

            foreach (SensorBase sensor in sensors)
            {
                sensor.Sense();
            }
        }
        utilityBrain?.Tick();
        MemorySystem.Update(memoryDecayRate);
    }
}