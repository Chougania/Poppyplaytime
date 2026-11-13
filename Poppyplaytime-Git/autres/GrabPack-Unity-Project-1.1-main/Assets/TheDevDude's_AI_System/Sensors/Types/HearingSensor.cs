using UnityEngine;

public class HearingSensor : SensorBase
{
    [SerializeField] private float hearingRange = 15f;

    public void HearNoise(Vector3 position, float loudness, Transform source = null)
    {
        if (Time.time < blackboard.Get<float>(AIKeys.IgnoreHearingUntil))
            return;

        float distance = Vector3.Distance(transform.position, position);

        if (distance > hearingRange)
            return;

        float strength = loudness * (1f - (distance / hearingRange));

        Stimulus stimulus = new Stimulus(
            StimulusSubject.Player,
            StimulusType.Sound,
            position,
            source,
            strength
        );

        blackboard.Set(AIKeys.HeardNoise, true);
        blackboard.Set(AIKeys.LastNoisePosition, position);
        blackboard.Set(AIKeys.LastNoiseTime, Time.time);
        blackboard.Set(AIKeys.NoiseVolume, strength);

        memorySystem.ProcessStimulus(stimulus);
    }

    public override void Sense()
    {

    }
}